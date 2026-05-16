'use strict';

/**
 * Modulo de Propagacion — carga indices solares y condiciones por banda.
 */
const Propagacion = (function () {
    let cargando = false;

    function init() {
        const btnActualizar = document.getElementById('btn-actualizar-propagacion');
        if (btnActualizar) {
            btnActualizar.addEventListener('click', function () {
                cargarPropagacion();
            });
        }

        // Cargar al activar la pestana
        const tabPropagacion = document.getElementById('tab-propagacion');
        if (tabPropagacion) {
            tabPropagacion.addEventListener('shown.bs.tab', function () {
                if (!cargando) {
                    cargarPropagacion();
                }
            });
        }

        // Carga inicial si estamos en la pagina completa
        const panelPropagacion = document.getElementById('tabla-propagacion');
        if (panelPropagacion && !tabPropagacion) {
            cargarPropagacion();
        }
    }

    function cargarPropagacion() {
        cargando = true;

        fetch('/api/propagacion')
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                actualizarIndices(datos);
                renderizarBandas(datos.bandas || []);
                actualizarTimestamp(datos.actualizacion || '');
                cargando = false;
            })
            .catch(function () {
                cargando = false;
            });
    }

    /**
     * Remueve las clases prop-bueno/prop-regular/prop-malo y aplica la correcta.
     * @param {HTMLElement} elemento - Elemento DOM al que aplicar la clase.
     * @param {number} valor - Valor numerico del indice.
     * @param {object} umbrales - { bueno, regular, invertido }.
     *   Si invertido=true, valores bajos son buenos (A-Index, K-Index).
     */
    function aplicarClaseIndice(elemento, valor, umbrales) {
        if (!elemento) { return; }

        elemento.classList.remove('prop-bueno', 'prop-regular', 'prop-malo');

        if (isNaN(valor)) { return; }

        let clase = '';
        if (umbrales.invertido) {
            // Menor es mejor (A-Index, K-Index)
            if (valor <= umbrales.bueno) {
                clase = 'prop-bueno';
            } else if (valor <= umbrales.regular) {
                clase = 'prop-regular';
            } else {
                clase = 'prop-malo';
            }
        } else {
            // Mayor es mejor (SFI, SN)
            if (valor >= umbrales.bueno) {
                clase = 'prop-bueno';
            } else if (valor >= umbrales.regular) {
                clase = 'prop-regular';
            } else {
                clase = 'prop-malo';
            }
        }

        elemento.classList.add(clase);
    }

    function actualizarIndices(datos) {
        const elemSfi = document.getElementById('prop-sfi');
        const elemSn = document.getElementById('prop-sn');
        const elemA = document.getElementById('prop-a');
        const elemK = document.getElementById('prop-k');

        if (elemSfi) { elemSfi.textContent = datos.sfi || '--'; }
        if (elemSn) { elemSn.textContent = datos.sn || '--'; }
        if (elemA) { elemA.textContent = datos.a || '--'; }
        if (elemK) { elemK.textContent = datos.k || '--'; }

        aplicarClaseIndice(elemSfi, parseFloat(datos.sfi), { bueno: 150, regular: 90, invertido: false });
        aplicarClaseIndice(elemSn, parseFloat(datos.sn), { bueno: 100, regular: 50, invertido: false });
        aplicarClaseIndice(elemA, parseFloat(datos.a), { bueno: 10, regular: 30, invertido: true });
        aplicarClaseIndice(elemK, parseFloat(datos.k), { bueno: 2, regular: 4, invertido: true });
    }

    function actualizarTimestamp(actualizacion) {
        const elem = document.getElementById('propagacion-actualizacion');
        if (elem) {
            elem.textContent = actualizacion ? 'Actualizado: ' + actualizacion : 'Sin datos';
        }
    }

    function renderizarBandas(bandas) {
        const tbody = document.getElementById('propagacion-tbody');
        if (!tbody) { return; }

        tbody.innerHTML = '';

        for (let i = 0; i < bandas.length; i++) {
            const b = bandas[i];
            const tr = document.createElement('tr');

            tr.innerHTML =
                '<td>' + (b.banda || '') + '</td>' +
                '<td class="' + claseCondicion(b.dia) + '">' + (b.dia || '') + '</td>' +
                '<td class="' + claseCondicion(b.noche) + '">' + (b.noche || '') + '</td>';

            tbody.appendChild(tr);
        }
    }

    function claseCondicion(condicion) {
        if (!condicion) { return ''; }
        const cond = condicion.toLowerCase();
        if (cond === 'buena' || cond === 'excelente') { return 'prop-buena'; }
        if (cond === 'regular') { return 'prop-regular'; }
        return 'prop-mala';
    }

    return {
        init: init,
        cargar: cargarPropagacion
    };
})();

document.addEventListener('DOMContentLoaded', Propagacion.init);
