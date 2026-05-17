'use strict';

/**
 * Modulo de Control Remoto del Rig.
 * Gestiona la conexion SignalR y la interfaz de control del rig via web.
 */
const ControlRemoto = (function () {
    const MAX_LOG_ENTRADAS = 20;
    const PTT_TIMEOUT_SEGUNDOS = 180;

    let conexion = null;
    let pttActivo = false;
    let pttIntervalId = null;
    let pttSegundosRestantes = 0;

    // --- Elementos del DOM ---
    function el(id) {
        return document.getElementById(id);
    }

    // --- Formateo ---
    function formatearFrecuencia(hz) {
        if (!hz || hz <= 0) return '---.---.--- Hz';
        const texto = hz.toString();
        const partes = [];
        let i = texto.length;
        while (i > 0) {
            const inicio = Math.max(0, i - 3);
            partes.unshift(texto.substring(inicio, i));
            i = inicio;
        }
        return partes.join('.') + ' Hz';
    }

    function horaActual() {
        const ahora = new Date();
        const hh = String(ahora.getHours()).padStart(2, '0');
        const mm = String(ahora.getMinutes()).padStart(2, '0');
        const ss = String(ahora.getSeconds()).padStart(2, '0');
        return hh + ':' + mm + ':' + ss;
    }

    // --- Log de comandos ---
    function agregarLog(mensaje, clase) {
        const logDiv = el('logComandos');
        if (!logDiv) return;

        const entrada = document.createElement('div');
        entrada.className = 'log-entrada ' + (clase || 'log-info');
        entrada.innerHTML = '<span class="log-timestamp">[' + horaActual() + ']</span> ' + mensaje;

        logDiv.appendChild(entrada);

        // Mantener maximo de entradas
        const entradas = logDiv.querySelectorAll('.log-entrada');
        while (entradas.length > MAX_LOG_ENTRADAS) {
            logDiv.removeChild(entradas[0]);
            break;
        }

        logDiv.scrollTop = logDiv.scrollHeight;
    }

    function limpiarLog() {
        const logDiv = el('logComandos');
        if (logDiv) {
            logDiv.innerHTML = '';
            agregarLog('Log limpiado.', 'log-info');
        }
    }

    // --- Actualizacion de UI ---
    function actualizarIndicadorConexion(conectado) {
        const indicador = el('indicadorConexion');
        const texto = el('textoConexion');
        if (!indicador || !texto) return;

        if (conectado) {
            indicador.className = 'indicador-conexion indicador-conectado';
            texto.textContent = 'Servicio conectado';
        } else {
            indicador.className = 'indicador-conexion indicador-desconectado';
            texto.textContent = 'Servicio desconectado';
        }
    }

    function actualizarEstadoRig(estado) {
        if (!estado) return;

        // Frecuencia
        const displayFreq = el('displayFrecuencia');
        if (displayFreq) {
            displayFreq.textContent = formatearFrecuencia(estado.frecuenciaHz);
        }

        // Modo
        const badgeModo = el('badgeModo');
        if (badgeModo) {
            badgeModo.textContent = estado.modo || '---';
        }

        // Banda
        const badgeBanda = el('badgeBanda');
        if (badgeBanda) {
            badgeBanda.textContent = estado.banda || '---';
        }

        // VFO
        const badgeVfo = el('badgeVfo');
        if (badgeVfo) {
            badgeVfo.textContent = 'VFO ' + (estado.vfoActivo || 'A');
        }

        // Split
        const badgeSplit = el('badgeSplit');
        if (badgeSplit) {
            badgeSplit.textContent = 'Split ' + (estado.splitActivo ? 'ON' : 'OFF');
            badgeSplit.className = 'badge badge-modo ' + (estado.splitActivo ? 'bg-warning' : 'bg-secondary');
        }

        // TX/RX
        const indicadorTxRx = el('indicadorTxRx');
        if (indicadorTxRx) {
            if (estado.transmitiendo) {
                indicadorTxRx.className = 'indicador-txrx indicador-tx';
                indicadorTxRx.textContent = 'TX';
            } else {
                indicadorTxRx.className = 'indicador-txrx indicador-rx';
                indicadorTxRx.textContent = 'RX';
            }
        }

        // S-Meter
        const smeterBarra = el('smeterBarra');
        const smeterEtiqueta = el('smeterEtiqueta');
        if (smeterBarra && smeterEtiqueta) {
            const porcentaje = Math.min(100, Math.max(0, estado.nivelSenal));
            smeterBarra.style.width = porcentaje + '%';
            smeterEtiqueta.textContent = obtenerEtiquetaSmeter(porcentaje);
        }

        // Potencia display
        const valorPotDisplay = el('valorPotenciaDisplay');
        const barraPot = el('barraPotencia');
        if (valorPotDisplay) {
            valorPotDisplay.textContent = estado.potenciaVatios || 0;
        }
        if (barraPot) {
            const porcPot = Math.min(100, Math.max(0, estado.potenciaVatios));
            barraPot.style.width = porcPot + '%';
        }

        // Input frecuencia
        const inputFreq = el('inputFrecuencia');
        if (inputFreq && !inputFreq.matches(':focus')) {
            inputFreq.value = estado.frecuenciaHz || '';
        }

        // Select modo
        const selectModo = el('selectModo');
        if (selectModo && !selectModo.matches(':focus') && estado.modo) {
            selectModo.value = estado.modo;
        }

        // Slider potencia
        const sliderPot = el('sliderPotencia');
        if (sliderPot && !sliderPot.matches(':active')) {
            sliderPot.value = estado.potenciaVatios || 0;
            const valorSlider = el('valorSliderPotencia');
            if (valorSlider) {
                valorSlider.textContent = estado.potenciaVatios || 0;
            }
        }

        // Conectado
        actualizarBotonesConexion(estado.conectado);
    }

    function obtenerEtiquetaSmeter(porcentaje) {
        if (porcentaje <= 0) return 'S0';
        if (porcentaje <= 6) return 'S1';
        if (porcentaje <= 12) return 'S2';
        if (porcentaje <= 18) return 'S3';
        if (porcentaje <= 25) return 'S4';
        if (porcentaje <= 33) return 'S5';
        if (porcentaje <= 42) return 'S6';
        if (porcentaje <= 50) return 'S7';
        if (porcentaje <= 58) return 'S8';
        if (porcentaje <= 67) return 'S9';
        if (porcentaje <= 78) return 'S9+10';
        if (porcentaje <= 89) return 'S9+20';
        return 'S9+30';
    }

    function actualizarBotonesConexion(conectado) {
        const btnConectar = el('btnConectar');
        const btnDesconectar = el('btnDesconectar');
        if (btnConectar) {
            btnConectar.disabled = conectado;
        }
        if (btnDesconectar) {
            btnDesconectar.disabled = !conectado;
        }
    }

    // --- PTT ---
    function activarPtt() {
        if (pttActivo) {
            desactivarPtt();
            return;
        }

        pttActivo = true;
        pttSegundosRestantes = PTT_TIMEOUT_SEGUNDOS;

        const btnPtt = el('btnPtt');
        if (btnPtt) {
            btnPtt.className = 'btn btn-ptt btn-ptt-activo';
            btnPtt.textContent = 'PTT - TRANSMITIENDO';
        }

        const countdown = el('pttCountdown');
        const timer = el('pttTimer');
        if (countdown) countdown.style.display = 'block';
        if (timer) timer.textContent = pttSegundosRestantes;

        enviarComando('CambiarPtt', 'true');
        agregarLog('PTT activado (timeout: ' + PTT_TIMEOUT_SEGUNDOS + 's)', 'log-info');

        pttIntervalId = setInterval(function () {
            pttSegundosRestantes--;
            if (timer) timer.textContent = pttSegundosRestantes;

            if (pttSegundosRestantes <= 0) {
                desactivarPtt();
                agregarLog('PTT desactivado por timeout de seguridad.', 'log-error');
            }
        }, 1000);
    }

    function desactivarPtt() {
        pttActivo = false;

        if (pttIntervalId) {
            clearInterval(pttIntervalId);
            pttIntervalId = null;
        }

        const btnPtt = el('btnPtt');
        if (btnPtt) {
            btnPtt.className = 'btn btn-ptt btn-ptt-inactivo';
            btnPtt.textContent = 'PTT - TRANSMITIR';
        }

        const countdown = el('pttCountdown');
        if (countdown) countdown.style.display = 'none';

        enviarComando('CambiarPtt', 'false');
        agregarLog('PTT desactivado.', 'log-info');
    }

    // --- Comandos ---
    function enviarComando(tipoTexto, payload) {
        if (!conexion || conexion.state !== signalR.HubConnectionState.Connected) {
            agregarLog('No conectado al servidor. Comando descartado.', 'log-error');
            return;
        }

        const usuarioId = el('usuarioId')?.value || '';
        const tipoMap = {
            'CambiarFrecuencia': 0,
            'CambiarModo': 1,
            'CambiarBanda': 2,
            'CambiarPtt': 3,
            'CambiarVfo': 4,
            'Conectar': 5,
            'Desconectar': 6,
            'ObtenerEstado': 7,
            'CambiarPotencia': 8,
            'CambiarSplit': 9
        };

        const comando = {
            comandoId: crypto.randomUUID(),
            tipo: tipoMap[tipoTexto] !== undefined ? tipoMap[tipoTexto] : 7,
            usuarioId: usuarioId,
            payload: payload || null,
            creadoEn: new Date().toISOString()
        };

        conexion.invoke('EnviarComando', comando)
            .then(function () {
                agregarLog('Comando enviado: ' + tipoTexto + (payload ? ' [' + payload + ']' : ''), 'log-info');
            })
            .catch(function (err) {
                agregarLog('Error enviando comando: ' + err.toString(), 'log-error');
            });
    }

    // --- SignalR ---
    function iniciarConexion() {
        conexion = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/relay-rig')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        // Handlers
        conexion.on('RecibirEstadoRig', function (estado) {
            actualizarEstadoRig(estado);
        });

        conexion.on('RecibirRespuestaComando', function (respuesta) {
            if (respuesta.exitoso) {
                agregarLog('Respuesta OK: ' + (respuesta.datos || 'sin datos'), 'log-exito');
            } else {
                agregarLog('Respuesta ERROR: ' + (respuesta.mensajeError || 'error desconocido'), 'log-error');
            }
        });

        conexion.on('RecibirConexionServicio', function (conectado) {
            actualizarIndicadorConexion(conectado);
            const mensaje = conectado ? 'Servicio de escritorio conectado.' : 'Servicio de escritorio desconectado.';
            agregarLog(mensaje, conectado ? 'log-exito' : 'log-error');
        });

        conexion.onreconnecting(function () {
            agregarLog('Reconectando al servidor...', 'log-info');
        });

        conexion.onreconnected(function () {
            agregarLog('Reconectado al servidor.', 'log-exito');
            enviarComando('ObtenerEstado', null);
        });

        conexion.onclose(function () {
            agregarLog('Conexion cerrada. Intentando reconectar...', 'log-error');
            setTimeout(function () {
                iniciarConexionHub();
            }, 5000);
        });

        iniciarConexionHub();
    }

    function iniciarConexionHub() {
        conexion.start()
            .then(function () {
                agregarLog('Conectado al servidor SignalR.', 'log-exito');
                enviarComando('ObtenerEstado', null);
            })
            .catch(function (err) {
                agregarLog('Error de conexion: ' + err.toString(), 'log-error');
                setTimeout(function () {
                    iniciarConexionHub();
                }, 5000);
            });
    }

    // --- Eventos de UI ---
    function enlazarEventos() {
        // Cambiar frecuencia
        const btnFreq = el('btnCambiarFrecuencia');
        if (btnFreq) {
            btnFreq.addEventListener('click', function () {
                const valor = el('inputFrecuencia')?.value;
                if (valor && parseInt(valor, 10) > 0) {
                    enviarComando('CambiarFrecuencia', valor);
                }
            });
        }

        // Enter en input frecuencia
        const inputFreq = el('inputFrecuencia');
        if (inputFreq) {
            inputFreq.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    btnFreq?.click();
                }
            });
        }

        // Cambiar modo
        const selectModo = el('selectModo');
        if (selectModo) {
            selectModo.addEventListener('change', function () {
                enviarComando('CambiarModo', selectModo.value);
            });
        }

        // Cambiar banda
        const selectBanda = el('selectBanda');
        if (selectBanda) {
            selectBanda.addEventListener('change', function () {
                enviarComando('CambiarBanda', selectBanda.value);
            });
        }

        // Slider potencia
        const sliderPot = el('sliderPotencia');
        if (sliderPot) {
            sliderPot.addEventListener('input', function () {
                const valorLabel = el('valorSliderPotencia');
                if (valorLabel) valorLabel.textContent = sliderPot.value;
            });
            sliderPot.addEventListener('change', function () {
                enviarComando('CambiarPotencia', sliderPot.value);
            });
        }

        // PTT
        const btnPtt = el('btnPtt');
        if (btnPtt) {
            btnPtt.addEventListener('click', function () {
                activarPtt();
            });
        }

        // Conectar
        const btnConectar = el('btnConectar');
        if (btnConectar) {
            btnConectar.addEventListener('click', function () {
                enviarComando('Conectar', null);
            });
        }

        // Desconectar
        const btnDesconectar = el('btnDesconectar');
        if (btnDesconectar) {
            btnDesconectar.addEventListener('click', function () {
                enviarComando('Desconectar', null);
                if (pttActivo) desactivarPtt();
            });
        }

        // Limpiar log
        const btnLimpiar = el('btnLimpiarLog');
        if (btnLimpiar) {
            btnLimpiar.addEventListener('click', limpiarLog);
        }
    }

    // --- Inicializacion ---
    function inicializar() {
        enlazarEventos();
        iniciarConexion();
    }

    // Iniciar cuando el DOM este listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', inicializar);
    } else {
        inicializar();
    }

    // API publica
    return {
        enviarComando: enviarComando,
        formatearFrecuencia: formatearFrecuencia
    };
})();
