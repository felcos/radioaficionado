'use strict';

/**
 * Modulo de DXCC Tracking — carga entidades DXCC y su estado por banda.
 */
const Dxcc = (function () {
    let cargando = false;
    let continenteActual = '';
    let mapaInstance = null;
    let marcadores = [];

    /** Limites aproximados de latitud/longitud por continente DXCC */
    const limitesContinente = {
        'AF': { latMin: -5, latMax: 20, lngMin: 20, lngMax: 40 },
        'AS': { latMin: 30, latMax: 45, lngMin: 80, lngMax: 120 },
        'EU': { latMin: 45, latMax: 55, lngMin: 10, lngMax: 30 },
        'NA': { latMin: 35, latMax: 45, lngMin: -100, lngMax: -80 },
        'OC': { latMin: -25, latMax: -10, lngMin: 140, lngMax: 170 },
        'SA': { latMin: -20, latMax: 0, lngMin: -60, lngMax: -40 }
    };

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

        // Inicializar el mapa con un pequeno retraso para que el DOM renderice
        setTimeout(function () {
            inicializarMapa();
        }, 200);
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
                actualizarMapaDxcc(datos.entidades || []);
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

    /**
     * Inicializa el mapa Leaflet para la visualizacion DXCC.
     */
    function inicializarMapa() {
        if (mapaInstance) { return; }
        if (typeof L === 'undefined') { return; }

        const contenedor = document.getElementById('mapa-dxcc');
        if (!contenedor) { return; }

        mapaInstance = L.map('mapa-dxcc', {
            center: [20, 0],
            zoom: 2,
            minZoom: 1,
            maxZoom: 6,
            zoomControl: true,
            attributionControl: true
        });

        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; OpenStreetMap contributors &copy; CARTO',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(mapaInstance);
    }

    /**
     * Calcula coordenadas aproximadas de una entidad DXCC dentro de su continente.
     * Usa un hash determinista basado en el numero de entidad.
     * @param {object} entidad - Objeto entidad con numero y continente.
     * @returns {number[]|null} Array [lat, lng] o null si no se puede calcular.
     */
    function coordenadaEntidad(entidad) {
        const limites = limitesContinente[entidad.continente];
        if (!limites) { return null; }

        const hash = (entidad.numero || 0) % 100;
        const lat = limites.latMin + (hash / 100) * (limites.latMax - limites.latMin);
        const lng = limites.lngMin + ((hash * 7) % 100 / 100) * (limites.lngMax - limites.lngMin);
        return [lat, lng];
    }

    /**
     * Actualiza los marcadores del mapa DXCC segun las entidades cargadas.
     * Solo muestra entidades trabajadas (amarillo) y confirmadas (verde).
     * @param {Array} entidades - Lista de entidades DXCC.
     */
    function actualizarMapaDxcc(entidades) {
        if (!mapaInstance) { return; }

        // Limpiar marcadores existentes
        for (let i = 0; i < marcadores.length; i++) {
            mapaInstance.removeLayer(marcadores[i]);
        }
        marcadores = [];

        for (let i = 0; i < entidades.length; i++) {
            const ent = entidades[i];

            // Determinar estado general de la entidad
            const bandas = ent.bandas || {};
            let estadoGeneral = 'necesitado';

            const clavesBanda = Object.keys(bandas);
            for (let j = 0; j < clavesBanda.length; j++) {
                const estadoBanda = bandas[clavesBanda[j]];
                if (estadoBanda === 'confirmado') {
                    estadoGeneral = 'confirmado';
                    break;
                }
                if (estadoBanda === 'trabajado') {
                    estadoGeneral = 'trabajado';
                }
            }

            // Saltar las necesitadas — demasiados marcadores
            if (estadoGeneral === 'necesitado') { continue; }

            const coord = coordenadaEntidad(ent);
            if (!coord) { continue; }

            const esConfirmado = estadoGeneral === 'confirmado';
            const color = esConfirmado ? '#3fb950' : '#d4a017';
            const radio = esConfirmado ? 4 : 3;

            const marcador = L.circleMarker(coord, {
                radius: radio,
                fillColor: color,
                color: color,
                weight: 1,
                opacity: 0.9,
                fillOpacity: 0.7
            });

            const nombreEntidad = ent.nombre || 'Desconocida';
            const prefijo = ent.prefijo || '';
            const textoEstado = esConfirmado ? 'Confirmado' : 'Trabajado';
            marcador.bindPopup(
                '<strong>' + nombreEntidad + '</strong><br>' +
                'Prefijo: ' + prefijo + '<br>' +
                'Estado: <span style="color:' + color + ';">' + textoEstado + '</span>'
            );

            marcador.addTo(mapaInstance);
            marcadores.push(marcador);
        }
    }

    return {
        init: init,
        cargar: cargarEntidades
    };
})();

document.addEventListener('DOMContentLoaded', Dxcc.init);
