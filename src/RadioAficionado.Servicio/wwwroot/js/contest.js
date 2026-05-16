'use strict';

/**
 * Modulo de Contest — carga estadisticas del contest activo.
 */
const Contest = (function () {
    let cargando = false;
    let contestActual = '';

    function init() {
        const selectContest = document.getElementById('contest-seleccionar');
        const btnIniciar = document.getElementById('btn-contest-iniciar');

        if (selectContest) {
            selectContest.addEventListener('change', function () {
                contestActual = selectContest.value;
                cargarEstadisticas();
            });
        }

        if (btnIniciar) {
            btnIniciar.addEventListener('click', function () {
                if (contestActual) {
                    cargarEstadisticas();
                }
            });
        }

        // Cargar al activar la pestana
        const tabContest = document.getElementById('tab-contest');
        if (tabContest) {
            tabContest.addEventListener('shown.bs.tab', function () {
                if (!cargando) {
                    cargarEstadisticas();
                }
            });
        }

        // Carga inicial si estamos en la pagina completa
        const panelContest = document.getElementById('contest-qsos');
        if (panelContest && !tabContest) {
            cargarEstadisticas();
        }
    }

    function cargarEstadisticas() {
        cargando = true;

        let url = '/api/contest';
        if (contestActual) {
            url += '?contest=' + encodeURIComponent(contestActual);
        }

        fetch(url)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                actualizarEstadisticas(datos);
                cargando = false;
            })
            .catch(function () {
                cargando = false;
            });
    }

    function actualizarEstadisticas(datos) {
        const elemQsos = document.getElementById('contest-qsos');
        const elemMultiplicadores = document.getElementById('contest-multiplicadores');
        const elemPuntos = document.getElementById('contest-puntos');
        const elemScore = document.getElementById('contest-score');
        const elemRate = document.getElementById('contest-rate');

        if (elemQsos) { elemQsos.textContent = datos.qsos || 0; }
        if (elemMultiplicadores) { elemMultiplicadores.textContent = datos.multiplicadores || 0; }
        if (elemPuntos) { elemPuntos.textContent = datos.puntos || 0; }
        if (elemScore) { elemScore.textContent = formatearNumero(datos.score || 0); }
        if (elemRate) { elemRate.textContent = datos.rate || 0; }
    }

    function formatearNumero(num) {
        return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    }

    return {
        init: init,
        cargar: cargarEstadisticas
    };
})();

document.addEventListener('DOMContentLoaded', Contest.init);
