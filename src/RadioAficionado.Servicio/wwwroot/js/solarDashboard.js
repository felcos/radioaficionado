'use strict';

/**
 * Modulo de Dashboard Solar — datos en tiempo real de NOAA + N0NBH.
 * Extiende el panel de propagación básico con viento solar, Bz, VHF, alertas y gráfico 30 días.
 */
const SolarDashboard = (function () {
    let graficoHistorico = null;

    function init() {
        cargarDatosSolares();
        cargarHistorico();

        const btnActualizar = document.getElementById('btn-actualizar-solar');
        if (btnActualizar) {
            btnActualizar.addEventListener('click', function () {
                cargarDatosSolares();
                cargarHistorico();
            });
        }
    }

    function cargarDatosSolares() {
        fetch('/api/propagacion/solar')
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                renderizarIndicesPrincipales(datos);
                renderizarVientoSolar(datos);
                renderizarCondicionesHf(datos.condicionesHf || []);
                renderizarCondicionesVhf(datos.condicionesVhf || {});
                renderizarEscalas(datos.escalas || {});
                renderizarAlertas(datos.alertasActivas || []);
            })
            .catch(function (err) {
                console.error('Error cargando datos solares:', err);
            });
    }

    function renderizarIndicesPrincipales(datos) {
        setValorConClase('solar-sfi', datos.sfi, { bueno: 150, regular: 90, invertido: false });
        setValorConClase('solar-kp', datos.kp, { bueno: 2, regular: 4, invertido: true });
        setValorConClase('solar-a', datos.ap, { bueno: 10, regular: 30, invertido: true });
        setTexto('solar-sn', datos.numeroManchasSolares || '--');
        setTexto('solar-xray', datos.rayosX || '--');
        setTexto('solar-geomag', datos.campoGeomagnetico || '--');
        setTexto('solar-ruido', datos.ruidoSenal || '--');
        setTexto('solar-actualizacion', datos.fechaActualizacion ? 'Actualizado: ' + datos.fechaActualizacion : '');
    }

    function renderizarVientoSolar(datos) {
        const velocidad = datos.velocidadVientoSolar || 0;
        setTexto('solar-viento-vel', Math.round(velocidad) + ' km/s');

        // Velocidad: <400 tranquilo (verde), 400-600 elevado (amarillo), >600 alto (rojo)
        const elemVel = document.getElementById('solar-viento-vel');
        if (elemVel) {
            elemVel.classList.remove('prop-bueno', 'prop-regular', 'prop-malo');
            if (velocidad < 400) { elemVel.classList.add('prop-bueno'); }
            else if (velocidad < 600) { elemVel.classList.add('prop-regular'); }
            else { elemVel.classList.add('prop-malo'); }
        }

        const bt = datos.bt || 0;
        const bz = datos.bzGsm || 0;
        setTexto('solar-bt', bt.toFixed(1) + ' nT');
        setTexto('solar-bz', bz.toFixed(1) + ' nT');

        // Bz negativo = bueno para propagación (más ionización)
        const elemBz = document.getElementById('solar-bz');
        if (elemBz) {
            elemBz.classList.remove('prop-bueno', 'prop-regular', 'prop-malo');
            if (bz <= -5) { elemBz.classList.add('prop-malo'); } // Tormenta
            else if (bz < 0) { elemBz.classList.add('prop-regular'); }
            else { elemBz.classList.add('prop-bueno'); }
        }
    }

    function renderizarCondicionesHf(condiciones) {
        const tbody = document.getElementById('solar-hf-tbody');
        if (!tbody) { return; }
        tbody.innerHTML = '';

        for (let i = 0; i < condiciones.length; i++) {
            const c = condiciones[i];
            const tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + (c.banda || '') + '</td>' +
                '<td class="' + claseCondicion(c.dia) + '">' + (c.dia || '') + '</td>' +
                '<td class="' + claseCondicion(c.noche) + '">' + (c.noche || '') + '</td>';
            tbody.appendChild(tr);
        }
    }

    function renderizarCondicionesVhf(vhf) {
        setTextoConClase('solar-vhf-aurora', vhf.auroraVhf || 'N/A');
        setTextoConClase('solar-vhf-eskip-eu', vhf.eSkipEuropa || 'N/A');
        setTextoConClase('solar-vhf-eskip-na', vhf.eSkipNorteamerica || 'N/A');
    }

    function renderizarEscalas(escalas) {
        setEscala('solar-escala-r', 'R', escalas.escalaR);
        setEscala('solar-escala-s', 'S', escalas.escalaS);
        setEscala('solar-escala-g', 'G', escalas.escalaG);
    }

    function setEscala(elementoId, letra, valor) {
        const elem = document.getElementById(elementoId);
        if (!elem) { return; }
        const nivel = valor || '0';
        elem.textContent = letra + nivel;
        elem.className = 'solar-escala-badge';
        if (nivel === '0' || !nivel) { elem.classList.add('escala-none'); }
        else if (nivel === '1') { elem.classList.add('escala-minor'); }
        else if (nivel === '2') { elem.classList.add('escala-moderate'); }
        else { elem.classList.add('escala-severe'); }
    }

    function renderizarAlertas(alertas) {
        const contenedor = document.getElementById('solar-alertas');
        if (!contenedor) { return; }
        contenedor.innerHTML = '';

        if (alertas.length === 0) {
            contenedor.innerHTML = '<div class="solar-sin-alertas">Sin alertas activas</div>';
            return;
        }

        const limite = Math.min(alertas.length, 5);
        for (let i = 0; i < limite; i++) {
            const alerta = alertas[i];
            const div = document.createElement('div');
            div.className = 'solar-alerta-item';
            // Extraer primera línea del mensaje
            const primeraLinea = (alerta.mensaje || '').split('\n')[0] || alerta.codigo;
            div.innerHTML =
                '<span class="solar-alerta-codigo">' + (alerta.codigo || '') + '</span> ' +
                '<span class="solar-alerta-texto">' + primeraLinea + '</span>';
            contenedor.appendChild(div);
        }
    }

    function cargarHistorico() {
        fetch('/api/propagacion/historico')
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                renderizarGraficoHistorico(datos);
            })
            .catch(function (err) {
                console.error('Error cargando histórico solar:', err);
            });
    }

    function renderizarGraficoHistorico(datos) {
        const canvas = document.getElementById('grafico-historico-solar');
        if (!canvas || typeof Chart === 'undefined') { return; }

        if (graficoHistorico) {
            graficoHistorico.destroy();
            graficoHistorico = null;
        }

        const sfiDatos = datos.sfi || [];
        const kpDatos = datos.kp || [];

        const etiquetasSfi = [];
        const valoresSfi = [];
        for (let i = 0; i < sfiDatos.length; i++) {
            etiquetasSfi.push(sfiDatos[i].fecha ? sfiDatos[i].fecha.substring(5, 10) : '');
            valoresSfi.push(sfiDatos[i].sfi || 0);
        }

        const etiquetasKp = [];
        const valoresKp = [];
        for (let i = 0; i < kpDatos.length; i++) {
            etiquetasKp.push(kpDatos[i].fecha ? kpDatos[i].fecha.substring(5, 10) : '');
            valoresKp.push(kpDatos[i].kp || 0);
        }

        // Usar las etiquetas de SFI (diario) como base
        const contexto = canvas.getContext('2d');
        graficoHistorico = new Chart(contexto, {
            type: 'line',
            data: {
                labels: etiquetasSfi,
                datasets: [
                    {
                        label: 'SFI',
                        data: valoresSfi,
                        borderColor: '#22c55e',
                        backgroundColor: 'rgba(34, 197, 94, 0.1)',
                        fill: true,
                        tension: 0.3,
                        pointRadius: 2,
                        borderWidth: 2,
                        yAxisID: 'y'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { labels: { color: '#c9d1d9', font: { size: 11 } } }
                },
                scales: {
                    x: {
                        ticks: { color: '#8b949e', font: { size: 9 }, maxRotation: 45 },
                        grid: { color: '#21262d' }
                    },
                    y: {
                        position: 'left',
                        title: { display: true, text: 'SFI', color: '#22c55e' },
                        ticks: { color: '#22c55e', font: { size: 10 } },
                        grid: { color: '#21262d' }
                    }
                }
            }
        });
    }

    // Helpers
    function setTexto(id, texto) {
        const elem = document.getElementById(id);
        if (elem) { elem.textContent = texto; }
    }

    function setValorConClase(id, valor, umbrales) {
        const elem = document.getElementById(id);
        if (!elem) { return; }
        elem.textContent = valor !== undefined && valor !== null ? valor : '--';
        elem.classList.remove('prop-bueno', 'prop-regular', 'prop-malo');
        const num = parseFloat(valor);
        if (isNaN(num)) { return; }
        if (umbrales.invertido) {
            if (num <= umbrales.bueno) { elem.classList.add('prop-bueno'); }
            else if (num <= umbrales.regular) { elem.classList.add('prop-regular'); }
            else { elem.classList.add('prop-malo'); }
        } else {
            if (num >= umbrales.bueno) { elem.classList.add('prop-bueno'); }
            else if (num >= umbrales.regular) { elem.classList.add('prop-regular'); }
            else { elem.classList.add('prop-malo'); }
        }
    }

    function setTextoConClase(id, texto) {
        const elem = document.getElementById(id);
        if (!elem) { return; }
        elem.textContent = texto;
        elem.classList.remove('prop-buena', 'prop-mala');
        const lower = (texto || '').toLowerCase();
        if (lower === 'band closed' || lower === 'closed' || lower === 'n/a') {
            elem.classList.add('prop-mala');
        } else {
            elem.classList.add('prop-buena');
        }
    }

    function claseCondicion(condicion) {
        if (!condicion) { return ''; }
        const c = condicion.toLowerCase();
        if (c === 'good' || c === 'buena' || c === 'excelente' || c === 'excellent') { return 'prop-buena'; }
        if (c === 'fair' || c === 'regular') { return 'prop-regular'; }
        if (c === 'poor' || c === 'pobre' || c === 'mala') { return 'prop-mala'; }
        return 'prop-cerrada';
    }

    return {
        init: init,
        cargar: cargarDatosSolares
    };
})();

document.addEventListener('DOMContentLoaded', SolarDashboard.init);
