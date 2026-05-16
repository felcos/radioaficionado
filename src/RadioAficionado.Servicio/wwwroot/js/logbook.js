'use strict';

/**
 * Modulo de Logbook — carga QSOs por AJAX y permite busqueda.
 */
const Logbook = (function () {
    let pagina = 1;
    const porPagina = 50;
    let cargando = false;

    function init() {
        const inputBuscar = document.getElementById('logbook-buscar');
        if (inputBuscar) {
            let timeout = null;
            inputBuscar.addEventListener('input', function () {
                clearTimeout(timeout);
                timeout = setTimeout(function () {
                    pagina = 1;
                    cargarQsos(inputBuscar.value.trim());
                }, 300);
            });
        }

        const btnExportar = document.getElementById('btn-exportar-adif');
        if (btnExportar) {
            btnExportar.addEventListener('click', exportarAdif);
        }

        const btnImportar = document.getElementById('btn-importar-adif');
        const inputArchivo = document.getElementById('input-archivo-adif');
        if (btnImportar && inputArchivo) {
            btnImportar.addEventListener('click', function () {
                inputArchivo.click();
            });
            inputArchivo.addEventListener('change', function () {
                if (inputArchivo.files.length > 0) {
                    importarAdif(inputArchivo.files[0]);
                    inputArchivo.value = '';
                }
            });
        }

        // Cargar QSOs al activar la pestana
        const tabLogbook = document.getElementById('tab-logbook');
        if (tabLogbook) {
            tabLogbook.addEventListener('shown.bs.tab', function () {
                if (!cargando) {
                    cargarQsos('');
                }
            });
        }
    }

    function cargarQsos(busqueda) {
        cargando = true;
        const url = '/api/logbook?pagina=' + pagina + '&porPagina=' + porPagina +
            (busqueda ? '&busqueda=' + encodeURIComponent(busqueda) : '');

        fetch(url)
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                renderizarTabla(datos.qsos || []);
                actualizarTotal(datos.total || 0);
                cargando = false;
            })
            .catch(function () {
                cargando = false;
            });
    }

    function renderizarTabla(qsos) {
        const tbody = document.getElementById('logbook-tbody');
        if (!tbody) { return; }

        tbody.innerHTML = '';

        for (let i = 0; i < qsos.length; i++) {
            const qso = qsos[i];
            const tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + (qso.fechaHora || '') + '</td>' +
                '<td><strong>' + (qso.indicativo || '') + '</strong></td>' +
                '<td>' + (qso.banda || '') + '</td>' +
                '<td>' + (qso.modo || '') + '</td>' +
                '<td>' + (qso.rstEnviado || '') + '</td>' +
                '<td>' + (qso.rstRecibido || '') + '</td>' +
                '<td>' + (qso.grid || '') + '</td>' +
                '<td>' + (qso.dxcc || '') + '</td>' +
                '<td>' + (qso.confirmado ? '&#10003;' : '') + '</td>';
            tbody.appendChild(tr);
        }
    }

    function actualizarTotal(total) {
        const elem = document.getElementById('logbook-total');
        if (elem) {
            elem.textContent = total + ' QSOs';
        }
    }

    function exportarAdif() {
        window.location.href = '/api/logbook/exportar-adif';
    }

    function importarAdif(archivo) {
        const contenedorResultado = document.getElementById('resultado-importacion');
        const btnImportar = document.getElementById('btn-importar-adif');

        if (btnImportar) {
            btnImportar.disabled = true;
            btnImportar.textContent = 'Importando...';
        }

        if (contenedorResultado) {
            contenedorResultado.style.display = 'none';
            contenedorResultado.innerHTML = '';
        }

        const formData = new FormData();
        formData.append('archivo', archivo);

        fetch('/api/logbook/importar-adif', {
            method: 'POST',
            body: formData
        })
            .then(function (resp) {
                if (!resp.ok) {
                    return resp.json().then(function (err) {
                        throw new Error(err.mensaje || 'Error al importar el archivo.');
                    });
                }
                return resp.json();
            })
            .then(function (datos) {
                if (contenedorResultado) {
                    let html = '<strong>' + datos.importados + ' QSO(s) importados</strong>';
                    if (datos.errores > 0) {
                        html += ', ' + datos.errores + ' error(es)/duplicado(s)';
                    }
                    if (datos.detalles && datos.detalles.length > 0) {
                        html += '<br/><small>' + datos.detalles.slice(0, 10).join('<br/>') + '</small>';
                        if (datos.detalles.length > 10) {
                            html += '<br/><small>... y ' + (datos.detalles.length - 10) + ' mas</small>';
                        }
                    }
                    contenedorResultado.innerHTML = html;
                    contenedorResultado.className = datos.errores > 0
                        ? 'alert alert-warning mt-2'
                        : 'alert alert-success mt-2';
                    contenedorResultado.style.display = 'block';
                }

                // Refrescar la tabla del logbook
                pagina = 1;
                const inputBuscar = document.getElementById('logbook-buscar');
                cargarQsos(inputBuscar ? inputBuscar.value.trim() : '');
            })
            .catch(function (error) {
                if (contenedorResultado) {
                    contenedorResultado.innerHTML = '<strong>Error:</strong> ' + error.message;
                    contenedorResultado.className = 'alert alert-danger mt-2';
                    contenedorResultado.style.display = 'block';
                }
            })
            .finally(function () {
                if (btnImportar) {
                    btnImportar.disabled = false;
                    btnImportar.textContent = 'Importar ADIF';
                }
            });
    }

    return {
        init: init,
        cargar: cargarQsos
    };
})();

document.addEventListener('DOMContentLoaded', Logbook.init);
