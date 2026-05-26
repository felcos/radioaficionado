'use strict';

/**
 * Modulo de DX Cluster — recibe spots en tiempo real via SignalR (HubEstado).
 */
const DxCluster = (function () {
    const maxSpots = 200;
    let spots = [];

    /**
     * Limites de bandas de radioaficionado en KHz para derivar la banda a partir de la frecuencia.
     */
    const bandasKHz = [
        { nombre: '160m', min: 1800, max: 2000 },
        { nombre: '80m', min: 3500, max: 4000 },
        { nombre: '60m', min: 5330, max: 5410 },
        { nombre: '40m', min: 7000, max: 7300 },
        { nombre: '30m', min: 10100, max: 10150 },
        { nombre: '20m', min: 14000, max: 14350 },
        { nombre: '17m', min: 18068, max: 18168 },
        { nombre: '15m', min: 21000, max: 21450 },
        { nombre: '12m', min: 24890, max: 24990 },
        { nombre: '10m', min: 28000, max: 29700 },
        { nombre: '6m', min: 50000, max: 54000 },
        { nombre: '2m', min: 144000, max: 148000 }
    ];

    function obtenerBanda(frecuenciaHz) {
        const khz = frecuenciaHz / 1000;
        for (let i = 0; i < bandasKHz.length; i++) {
            if (khz >= bandasKHz[i].min && khz <= bandasKHz[i].max) {
                return bandasKHz[i].nombre;
            }
        }
        return '';
    }

    function extraerModo(comentario) {
        if (!comentario) { return ''; }
        const upper = comentario.toUpperCase();
        const modos = ['FT8', 'FT4', 'CW', 'SSB', 'RTTY', 'JT65', 'JT9', 'PSK31', 'WSPR', 'JS8'];
        for (let i = 0; i < modos.length; i++) {
            if (upper.indexOf(modos[i]) !== -1) {
                return modos[i];
            }
        }
        return '';
    }

    function formatearHora(fechaIso) {
        if (!fechaIso) { return ''; }
        const fecha = new Date(fechaIso);
        if (isNaN(fecha.getTime())) { return ''; }
        const hh = String(fecha.getUTCHours()).padStart(2, '0');
        const mm = String(fecha.getUTCMinutes()).padStart(2, '0');
        return hh + ':' + mm;
    }

    function formatearFrecuencia(frecuenciaHz) {
        if (!frecuenciaHz) { return ''; }
        const khz = frecuenciaHz / 1000;
        return khz.toFixed(1);
    }

    function init(hubEstado) {
        if (!hubEstado) { return; }

        hubEstado.on('RecibirSpotDx', function (spotDto) {
            const spot = {
                hora: formatearHora(spotDto.horaUtc),
                indicativoDx: spotDto.dx || '',
                frecuencia: formatearFrecuencia(spotDto.frecuenciaHz),
                frecuenciaHz: spotDto.frecuenciaHz || 0,
                indicativoSpotter: spotDto.spotteador || '',
                comentario: spotDto.comentario || '',
                banda: obtenerBanda(spotDto.frecuenciaHz || 0),
                modo: extraerModo(spotDto.comentario),
                dxcc: ''
            };
            agregarSpot(spot);
        });

        hubEstado.on('RecibirNotificacion', function (tipo, mensaje) {
            if (tipo === 'dxcluster-conexion') {
                setEstadoConexion(mensaje === 'conectado');
            }
        });

        hubEstado.on('RecibirAlerta', function (nombreRegla, mensaje, dx, frecuenciaHz, conSonido) {
            mostrarAlerta(nombreRegla, mensaje, dx, frecuenciaHz, conSonido);
        });

        const filtroBanda = document.getElementById('dxcluster-filtro-banda');
        if (filtroBanda) {
            filtroBanda.addEventListener('change', renderizar);
        }

        const filtroModo = document.getElementById('dxcluster-filtro-modo');
        if (filtroModo) {
            filtroModo.addEventListener('change', renderizar);
        }

        const filtroIndicativo = document.getElementById('dxcluster-filtro-indicativo');
        if (filtroIndicativo) {
            filtroIndicativo.addEventListener('input', renderizar);
        }
    }

    function agregarSpot(spot) {
        spots.unshift(spot);
        if (spots.length > maxSpots) {
            spots.length = maxSpots;
        }

        actualizarContador();
        renderizar();
    }

    function renderizar() {
        const tbody = document.getElementById('dxcluster-tbody');
        if (!tbody) { return; }

        const filtroBanda = document.getElementById('dxcluster-filtro-banda');
        const filtroModo = document.getElementById('dxcluster-filtro-modo');
        const filtroIndicativo = document.getElementById('dxcluster-filtro-indicativo');
        const bandaFiltro = filtroBanda ? filtroBanda.value : '';
        const modoFiltro = filtroModo ? filtroModo.value : '';
        const indicativoFiltro = filtroIndicativo ? filtroIndicativo.value.toUpperCase().trim() : '';

        let filtrados = spots;

        if (indicativoFiltro) {
            filtrados = filtrados.filter(function (s) {
                return s.indicativoDx.toUpperCase().indexOf(indicativoFiltro) !== -1 ||
                       s.indicativoSpotter.toUpperCase().indexOf(indicativoFiltro) !== -1;
            });
        }
        if (bandaFiltro) {
            filtrados = filtrados.filter(function (s) { return s.banda === bandaFiltro; });
        }
        if (modoFiltro) {
            filtrados = filtrados.filter(function (s) { return s.modo === modoFiltro; });
        }

        tbody.innerHTML = '';

        const limite = Math.min(filtrados.length, 100);
        for (let i = 0; i < limite; i++) {
            const spot = filtrados[i];
            const tr = document.createElement('tr');
            tr.className = spot.colorIndicativo ? '' : '';
            if (spot.colorIndicativo) {
                tr.style.color = spot.colorIndicativo;
            }
            tr.innerHTML =
                '<td>' + (spot.hora || '') + '</td>' +
                '<td><strong>' + (spot.indicativoDx || '') + '</strong></td>' +
                '<td>' + (spot.frecuencia || '') + '</td>' +
                '<td>' + (spot.indicativoSpotter || '') + '</td>' +
                '<td>' + (spot.comentario || '') + '</td>' +
                '<td>' + (spot.dxcc || '') + '</td>';

            // Click para sintonizar
            tr.style.cursor = 'pointer';
            tr.addEventListener('click', (function (frecuencia) {
                return function () {
                    sintonizarSpot(frecuencia);
                };
            })(spot.frecuenciaHz));

            tbody.appendChild(tr);
        }
    }

    function sintonizarSpot(frecuenciaHz) {
        if (frecuenciaHz && window.operacionModule && window.operacionModule.cambiarFrecuencia) {
            window.operacionModule.cambiarFrecuencia(frecuenciaHz);
        }
    }

    function actualizarContador() {
        const elem = document.getElementById('dxcluster-contador');
        if (elem) {
            elem.textContent = spots.length + ' spots';
        }
    }

    function setEstadoConexion(conectado) {
        const elem = document.getElementById('dxcluster-estado-conexion');
        if (elem) {
            elem.textContent = conectado ? 'Conectado' : 'Desconectado';
            elem.className = 'badge ' + (conectado ? 'bg-success' : 'bg-secondary');
        }
    }

    function mostrarAlerta(nombreRegla, mensaje, dx, frecuenciaHz, conSonido) {
        // Crear toast de alerta
        const contenedor = document.getElementById('dxcluster-alertas') || crearContenedorAlertas();
        const toast = document.createElement('div');
        toast.className = 'dxcluster-alerta';
        toast.innerHTML =
            '<div class="dxcluster-alerta-titulo">' +
            '<span class="dxcluster-alerta-icono">&#9888;</span> ' +
            '<strong>' + nombreRegla + '</strong>' +
            '<button class="dxcluster-alerta-cerrar" title="Cerrar">&times;</button>' +
            '</div>' +
            '<div class="dxcluster-alerta-mensaje">' + mensaje + '</div>';

        toast.querySelector('.dxcluster-alerta-cerrar').addEventListener('click', function () {
            toast.remove();
        });

        // Click en el toast sintoniza la frecuencia
        toast.style.cursor = 'pointer';
        toast.addEventListener('click', function (e) {
            if (e.target.classList.contains('dxcluster-alerta-cerrar')) { return; }
            sintonizarSpot(frecuenciaHz);
        });

        contenedor.prepend(toast);

        // Auto-cerrar despues de 15 segundos
        setTimeout(function () {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 15000);

        // Sonido de alerta
        if (conSonido) {
            reproducirSonidoAlerta();
        }
    }

    function crearContenedorAlertas() {
        const contenedor = document.createElement('div');
        contenedor.id = 'dxcluster-alertas';
        contenedor.className = 'dxcluster-alertas-contenedor';
        const panelDx = document.querySelector('.panel-dxcluster-contenido');
        if (panelDx) {
            panelDx.prepend(contenedor);
        } else {
            document.body.appendChild(contenedor);
        }
        return contenedor;
    }

    function reproducirSonidoAlerta() {
        try {
            const contextoAudio = new (window.AudioContext || window.webkitAudioContext)();
            const oscilador = contextoAudio.createOscillator();
            const ganancia = contextoAudio.createGain();
            oscilador.connect(ganancia);
            ganancia.connect(contextoAudio.destination);
            oscilador.type = 'sine';
            oscilador.frequency.setValueAtTime(880, contextoAudio.currentTime);
            oscilador.frequency.setValueAtTime(1100, contextoAudio.currentTime + 0.1);
            oscilador.frequency.setValueAtTime(880, contextoAudio.currentTime + 0.2);
            ganancia.gain.setValueAtTime(0.3, contextoAudio.currentTime);
            ganancia.gain.exponentialRampToValueAtTime(0.01, contextoAudio.currentTime + 0.4);
            oscilador.start(contextoAudio.currentTime);
            oscilador.stop(contextoAudio.currentTime + 0.4);
        } catch (e) {
            // AudioContext no disponible, ignorar silenciosamente
        }
    }

    return {
        init: init,
        setEstadoConexion: setEstadoConexion,
        mostrarAlerta: mostrarAlerta
    };
})();
