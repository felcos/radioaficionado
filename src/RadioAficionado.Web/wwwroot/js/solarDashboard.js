'use strict';

/**
 * Modulo de Dashboard Solar — datos en tiempo real de NOAA.
 * Carga datos directamente de las APIs publicas de NOAA (sin backend).
 */
const SolarDashboard = (function () {
    let graficoHistorico = null;
    let graficoBandas = null;

    // URLs de las APIs publicas de NOAA
    const URL_SOLAR_WIND = 'https://services.swpc.noaa.gov/products/summary/solar-wind-mag-field.json';
    const URL_SOLAR_WIND_SPEED = 'https://services.swpc.noaa.gov/products/summary/solar-wind-speed.json';
    const URL_10CM_FLUX = 'https://services.swpc.noaa.gov/products/summary/10cm-flux.json';
    const URL_SUNSPOT = 'https://services.swpc.noaa.gov/json/solar-cycle/sunspots.json';
    const URL_KP_1MIN = 'https://services.swpc.noaa.gov/products/noaa-estimated-planetary-k-index-1-minute.json';
    const URL_XRAY = 'https://services.swpc.noaa.gov/json/goes/primary/xray-flares-latest.json';
    const URL_AP = 'https://services.swpc.noaa.gov/products/noaa-planetary-k-index.json';
    const URL_SFI_HISTORICO = 'https://services.swpc.noaa.gov/json/f107_cm_flux.json';
    const URL_ALERTS = 'https://services.swpc.noaa.gov/products/alerts.json';

    // Bandas HF y sus frecuencias criticas aproximadas para condiciones
    const bandasHf = [
        { nombre: '80m-40m', mufMin: 3.5, mufMax: 7.3 },
        { nombre: '30m-20m', mufMin: 10.1, mufMax: 14.35 },
        { nombre: '17m-15m', mufMin: 18.068, mufMax: 21.45 },
        { nombre: '12m-10m', mufMin: 24.89, mufMax: 29.7 }
    ];

    function init() {
        cargarTodo();

        const btnActualizar = document.getElementById('btn-actualizar-solar');
        if (btnActualizar) {
            btnActualizar.addEventListener('click', function () {
                cargarTodo();
            });
        }
    }

    function cargarTodo() {
        cargarFlujySfi();
        cargarVientoSolar();
        cargarKp();
        cargarHistorico();
        cargarAlertas();
    }

    function cargarFlujySfi() {
        fetch(URL_10CM_FLUX)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                const sfi = datos.Flux || '--';
                setValorConClase('solar-sfi', sfi, { bueno: 150, regular: 90, invertido: false });
                setTexto('solar-actualizacion', datos.TimeStamp ? 'Actualizado: ' + datos.TimeStamp + ' UTC' : '');
                estimarCondicionesBandas(parseFloat(sfi));
            })
            .catch(function () {
                setTexto('solar-sfi', '--');
            });
    }

    function cargarVientoSolar() {
        Promise.all([
            fetch(URL_SOLAR_WIND).then(function (r) { return r.json(); }),
            fetch(URL_SOLAR_WIND_SPEED).then(function (r) { return r.json(); })
        ]).then(function (resultados) {
            const mag = resultados[0];
            const vel = resultados[1];

            const bt = parseFloat(mag.Bt) || 0;
            const bz = parseFloat(mag.Bz) || 0;
            const velocidad = parseFloat(vel.WindSpeed) || 0;

            setTexto('solar-viento-vel', Math.round(velocidad) + ' km/s');
            const elemVel = document.getElementById('solar-viento-vel');
            if (elemVel) {
                elemVel.classList.remove('text-success', 'text-warning', 'text-danger');
                if (velocidad < 400) { elemVel.classList.add('text-success'); }
                else if (velocidad < 600) { elemVel.classList.add('text-warning'); }
                else { elemVel.classList.add('text-danger'); }
            }

            setTexto('solar-bt', bt.toFixed(1) + ' nT');
            setTexto('solar-bz', bz.toFixed(1) + ' nT');
            const elemBz = document.getElementById('solar-bz');
            if (elemBz) {
                elemBz.classList.remove('text-success', 'text-warning', 'text-danger');
                if (bz <= -5) { elemBz.classList.add('text-danger'); }
                else if (bz < 0) { elemBz.classList.add('text-warning'); }
                else { elemBz.classList.add('text-success'); }
            }
        }).catch(function () {
            setTexto('solar-viento-vel', '--');
            setTexto('solar-bt', '--');
            setTexto('solar-bz', '--');
        });
    }

    function cargarKp() {
        fetch(URL_AP)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                if (datos.length > 1) {
                    const ultimo = datos[datos.length - 1];
                    const kp = ultimo[1] || '--';
                    const ap = ultimo[2] || '--';
                    setValorConClase('solar-kp', kp, { bueno: 2, regular: 4, invertido: true });
                    setValorConClase('solar-a', ap, { bueno: 10, regular: 30, invertido: true });
                }
            })
            .catch(function () {
                setTexto('solar-kp', '--');
                setTexto('solar-a', '--');
            });
    }

    function estimarCondicionesBandas(sfi) {
        const tbody = document.getElementById('solar-hf-tbody');
        if (!tbody) { return; }
        tbody.innerHTML = '';

        for (let i = 0; i < bandasHf.length; i++) {
            const b = bandasHf[i];
            let condDia = 'Pobre';
            let condNoche = 'Cerrada';

            if (sfi >= 150) {
                condDia = 'Buena';
                condNoche = b.mufMin < 15 ? 'Buena' : 'Regular';
            } else if (sfi >= 100) {
                condDia = b.mufMin < 20 ? 'Buena' : 'Regular';
                condNoche = b.mufMin < 10 ? 'Buena' : (b.mufMin < 15 ? 'Regular' : 'Pobre');
            } else if (sfi >= 70) {
                condDia = b.mufMin < 15 ? 'Buena' : (b.mufMin < 22 ? 'Regular' : 'Pobre');
                condNoche = b.mufMin < 8 ? 'Regular' : 'Pobre';
            } else {
                condDia = b.mufMin < 10 ? 'Regular' : 'Pobre';
                condNoche = b.mufMin < 5 ? 'Pobre' : 'Cerrada';
            }

            const tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + b.nombre + '</td>' +
                '<td class="' + claseCondicion(condDia) + '">' + condDia + '</td>' +
                '<td class="' + claseCondicion(condNoche) + '">' + condNoche + '</td>';
            tbody.appendChild(tr);
        }

        actualizarGraficoBandas();
    }

    function actualizarGraficoBandas() {
        const canvas = document.getElementById('grafico-propagacion');
        if (!canvas || typeof Chart === 'undefined') { return; }

        const tbody = document.getElementById('solar-hf-tbody');
        if (!tbody) { return; }

        const etiquetas = [];
        const datosDia = [];
        const datosNoche = [];
        const filas = tbody.querySelectorAll('tr');

        for (let i = 0; i < filas.length; i++) {
            const celdas = filas[i].querySelectorAll('td');
            if (celdas.length >= 3) {
                etiquetas.push(celdas[0].textContent);
                datosDia.push(condicionANumero(celdas[1].textContent));
                datosNoche.push(condicionANumero(celdas[2].textContent));
            }
        }

        if (graficoBandas) {
            graficoBandas.destroy();
            graficoBandas = null;
        }

        const contexto = canvas.getContext('2d');
        graficoBandas = new Chart(contexto, {
            type: 'bar',
            data: {
                labels: etiquetas,
                datasets: [
                    {
                        label: 'Dia',
                        data: datosDia,
                        backgroundColor: 'rgba(34, 197, 94, 0.7)',
                        borderColor: 'rgba(34, 197, 94, 1)',
                        borderWidth: 1
                    },
                    {
                        label: 'Noche',
                        data: datosNoche,
                        backgroundColor: 'rgba(59, 130, 246, 0.7)',
                        borderColor: 'rgba(59, 130, 246, 1)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { labels: { color: '#c9d1d9', font: { size: 11 } } },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const mapa = { 0: 'Cerrada', 1: 'Pobre', 2: 'Regular', 3: 'Buena' };
                                return ctx.dataset.label + ': ' + (mapa[ctx.raw] || ctx.raw);
                            }
                        }
                    }
                },
                scales: {
                    x: { ticks: { color: '#8b949e', font: { size: 10 } }, grid: { color: '#21262d' } },
                    y: {
                        min: 0, max: 3,
                        ticks: {
                            stepSize: 1, color: '#8b949e', font: { size: 10 },
                            callback: function (v) {
                                const mapa = { 0: 'Cerrada', 1: 'Pobre', 2: 'Regular', 3: 'Buena' };
                                return mapa[v] || v;
                            }
                        },
                        grid: { color: '#21262d' }
                    }
                }
            }
        });
    }

    function cargarHistorico() {
        fetch(URL_SFI_HISTORICO)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                // Tomar los ultimos 30 registros
                const ultimos = datos.slice(-30);
                renderizarGraficoHistorico(ultimos);
            })
            .catch(function () {
                console.error('Error cargando historico SFI');
            });
    }

    function renderizarGraficoHistorico(datos) {
        const canvas = document.getElementById('grafico-historico-solar');
        if (!canvas || typeof Chart === 'undefined') { return; }

        if (graficoHistorico) {
            graficoHistorico.destroy();
            graficoHistorico = null;
        }

        const etiquetas = [];
        const valores = [];
        for (let i = 0; i < datos.length; i++) {
            const d = datos[i];
            const fecha = d.time_tag || d.fecha || '';
            etiquetas.push(fecha.length >= 10 ? fecha.substring(5, 10) : fecha);
            valores.push(d.flux || d.sfi || 0);
        }

        const contexto = canvas.getContext('2d');
        graficoHistorico = new Chart(contexto, {
            type: 'line',
            data: {
                labels: etiquetas,
                datasets: [{
                    label: 'SFI (30 dias)',
                    data: valores,
                    borderColor: '#22c55e',
                    backgroundColor: 'rgba(34, 197, 94, 0.1)',
                    fill: true,
                    tension: 0.3,
                    pointRadius: 2,
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: { legend: { labels: { color: '#c9d1d9', font: { size: 11 } } } },
                scales: {
                    x: { ticks: { color: '#8b949e', font: { size: 9 }, maxRotation: 45 }, grid: { color: '#21262d' } },
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

    function cargarAlertas() {
        fetch(URL_ALERTS)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                const contenedor = document.getElementById('solar-alertas');
                if (!contenedor) { return; }
                contenedor.innerHTML = '';

                if (!datos || datos.length === 0) {
                    contenedor.innerHTML = '<div class="text-muted">Sin alertas activas</div>';
                    return;
                }

                const limite = Math.min(datos.length, 5);
                for (let i = 0; i < limite; i++) {
                    const alerta = datos[i];
                    const div = document.createElement('div');
                    div.className = 'mb-2 p-2 border border-warning rounded small';
                    const mensaje = alerta.message || alerta.producto || '';
                    const primeraLinea = mensaje.split('\n')[0] || 'Alerta NOAA';

                    // Construir con nodos de texto: los datos provienen de NOAA (externos),
                    // usar textContent evita inyeccion de HTML/script (XSS).
                    const badge = document.createElement('span');
                    badge.className = 'badge bg-warning text-dark me-1';
                    badge.textContent = alerta.product_id || 'NOAA';

                    const texto = document.createElement('span');
                    texto.textContent = primeraLinea.substring(0, 120);

                    div.appendChild(badge);
                    div.appendChild(document.createTextNode(' '));
                    div.appendChild(texto);
                    contenedor.appendChild(div);
                }
            })
            .catch(function () {
                const contenedor = document.getElementById('solar-alertas');
                if (contenedor) {
                    contenedor.innerHTML = '<div class="text-muted">No se pudieron cargar las alertas</div>';
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
        elem.classList.remove('text-success', 'text-warning', 'text-danger');
        const num = parseFloat(valor);
        if (isNaN(num)) { return; }
        if (umbrales.invertido) {
            if (num <= umbrales.bueno) { elem.classList.add('text-success'); }
            else if (num <= umbrales.regular) { elem.classList.add('text-warning'); }
            else { elem.classList.add('text-danger'); }
        } else {
            if (num >= umbrales.bueno) { elem.classList.add('text-success'); }
            else if (num >= umbrales.regular) { elem.classList.add('text-warning'); }
            else { elem.classList.add('text-danger'); }
        }
    }

    function condicionANumero(condicion) {
        if (!condicion) { return 0; }
        const c = condicion.toLowerCase();
        if (c === 'buena' || c === 'excelente') { return 3; }
        if (c === 'regular') { return 2; }
        if (c === 'pobre' || c === 'mala') { return 1; }
        return 0;
    }

    function claseCondicion(condicion) {
        if (!condicion) { return ''; }
        const c = condicion.toLowerCase();
        if (c === 'buena' || c === 'excelente') { return 'text-success'; }
        if (c === 'regular') { return 'text-warning'; }
        if (c === 'pobre' || c === 'mala') { return 'text-danger'; }
        return 'text-secondary';
    }

    return {
        init: init,
        cargar: cargarTodo
    };
})();

document.addEventListener('DOMContentLoaded', SolarDashboard.init);
