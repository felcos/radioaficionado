'use strict';

/**
 * Modulo de Satelites — carga proximos pases de satelites amateur.
 */
const Satelites = (function () {
    let cargando = false;
    let sateliteActual = '';

    function init() {
        const selectSatelite = document.getElementById('satelites-seleccionar');
        const btnActualizarTle = document.getElementById('btn-sat-actualizar-tle');

        if (selectSatelite) {
            selectSatelite.addEventListener('change', function () {
                sateliteActual = selectSatelite.value;
                cargarPases();
            });
        }

        if (btnActualizarTle) {
            btnActualizarTle.addEventListener('click', function () {
                cargarPases();
            });
        }

        // Cargar al activar la pestana
        const tabSatelites = document.getElementById('tab-satelites');
        if (tabSatelites) {
            tabSatelites.addEventListener('shown.bs.tab', function () {
                if (!cargando) {
                    cargarPases();
                }
            });
        }

        // Carga inicial si estamos en la pagina completa
        const panelSatelites = document.getElementById('tabla-pases');
        if (panelSatelites && !tabSatelites) {
            cargarPases();
        }
    }

    function cargarPases() {
        cargando = true;

        let url = '/api/satelites';
        if (sateliteActual) {
            url += '?satelite=' + encodeURIComponent(sateliteActual);
        }

        fetch(url)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                renderizarPases(datos.pases || []);
                actualizarInfo(datos.pases || []);
                cargando = false;
            })
            .catch(function () {
                cargando = false;
            });
    }

    function renderizarPases(pases) {
        const tbody = document.getElementById('pases-tbody');
        if (!tbody) { return; }

        tbody.innerHTML = '';

        for (let i = 0; i < pases.length; i++) {
            const paso = pases[i];
            const tr = document.createElement('tr');

            const aosHora = extraerHora(paso.aos);
            const losHora = extraerHora(paso.los);
            const doppler = calcularDopplerIndicativo(paso.elevacionMaxima);

            tr.innerHTML =
                '<td>' + (paso.satelite || '') + '</td>' +
                '<td>' + aosHora + '</td>' +
                '<td>' + losHora + '</td>' +
                '<td>' + (paso.elevacionMaxima || 0) + '&deg;</td>' +
                '<td>' + doppler + '</td>';

            if (paso.altaElevacion) {
                tr.classList.add('table-success');
            }

            tbody.appendChild(tr);
        }

        if (pases.length === 0) {
            const tr = document.createElement('tr');
            tr.innerHTML = '<td colspan="5" class="text-center text-muted">Seleccione un satelite para ver los pases</td>';
            tbody.appendChild(tr);
        }
    }

    function actualizarInfo(pases) {
        const infoDetalle = document.getElementById('sat-info-detalle');
        if (!infoDetalle) { return; }

        if (pases.length === 0) {
            infoDetalle.innerHTML = '<p class="text-muted">Seleccione un satelite para ver los pases.</p>';
            return;
        }

        const proximo = pases[0];
        const duracion = proximo.duracionSegundos || 0;
        const minutos = Math.floor(duracion / 60);
        const segundos = duracion % 60;

        infoDetalle.innerHTML =
            '<p><strong>Proximo pase:</strong> ' + (proximo.satelite || '') + '</p>' +
            '<p><strong>AOS:</strong> ' + (proximo.aos || '') + ' (Az ' + (proximo.azimutAos || 0) + '&deg;)</p>' +
            '<p><strong>LOS:</strong> ' + (proximo.los || '') + ' (Az ' + (proximo.azimutLos || 0) + '&deg;)</p>' +
            '<p><strong>Elevacion max:</strong> ' + (proximo.elevacionMaxima || 0) + '&deg;</p>' +
            '<p><strong>Duracion:</strong> ' + minutos + 'm ' + segundos + 's</p>' +
            (proximo.altaElevacion ? '<p class="text-success"><strong>Pase de alta elevacion — ideal para contactos</strong></p>' : '');
    }

    function extraerHora(fechaStr) {
        if (!fechaStr) { return ''; }
        const partes = fechaStr.split(' ');
        return partes.length > 1 ? partes[1] : fechaStr;
    }

    function calcularDopplerIndicativo(elevacion) {
        // Indicacion cualitativa del efecto Doppler basado en la elevacion
        if (elevacion >= 60) { return 'Alto'; }
        if (elevacion >= 30) { return 'Medio'; }
        return 'Bajo';
    }

    return {
        init: init,
        cargar: cargarPases
    };
})();

document.addEventListener('DOMContentLoaded', Satelites.init);
