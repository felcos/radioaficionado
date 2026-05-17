'use strict';

/**
 * Modulo de Mapa de QSOs — great circle lines desde QTH a cada contacto.
 */
const MapaQsos = (function () {
    let mapaInstance = null;
    let capaLineas = null;
    let capaMarcadores = null;
    let capaGrids = null;
    let qthPropio = [40.4168, -3.7038]; // Madrid por defecto, configurable

    // Colores por banda
    const coloresBanda = {
        'Banda160m': '#8b5cf6',
        'Banda80m': '#6366f1',
        'Banda60m': '#3b82f6',
        'Banda40m': '#06b6d4',
        'Banda30m': '#14b8a6',
        'Banda20m': '#22c55e',
        'Banda17m': '#84cc16',
        'Banda15m': '#eab308',
        'Banda12m': '#f97316',
        'Banda10m': '#ef4444',
        'Banda6m': '#ec4899',
        'Banda2m': '#f43f5e'
    };

    function init() {
        // Inicializar mapa
        inicializarMapa();
        // Cargar QSOs
        cargarQsos();
        // Configurar filtros
        configurarFiltros();
    }

    function inicializarMapa() {
        if (mapaInstance) { return; }
        const contenedor = document.getElementById('mapa-qsos');
        if (!contenedor || typeof L === 'undefined') { return; }

        mapaInstance = L.map('mapa-qsos', {
            center: qthPropio,
            zoom: 3,
            minZoom: 2,
            maxZoom: 12,
            zoomControl: true
        });

        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; OSM &copy; CARTO',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(mapaInstance);

        capaLineas = L.layerGroup().addTo(mapaInstance);
        capaMarcadores = L.layerGroup().addTo(mapaInstance);
        capaGrids = L.layerGroup(); // No añadir por defecto

        // Marcador QTH propio
        L.circleMarker(qthPropio, {
            radius: 6,
            fillColor: '#ff0000',
            color: '#ff0000',
            weight: 2,
            opacity: 1,
            fillOpacity: 0.8
        }).bindPopup('<strong>Mi QTH</strong>').addTo(mapaInstance);
    }

    function cargarQsos() {
        // Cargar TODOS los QSOs (sin paginación, para el mapa)
        fetch('/api/logbook?porPagina=200')
            .then(function (resp) { return resp.json(); })
            .then(function (datos) {
                dibujarQsos(datos.qsos || []);
                actualizarEstadisticas(datos.qsos || []);
            })
            .catch(function (err) {
                console.error('Error cargando QSOs para mapa:', err);
            });
    }

    function dibujarQsos(qsos) {
        if (!mapaInstance || typeof L.Geodesic === 'undefined') { return; }
        capaLineas.clearLayers();
        capaMarcadores.clearLayers();

        const filtroBanda = document.getElementById('mapa-filtro-banda');
        const bandaFiltro = filtroBanda ? filtroBanda.value : '';

        const gridsVistos = {};

        for (let i = 0; i < qsos.length; i++) {
            const qso = qsos[i];
            if (!qso.grid || qso.grid.length < 4) { continue; }
            if (bandaFiltro && qso.banda !== bandaFiltro) { continue; }

            const coordContacto = gridACoordenadas(qso.grid);
            if (!coordContacto) { continue; }

            const color = coloresBanda[qso.banda] || '#8b949e';

            // Línea great circle con Leaflet.Geodesic
            const linea = new L.Geodesic([qthPropio, coordContacto], {
                weight: 1.5,
                opacity: 0.6,
                color: color,
                steps: 50
            });
            linea.addTo(capaLineas);

            // Marcador en destino
            const marcador = L.circleMarker(coordContacto, {
                radius: 3,
                fillColor: color,
                color: color,
                weight: 1,
                opacity: 0.8,
                fillOpacity: 0.6
            });

            const distanciaKm = calcularDistancia(qthPropio[0], qthPropio[1], coordContacto[0], coordContacto[1]);
            marcador.bindPopup(
                '<strong>' + (qso.indicativo || '') + '</strong><br>' +
                'Banda: ' + (qso.banda || '') + '<br>' +
                'Modo: ' + (qso.modo || '') + '<br>' +
                'Grid: ' + (qso.grid || '') + '<br>' +
                'Distancia: ' + Math.round(distanciaKm) + ' km<br>' +
                'Fecha: ' + (qso.fechaHora || '')
            );
            marcador.addTo(capaMarcadores);

            // Contar grids
            const grid4 = qso.grid.substring(0, 4).toUpperCase();
            if (!gridsVistos[grid4]) {
                gridsVistos[grid4] = { banda: qso.banda, confirmado: qso.confirmado };
            }
        }
    }

    // Convertir grid locator (Maidenhead) a lat/lng
    function gridACoordenadas(grid) {
        if (!grid || grid.length < 4) { return null; }
        const g = grid.toUpperCase();

        const lng = (g.charCodeAt(0) - 65) * 20 - 180;
        const lat = (g.charCodeAt(1) - 65) * 10 - 90;
        const lngSub = parseInt(g.charAt(2)) * 2;
        const latSub = parseInt(g.charAt(3));

        let finalLng = lng + lngSub + 1; // centro del cuadrado
        let finalLat = lat + latSub + 0.5;

        if (grid.length >= 6) {
            const lngSub2 = (g.charCodeAt(4) - 65) * (2 / 24);
            const latSub2 = (g.charCodeAt(5) - 65) * (1 / 24);
            finalLng = lng + lngSub + lngSub2 + (1 / 24);
            finalLat = lat + latSub + latSub2 + (0.5 / 24);
        }

        return [finalLat, finalLng];
    }

    // Haversine distance
    function calcularDistancia(lat1, lon1, lat2, lon2) {
        const R = 6371;
        const dLat = (lat2 - lat1) * Math.PI / 180;
        const dLon = (lon2 - lon1) * Math.PI / 180;
        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        return R * c;
    }

    function actualizarEstadisticas(qsos) {
        const elemTotal = document.getElementById('mapa-total-qsos');
        const elemGrids = document.getElementById('mapa-total-grids');
        const elemDistMax = document.getElementById('mapa-dist-max');

        const grids = new Set();
        let distMax = 0;
        let qsoMasLejos = '';

        for (let i = 0; i < qsos.length; i++) {
            const qso = qsos[i];
            if (qso.grid && qso.grid.length >= 4) {
                grids.add(qso.grid.substring(0, 4).toUpperCase());
                const coord = gridACoordenadas(qso.grid);
                if (coord) {
                    const dist = calcularDistancia(qthPropio[0], qthPropio[1], coord[0], coord[1]);
                    if (dist > distMax) {
                        distMax = dist;
                        qsoMasLejos = qso.indicativo || '';
                    }
                }
            }
        }

        if (elemTotal) { elemTotal.textContent = qsos.length; }
        if (elemGrids) { elemGrids.textContent = grids.size; }
        if (elemDistMax) { elemDistMax.textContent = Math.round(distMax) + ' km (' + qsoMasLejos + ')'; }
    }

    function configurarFiltros() {
        const filtroBanda = document.getElementById('mapa-filtro-banda');
        if (filtroBanda) {
            filtroBanda.addEventListener('change', function () {
                cargarQsos();
            });
        }

        const toggleGrids = document.getElementById('mapa-toggle-grids');
        if (toggleGrids) {
            toggleGrids.addEventListener('change', function () {
                if (this.checked) {
                    capaGrids.addTo(mapaInstance);
                } else {
                    mapaInstance.removeLayer(capaGrids);
                }
            });
        }
    }

    return {
        init: init,
        cargar: cargarQsos
    };
})();

document.addEventListener('DOMContentLoaded', MapaQsos.init);
