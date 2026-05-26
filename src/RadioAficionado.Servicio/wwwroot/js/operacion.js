'use strict';

/**
 * Modulo principal de operacion.
 * Conecta SignalR a los 4 hubs y enlaza la UI.
 */
const Operacion = (function () {
    // ================================================================
    // CONEXIONES SIGNALR
    // ================================================================

    let hubRig = null;
    let hubWaterfall = null;
    let hubDecodificaciones = null;
    let hubEstado = null;
    let waterfall = null;

    // Estado local
    let conectadoAlRig = false;
    let enableTx = false;
    let autoSeq = false;
    let frecuenciaActualHz = 14074000;
    let modoActual = 'FT8';

    // ================================================================
    // INICIALIZACION
    // ================================================================

    function iniciar() {
        // Crear waterfall
        const canvas = document.getElementById('waterfall-canvas');
        const ejeX = document.getElementById('waterfall-eje-x');
        if (canvas && ejeX) {
            waterfall = new Waterfall(canvas, ejeX);
            waterfall.onFrecuenciaSeleccionada = function (frecHz) {
                const inputTxFreq = document.getElementById('tx-frecuencia');
                if (inputTxFreq) {
                    inputTxFreq.value = frecHz;
                }
            };
        }

        // Conectar hubs SignalR
        conectarHubs();

        // Enlazar UI
        enlazarBarraRig();
        enlazarPanelTx();
        enlazarPanelQso();
        enlazarConfiguracion();

        // Reloj UTC
        actualizarRelojUtc();
        setInterval(actualizarRelojUtc, 1000);

        // Atajos de teclado
        document.addEventListener('keydown', manejarAtajoTeclado);
    }

    // ================================================================
    // SIGNALR
    // ================================================================

    function conectarHubs() {
        // Hub Rig
        hubRig = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/rig')
            .withAutomaticReconnect()
            .build();

        hubRig.on('RecibirEstadoRig', actualizarEstadoRig);
        hubRig.on('RecibirConexionCambiada', actualizarConexion);
        hubRig.start().catch(function (err) { console.error('HubRig:', err); });

        // Hub Waterfall
        hubWaterfall = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/waterfall')
            .withAutomaticReconnect()
            .build();

        hubWaterfall.on('RecibirLineaEspectro', function (linea) {
            if (waterfall && linea.magnitudesDb) {
                const magnitudes = new Uint8Array(linea.magnitudesDb);
                waterfall.agregarLinea(magnitudes, linea.resolucionHz, linea.frecuenciaMinHz);
            }
        });
        hubWaterfall.start().catch(function (err) { console.error('HubWaterfall:', err); });

        // Hub Decodificaciones
        hubDecodificaciones = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/decodificaciones')
            .withAutomaticReconnect()
            .build();

        hubDecodificaciones.on('RecibirMensaje', agregarDecodificacion);
        hubDecodificaciones.start().catch(function (err) { console.error('HubDecodificaciones:', err); });

        // Hub Estado
        hubEstado = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/estado')
            .withAutomaticReconnect()
            .build();

        hubEstado.on('RecibirSpotDx', function (spot) {
            console.log('Spot DX:', spot.dx, spot.frecuenciaHz);
        });
        hubEstado.on('RecibirNotificacion', function (tipo, mensaje) {
            console.log('Notificacion:', tipo, mensaje);
        });
        hubEstado.start().catch(function (err) { console.error('HubEstado:', err); });
    }

    // ================================================================
    // ACTUALIZACION DE UI
    // ================================================================

    function actualizarEstadoRig(estado) {
        // Guardar frecuencia y modo actuales para registro de QSOs
        frecuenciaActualHz = estado.frecuenciaHz || 14074000;
        modoActual = estado.modo || 'FT8';

        // Frecuencia
        const freqDisplay = document.getElementById('frecuencia-display');
        if (freqDisplay) { freqDisplay.textContent = estado.frecuenciaDisplay; }

        // S-meter
        const smeterRelleno = document.getElementById('smeter-relleno');
        const smeterValor = document.getElementById('smeter-valor');
        if (smeterRelleno) { smeterRelleno.style.width = estado.nivelSenalPorcentaje + '%'; }
        if (smeterValor) { smeterValor.textContent = 'S' + estado.nivelSenal; }

        // PTT
        const btnPtt = document.getElementById('btn-ptt');
        if (btnPtt) {
            if (estado.transmitiendo) {
                btnPtt.classList.add('transmitiendo');
            } else {
                btnPtt.classList.remove('transmitiendo');
            }
        }

        // SWR y ALC (solo visibles durante transmision)
        const swrContenedor = document.getElementById('swr-contenedor');
        const alcContenedor = document.getElementById('alc-contenedor');
        const swrRelleno = document.getElementById('swr-relleno');
        const swrValor = document.getElementById('swr-valor');
        const alcRelleno = document.getElementById('alc-relleno');
        const alcValor = document.getElementById('alc-valor');

        if (estado.transmitiendo) {
            if (swrContenedor) { swrContenedor.classList.add('visible'); }
            if (alcContenedor) { alcContenedor.classList.add('visible'); }

            // SWR: rango 1.0 a 3.0+ — mapear a 0-100%
            if (estado.swr > 0) {
                const swrPct = Math.min(((estado.swr - 1.0) / 2.0) * 100, 100);
                if (swrRelleno) { swrRelleno.style.width = swrPct + '%'; }
                if (swrValor) { swrValor.textContent = estado.swr.toFixed(1) + ':1'; }
            } else {
                if (swrRelleno) { swrRelleno.style.width = '0%'; }
                if (swrValor) { swrValor.textContent = '--'; }
            }

            // ALC: ya viene como porcentaje 0-100
            if (estado.alc > 0) {
                if (alcRelleno) { alcRelleno.style.width = estado.alc + '%'; }
                if (alcValor) { alcValor.textContent = Math.round(estado.alc) + '%'; }
            } else {
                if (alcRelleno) { alcRelleno.style.width = '0%'; }
                if (alcValor) { alcValor.textContent = '--'; }
            }
        } else {
            if (swrContenedor) { swrContenedor.classList.remove('visible'); }
            if (alcContenedor) { alcContenedor.classList.remove('visible'); }
        }

        // VFO
        const btnVfo = document.getElementById('btn-vfo');
        if (btnVfo) { btnVfo.textContent = 'VFO ' + estado.vfoActivo; }

        // Banda activa
        const bandas = document.querySelectorAll('.btn-banda');
        bandas.forEach(function (btn) {
            if (btn.dataset.banda === estado.banda) {
                btn.classList.add('activa');
            } else {
                btn.classList.remove('activa');
            }
        });

        // Modo activo
        const modos = document.querySelectorAll('.btn-modo');
        modos.forEach(function (btn) {
            if (btn.dataset.modo === estado.modo) {
                btn.classList.add('activa');
            } else {
                btn.classList.remove('activa');
            }
        });
    }

    function actualizarConexion(conectado, detalle) {
        conectadoAlRig = conectado;
        const indicador = document.getElementById('indicador-conexion');
        const texto = document.getElementById('texto-conexion');
        const btnConectar = document.getElementById('btn-conectar-rig');
        const btnDesconectar = document.getElementById('btn-desconectar-rig');

        if (indicador) {
            if (conectado) {
                indicador.classList.add('conectado');
            } else {
                indicador.classList.remove('conectado');
            }
        }
        if (texto) { texto.textContent = detalle; }
        if (btnConectar) { btnConectar.disabled = conectado; }
        if (btnDesconectar) { btnDesconectar.disabled = !conectado; }

        const configEstado = document.getElementById('config-estado');
        if (configEstado) { configEstado.textContent = detalle; }
    }

    function agregarDecodificacion(mensaje) {
        const tbody = document.getElementById('tbody-decodificaciones');
        if (!tbody) { return; }

        const tr = document.createElement('tr');
        tr.style.color = mensaje.colorIndicativo;

        const hora = new Date(mensaje.marcaDeTiempo);
        const utc = hora.getUTCHours().toString().padStart(2, '0') + ':' +
                     hora.getUTCMinutes().toString().padStart(2, '0') + ':' +
                     hora.getUTCSeconds().toString().padStart(2, '0');

        tr.innerHTML =
            '<td>' + utc + '</td>' +
            '<td>' + mensaje.snr + '</td>' +
            '<td>' + mensaje.deltaTiempo.toFixed(1) + '</td>' +
            '<td>' + mensaje.frecuenciaAudioHz + '</td>' +
            '<td>' + escapeHtml(mensaje.texto) + '</td>';

        // Click para seleccionar DX
        tr.addEventListener('click', function () {
            seleccionarDx(mensaje);
        });

        tbody.appendChild(tr);

        // Auto-scroll
        const scroll = document.getElementById('tabla-decodificaciones-scroll');
        if (scroll) {
            scroll.scrollTop = scroll.scrollHeight;
        }
    }

    function seleccionarDx(mensaje) {
        if (mensaje.indicativoEmisor) {
            const inputDxCall = document.getElementById('qso-dx-call');
            if (inputDxCall) { inputDxCall.value = mensaje.indicativoEmisor; }
        }
        if (mensaje.localizador) {
            const inputGrid = document.getElementById('qso-grid');
            if (inputGrid) { inputGrid.value = mensaje.localizador; }
        }
        if (mensaje.reporteSenal) {
            const inputRptRcvd = document.getElementById('qso-rpt-rcvd');
            if (inputRptRcvd) { inputRptRcvd.value = mensaje.reporteSenal; }
        }

        // Marcar fila seleccionada
        const filas = document.querySelectorAll('#tbody-decodificaciones tr');
        filas.forEach(function (f) { f.classList.remove('seleccionada'); });
        // La ultima fila clickeada se marca (evento ya disparado)
    }

    // ================================================================
    // ENLAZAR BARRA RIG
    // ================================================================

    function enlazarBarraRig() {
        // Bandas
        document.querySelectorAll('.btn-banda').forEach(function (btn) {
            btn.addEventListener('click', function () {
                if (hubRig && hubRig.state === signalR.HubConnectionState.Connected) {
                    hubRig.invoke('CambiarBanda', btn.dataset.banda);
                }
            });
        });

        // Modos
        document.querySelectorAll('.btn-modo').forEach(function (btn) {
            btn.addEventListener('click', function () {
                if (hubRig && hubRig.state === signalR.HubConnectionState.Connected) {
                    hubRig.invoke('CambiarModo', btn.dataset.modo);
                }
            });
        });

        // PTT
        const btnPtt = document.getElementById('btn-ptt');
        if (btnPtt) {
            btnPtt.addEventListener('click', function () {
                if (hubRig && hubRig.state === signalR.HubConnectionState.Connected) {
                    const activar = !btnPtt.classList.contains('transmitiendo');
                    hubRig.invoke('CambiarPtt', activar);
                }
            });
        }

        // VFO
        const btnVfo = document.getElementById('btn-vfo');
        if (btnVfo) {
            btnVfo.addEventListener('click', function () {
                if (hubRig && hubRig.state === signalR.HubConnectionState.Connected) {
                    hubRig.invoke('CambiarVfo');
                }
            });
        }
    }

    // ================================================================
    // ENLAZAR PANEL TX
    // ================================================================

    function enlazarPanelTx() {
        document.querySelectorAll('.btn-tx').forEach(function (btn) {
            btn.addEventListener('click', function () {
                document.querySelectorAll('.btn-tx').forEach(function (b) { b.classList.remove('activo'); });
                btn.classList.add('activo');
            });
        });
    }

    // ================================================================
    // ENLAZAR PANEL QSO
    // ================================================================

    function enlazarPanelQso() {
        const btnEnableTx = document.getElementById('btn-enable-tx');
        const btnAutoSeq = document.getElementById('btn-auto-seq');
        const btnHalt = document.getElementById('btn-halt');

        if (btnEnableTx) {
            btnEnableTx.addEventListener('click', function () {
                enableTx = !enableTx;
                btnEnableTx.classList.toggle('activo', enableTx);
            });
        }

        if (btnAutoSeq) {
            btnAutoSeq.addEventListener('click', function () {
                autoSeq = !autoSeq;
                btnAutoSeq.classList.toggle('activo', autoSeq);
            });
        }

        if (btnHalt) {
            btnHalt.addEventListener('click', function () {
                enableTx = false;
                if (btnEnableTx) { btnEnableTx.classList.remove('activo'); }
                if (hubRig && hubRig.state === signalR.HubConnectionState.Connected) {
                    hubRig.invoke('CambiarPtt', false);
                }
            });
        }

        const btnLogQso = document.getElementById('btn-log-qso');
        if (btnLogQso) {
            btnLogQso.addEventListener('click', function () {
                registrarQso();
            });
        }
    }

    // ================================================================
    // ENLAZAR CONFIGURACION
    // ================================================================

    function enlazarConfiguracion() {
        // Toggle tipo conexion
        const tipoCat = document.getElementById('tipo-cat');
        const tipoRigctld = document.getElementById('tipo-rigctld');
        const configCat = document.getElementById('config-cat');
        const configRigctld = document.getElementById('config-rigctld');

        if (tipoCat && tipoRigctld) {
            tipoCat.addEventListener('change', function () {
                configCat.style.display = 'block';
                configRigctld.style.display = 'none';
            });
            tipoRigctld.addEventListener('change', function () {
                configCat.style.display = 'none';
                configRigctld.style.display = 'block';
            });
        }

        // Refrescar puertos
        const btnRefrescarPuertos = document.getElementById('btn-refrescar-puertos');
        if (btnRefrescarPuertos) {
            btnRefrescarPuertos.addEventListener('click', function () {
                refrescarPuertos();
            });
        }

        // Refrescar audio
        const btnRefrescarAudio = document.getElementById('btn-refrescar-audio');
        if (btnRefrescarAudio) {
            btnRefrescarAudio.addEventListener('click', function () {
                refrescarDispositivosAudio();
            });
        }

        // Conectar
        const btnConectar = document.getElementById('btn-conectar-rig');
        if (btnConectar) {
            btnConectar.addEventListener('click', function () {
                conectarRig();
            });
        }

        // Desconectar
        const btnDesconectar = document.getElementById('btn-desconectar-rig');
        if (btnDesconectar) {
            btnDesconectar.addEventListener('click', function () {
                desconectarRig();
            });
        }

        // Backup manual
        const btnBackup = document.getElementById('btn-backup-manual');
        if (btnBackup) {
            btnBackup.addEventListener('click', function () {
                crearBackupManual();
            });
        }

        // Guardar preferencias al cerrar modal
        const modal = document.getElementById('modal-configuracion');
        if (modal) {
            modal.addEventListener('show.bs.modal', function () {
                refrescarPuertos();
                refrescarDispositivosAudio();
                cargarPreferencias();
            });
            modal.addEventListener('hide.bs.modal', function () {
                guardarPreferencias();
            });
        }
    }

    function cargarPreferencias() {
        fetch('/api/configuracion/preferencias')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                const selectFecha = document.getElementById('config-formato-fecha');
                if (selectFecha && data.formatoFecha) { selectFecha.value = data.formatoFecha; }

                const txtNotas = document.getElementById('config-notas-estacion');
                if (txtNotas && data.notasEstacion !== undefined) { txtNotas.value = data.notasEstacion; }

                const chkBackup = document.getElementById('config-backup-auto');
                if (chkBackup && data.backupAutomatico !== undefined) { chkBackup.checked = data.backupAutomatico; }
            })
            .catch(function (err) { console.warn('Error cargando preferencias:', err); });
    }

    function guardarPreferencias() {
        const selectFecha = document.getElementById('config-formato-fecha');
        const txtNotas = document.getElementById('config-notas-estacion');
        const chkBackup = document.getElementById('config-backup-auto');

        const dto = {
            formatoFecha: selectFecha ? selectFecha.value : null,
            notasEstacion: txtNotas ? txtNotas.value : null,
            backupAutomatico: chkBackup ? chkBackup.checked : null
        };

        fetch('/api/configuracion/preferencias', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        }).catch(function (err) { console.warn('Error guardando preferencias:', err); });
    }

    function crearBackupManual() {
        const resultado = document.getElementById('backup-resultado');
        if (resultado) { resultado.textContent = 'Creando backup...'; }

        fetch('/api/configuracion/backup', { method: 'POST' })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (resultado) { resultado.textContent = data.mensaje; }
            })
            .catch(function (err) {
                if (resultado) { resultado.textContent = 'Error: ' + err; }
            });
    }

    function refrescarPuertos() {
        if (!hubRig || hubRig.state !== signalR.HubConnectionState.Connected) { return; }

        hubRig.invoke('ObtenerPuertos').then(function (puertos) {
            const select = document.getElementById('config-puerto');
            if (!select) { return; }
            select.innerHTML = '';
            puertos.forEach(function (puerto) {
                const option = document.createElement('option');
                option.value = puerto;
                option.textContent = puerto;
                select.appendChild(option);
            });
        });
    }

    function refrescarDispositivosAudio() {
        if (!hubRig || hubRig.state !== signalR.HubConnectionState.Connected) { return; }

        hubRig.invoke('ObtenerDispositivosAudio').then(function (dispositivos) {
            const select = document.getElementById('config-audio');
            if (!select) { return; }
            select.innerHTML = '';
            dispositivos.forEach(function (d) {
                if (d.esEntrada) {
                    const option = document.createElement('option');
                    option.value = d.id;
                    option.textContent = d.nombre;
                    select.appendChild(option);
                }
            });
        });
    }

    function conectarRig() {
        if (!hubRig || hubRig.state !== signalR.HubConnectionState.Connected) { return; }

        const usarCat = document.getElementById('tipo-cat').checked;

        const config = {
            usarCatSerial: usarCat,
            puerto: document.getElementById('config-puerto')?.value || '',
            baudios: parseInt(document.getElementById('config-baudios')?.value || '38400'),
            modelo: document.getElementById('config-modelo')?.value || 'Automatico',
            bitsDeDatos: parseInt(document.getElementById('config-bits-datos')?.value || '8'),
            bitsDeParada: parseInt(document.getElementById('config-stop-bits')?.value || '1'),
            paridad: document.getElementById('config-paridad')?.value || 'None',
            rtsEnable: document.getElementById('config-rts')?.checked || false,
            dtrEnable: document.getElementById('config-dtr')?.checked || false,
            metodoPtt: document.getElementById('config-ptt')?.value || 'CAT',
            intervaloPollingMs: parseInt(document.getElementById('config-polling')?.value || '200'),
            hostRigctld: document.getElementById('config-host')?.value || 'localhost',
            puertoRigctld: parseInt(document.getElementById('config-puerto-tcp')?.value || '4532'),
            dispositivoAudioEntrada: document.getElementById('config-audio')?.value || '',
            tasaDeMuestreoHz: parseInt(document.getElementById('config-sample-rate')?.value || '48000')
        };

        const configEstado = document.getElementById('config-estado');
        if (configEstado) { configEstado.textContent = 'Conectando...'; }

        hubRig.invoke('ConectarRig', config).catch(function (err) {
            if (configEstado) { configEstado.textContent = 'Error: ' + err; }
        });
    }

    function desconectarRig() {
        if (!hubRig || hubRig.state !== signalR.HubConnectionState.Connected) { return; }
        hubRig.invoke('DesconectarRig');
    }

    // ================================================================
    // RELOJ UTC
    // ================================================================

    function actualizarRelojUtc() {
        const reloj = document.getElementById('reloj-utc');
        if (!reloj) { return; }

        const ahora = new Date();
        const horas = ahora.getUTCHours().toString().padStart(2, '0');
        const minutos = ahora.getUTCMinutes().toString().padStart(2, '0');
        const segundos = ahora.getUTCSeconds().toString().padStart(2, '0');
        reloj.textContent = horas + ':' + minutos + ':' + segundos;
    }

    // ================================================================
    // ATAJOS DE TECLADO
    // ================================================================

    function manejarAtajoTeclado(e) {
        // No capturar si esta en un input
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') { return; }

        switch (e.key) {
            case 'F1':
                e.preventDefault();
                document.getElementById('btn-tx1')?.click();
                break;
            case 'F2':
                e.preventDefault();
                document.getElementById('btn-tx2')?.click();
                break;
            case 'F3':
                e.preventDefault();
                document.getElementById('btn-tx3')?.click();
                break;
            case 'F4':
                e.preventDefault();
                document.getElementById('btn-tx4')?.click();
                break;
            case 'F5':
                e.preventDefault();
                document.getElementById('btn-tx5')?.click();
                break;
            case 'F6':
                e.preventDefault();
                document.getElementById('btn-tx6')?.click();
                break;
            case 'Escape':
                e.preventDefault();
                document.getElementById('btn-halt')?.click();
                break;
        }
    }

    // ================================================================
    // REGISTRO DE QSO
    // ================================================================

    function registrarQso() {
        const inputDxCall = document.getElementById('qso-dx-call');
        const inputGrid = document.getElementById('qso-grid');
        const inputRptSent = document.getElementById('qso-rpt-sent');
        const inputRptRcvd = document.getElementById('qso-rpt-rcvd');

        const indicativo = inputDxCall ? inputDxCall.value.trim() : '';
        const grid = inputGrid ? inputGrid.value.trim() : '';
        const rstEnviado = inputRptSent ? inputRptSent.value.trim() : '';
        const rstRecibido = inputRptRcvd ? inputRptRcvd.value.trim() : '';

        if (!indicativo) {
            mostrarNotificacion('El indicativo es obligatorio.', true);
            return;
        }

        if (!rstEnviado || !rstRecibido) {
            mostrarNotificacion('Los reportes RST enviado y recibido son obligatorios.', true);
            return;
        }

        const datos = {
            indicativo: indicativo,
            frecuenciaHz: frecuenciaActualHz,
            modo: modoActual,
            rstEnviado: rstEnviado,
            rstRecibido: rstRecibido,
            grid: grid || null,
            nombre: null,
            comentario: null
        };

        fetch('/api/logbook/registrar', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(datos)
        })
        .then(function (respuesta) {
            return respuesta.json().then(function (cuerpo) {
                return { ok: respuesta.ok, status: respuesta.status, cuerpo: cuerpo };
            });
        })
        .then(function (resultado) {
            if (resultado.ok) {
                mostrarNotificacion('QSO registrado con ' + indicativo, false);
                // Limpiar campos
                if (inputDxCall) { inputDxCall.value = ''; }
                if (inputGrid) { inputGrid.value = ''; }
                if (inputRptSent) { inputRptSent.value = ''; }
                if (inputRptRcvd) { inputRptRcvd.value = ''; }
            } else {
                const mensaje = resultado.cuerpo.mensaje || 'Error al registrar el QSO.';
                mostrarNotificacion(mensaje, true);
            }
        })
        .catch(function (err) {
            mostrarNotificacion('Error de red: ' + err.message, true);
        });
    }

    function mostrarNotificacion(mensaje, esError) {
        const div = document.createElement('div');
        div.textContent = mensaje;
        div.style.position = 'fixed';
        div.style.top = '20px';
        div.style.right = '20px';
        div.style.padding = '12px 20px';
        div.style.borderRadius = '6px';
        div.style.color = '#fff';
        div.style.fontSize = '14px';
        div.style.fontWeight = '500';
        div.style.zIndex = '9999';
        div.style.boxShadow = '0 4px 12px rgba(0,0,0,0.3)';
        div.style.transition = 'opacity 0.3s ease';
        div.style.backgroundColor = esError ? '#dc3545' : '#28a745';

        document.body.appendChild(div);

        setTimeout(function () {
            div.style.opacity = '0';
            setTimeout(function () {
                if (div.parentNode) {
                    div.parentNode.removeChild(div);
                }
            }, 300);
        }, 3000);
    }

    // ================================================================
    // UTILIDADES
    // ================================================================

    function escapeHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto;
        return div.innerHTML;
    }

    // ================================================================
    // ARRANQUE
    // ================================================================

    document.addEventListener('DOMContentLoaded', iniciar);

    return {
        iniciar: iniciar
    };
})();
