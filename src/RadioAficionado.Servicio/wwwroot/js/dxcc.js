'use strict';

/**
 * Modulo de DXCC Tracking — carga entidades DXCC y su estado por banda.
 */
const Dxcc = (function () {
    let cargando = false;
    let continenteActual = '';

    function init() {
        const filtroBanda = document.getElementById('dxcc-filtro-banda');
        const filtroEstado = document.getElementById('dxcc-filtro-estado');

        if (filtroBanda) {
            filtroBanda.addEventListener('change', function () {
                cargarEntidades();
            });
        }

        if (filtroEstado) {
            filtroEstado.addEventListener('change', function () {
                cargarEntidades();
            });
        }

        // Filtro por continente
        const botonesContinente = document.querySelectorAll('.btn-continente');
        for (let i = 0; i < botonesContinente.length; i++) {
            botonesContinente[i].addEventListener('click', function () {
                const valor = this.getAttribute('data-continente') || '';
                continenteActual = (continenteActual === valor) ? '' : valor;

                for (let j = 0; j < botonesContinente.length; j++) {
                    botonesContinente[j].classList.remove('active');
                }
                if (continenteActual) {
                    this.classList.add('active');
                }

                cargarEntidades();
            });
        }

        // Cargar al activar la pestana
        const tabDxcc = document.getElementById('tab-dxcc');
        if (tabDxcc) {
            tabDxcc.addEventListener('shown.bs.tab', function () {
                if (!cargando) {
                    cargarEntidades();
                }
            });
        }

        // Carga inicial si estamos en la pagina completa
        const panelDxcc = document.getElementById('tabla-dxcc');
        if (panelDxcc && !tabDxcc) {
            cargarEntidades();
        }
    }

    function cargarEntidades() {
        cargando = true;
        const filtroBanda = document.getElementById('dxcc-filtro-banda');
        const filtroEstado = document.getElementById('dxcc-filtro-estado');

        let url = '/api/dxcc';
        const parametros = [];

        if (filtroBanda && filtroBanda.value) {
            parametros.push('banda=' + encodeURIComponent(filtroBanda.value));
        }
        if (filtroEstado && filtroEstado.value) {
            parametros.push('estado=' + encodeURIComponent(filtroEstado.value));
        }
        if (parametros.length > 0) {
            url += '?' + parametros.join('&');
        }

        fetch(url)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                actualizarResumen(datos.trabajados || 0, datos.confirmados || 0);
                renderizarTabla(datos.entidades || []);
                cargando = false;
            })
            .catch(function () {
                cargando = false;
            });
    }

    function actualizarResumen(trabajados, confirmados) {
        const elemTrabajados = document.getElementById('dxcc-trabajados');
        const elemConfirmados = document.getElementById('dxcc-confirmados');

        if (elemTrabajados) {
            elemTrabajados.textContent = trabajados;
        }
        if (elemConfirmados) {
            elemConfirmados.textContent = confirmados;
        }

        const progresoRelleno = document.getElementById('dxcc-progreso-relleno');
        const progresoTexto = document.getElementById('dxcc-progreso-texto');
        const total = 340; // meta DXCC
        if (progresoRelleno && progresoTexto) {
            const porcentaje = Math.round((trabajados / total) * 100);
            progresoRelleno.style.width = porcentaje + '%';
            progresoTexto.textContent = porcentaje + '%';
        }
    }

    function renderizarTabla(entidades) {
        const tbody = document.getElementById('dxcc-tbody');
        if (!tbody) { return; }

        tbody.innerHTML = '';

        for (let i = 0; i < entidades.length; i++) {
            const ent = entidades[i];

            if (continenteActual && ent.continente !== continenteActual) {
                continue;
            }

            const bandas = ent.bandas || {};
            const tr = document.createElement('tr');

            tr.innerHTML =
                '<td>' + (ent.numero || '') + '</td>' +
                '<td>' + (ent.nombre || '') + '</td>' +
                '<td><strong>' + (ent.prefijo || '') + '</strong></td>' +
                '<td>' + (ent.continente || '') + '</td>' +
                celdasBanda(bandas, '160m') +
                celdasBanda(bandas, '80m') +
                celdasBanda(bandas, '40m') +
                celdasBanda(bandas, '20m') +
                celdasBanda(bandas, '15m') +
                celdasBanda(bandas, '10m');

            tbody.appendChild(tr);
        }
    }

    function celdasBanda(bandas, banda) {
        const estado = bandas[banda] || 'necesitado';
        let clase = '';
        let simbolo = '';

        if (estado === 'confirmado') {
            clase = 'dxcc-banda-confirmado';
            simbolo = '&#10003;&#10003;';
        } else if (estado === 'trabajado') {
            clase = 'dxcc-banda-trabajado';
            simbolo = '&#10003;';
        } else {
            clase = 'dxcc-banda-necesitado';
            simbolo = '&middot;';
        }

        return '<td class="text-center ' + clase + '">' + simbolo + '</td>';
    }

    return {
        init: init,
        cargar: cargarEntidades
    };
})();

document.addEventListener('DOMContentLoaded', Dxcc.init);
