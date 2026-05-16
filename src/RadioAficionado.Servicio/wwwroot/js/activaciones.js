'use strict';

/**
 * Modulo de Activaciones POTA/SOTA — carga activaciones activas.
 */
const Activaciones = (function () {
    let cargando = false;
    let tipoActual = 'pota';

    function init() {
        const btnPota = document.getElementById('btn-pota');
        const btnSota = document.getElementById('btn-sota');
        const btnActualizar = document.getElementById('btn-activaciones-actualizar');
        const inputBuscar = document.getElementById('activaciones-buscar');

        if (btnPota) {
            btnPota.addEventListener('click', function () {
                tipoActual = 'pota';
                btnPota.classList.add('active');
                if (btnSota) { btnSota.classList.remove('active'); }
                cargarActivaciones();
            });
        }

        if (btnSota) {
            btnSota.addEventListener('click', function () {
                tipoActual = 'sota';
                btnSota.classList.add('active');
                if (btnPota) { btnPota.classList.remove('active'); }
                cargarActivaciones();
            });
        }

        if (btnActualizar) {
            btnActualizar.addEventListener('click', function () {
                cargarActivaciones();
            });
        }

        if (inputBuscar) {
            let timeout = null;
            inputBuscar.addEventListener('input', function () {
                clearTimeout(timeout);
                timeout = setTimeout(function () {
                    cargarActivaciones();
                }, 300);
            });
        }

        // Cargar al activar la pestana
        const tabActivaciones = document.getElementById('tab-activaciones');
        if (tabActivaciones) {
            tabActivaciones.addEventListener('shown.bs.tab', function () {
                if (!cargando) {
                    cargarActivaciones();
                }
            });
        }

        // Carga inicial si estamos en la pagina completa
        const panelActivaciones = document.getElementById('tabla-activaciones');
        if (panelActivaciones && !tabActivaciones) {
            cargarActivaciones();
        }
    }

    function cargarActivaciones() {
        cargando = true;
        const inputBuscar = document.getElementById('activaciones-buscar');
        const busqueda = inputBuscar ? inputBuscar.value.trim() : '';

        let url = '/api/activaciones?tipo=' + encodeURIComponent(tipoActual);
        if (busqueda) {
            url += '&busqueda=' + encodeURIComponent(busqueda);
        }

        fetch(url)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                renderizarTabla(datos.activaciones || []);
                cargando = false;
            })
            .catch(function () {
                cargando = false;
            });
    }

    function renderizarTabla(activaciones) {
        const tbody = document.getElementById('activaciones-tbody');
        if (!tbody) { return; }

        tbody.innerHTML = '';

        for (let i = 0; i < activaciones.length; i++) {
            const act = activaciones[i];
            const tr = document.createElement('tr');

            tr.innerHTML =
                '<td><strong>' + (act.referencia || '') + '</strong></td>' +
                '<td>' + (act.nombre || act.notas || '') + '</td>' +
                '<td>' + (act.activador || '') + '</td>' +
                '<td>' + (act.frecuencia || '') + '</td>' +
                '<td>' + (act.modo || '') + '</td>' +
                '<td>' + (act.spots || act.qsos || 0) + '</td>' +
                '<td>' + (act.utc || act.fechaInicio || '') + '</td>';

            tbody.appendChild(tr);
        }

        if (activaciones.length === 0) {
            const tr = document.createElement('tr');
            tr.innerHTML = '<td colspan="7" class="text-center text-muted">No hay activaciones activas</td>';
            tbody.appendChild(tr);
        }
    }

    return {
        init: init,
        cargar: cargarActivaciones
    };
})();

document.addEventListener('DOMContentLoaded', Activaciones.init);
