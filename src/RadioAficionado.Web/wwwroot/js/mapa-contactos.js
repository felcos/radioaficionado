'use strict';

/**
 * mapa-contactos.js
 * Inicializa un mapa Leaflet con los QSOs del logbook que tienen localizador Maidenhead.
 * Muestra marcadores con popups informativos y líneas desde la ubicación del operador.
 */

(function () {
    // Ubicación por defecto del operador (centro de España como fallback).
    // Se actualizará si hay QSOs con localizador propio.
    const UBICACION_OPERADOR_DEFECTO = [40.4168, -3.7038];
    const ZOOM_INICIAL = 3;
    const COLOR_LINEA = '#e6a817';
    const URL_DATOS = '/Logbook/MapaDatos';

    const elementoMapa = document.getElementById('mapa-contactos');
    const elementoInfo = document.getElementById('mapa-info');

    if (!elementoMapa) {
        console.error('No se encontró el elemento #mapa-contactos');
        return;
    }

    // Inicializar el mapa centrado en vista mundial.
    const mapa = L.map('mapa-contactos', {
        center: UBICACION_OPERADOR_DEFECTO,
        zoom: ZOOM_INICIAL,
        scrollWheelZoom: true
    });

    // Tiles de OpenStreetMap (gratuito, sin API key).
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 19
    }).addTo(mapa);

    /**
     * Crea el contenido HTML del popup para un marcador.
     * @param {Object} contacto - Datos del contacto.
     * @returns {string} HTML del popup.
     */
    function crearPopup(contacto) {
        let html = '<div>';
        html += '<div class="popup-indicativo">' + escaparHtml(contacto.indicativo) + '</div>';
        html += '<table class="popup-tabla">';
        html += '<tr><td>Fecha</td><td>' + escaparHtml(contacto.fecha) + '</td></tr>';
        html += '<tr><td>Modo</td><td>' + escaparHtml(contacto.modo) + '</td></tr>';

        if (contacto.banda) {
            html += '<tr><td>Banda</td><td>' + escaparHtml(contacto.banda) + '</td></tr>';
        }

        html += '<tr><td>Grid</td><td>' + escaparHtml(contacto.localizador) + '</td></tr>';
        html += '</table>';
        html += '</div>';

        return html;
    }

    /**
     * Escapa caracteres HTML para prevenir XSS.
     * @param {string} texto - Texto a escapar.
     * @returns {string} Texto escapado.
     */
    function escaparHtml(texto) {
        if (!texto) return '';
        const div = document.createElement('div');
        div.appendChild(document.createTextNode(texto));
        return div.innerHTML;
    }

    /**
     * Carga los datos de contactos del servidor y los muestra en el mapa.
     */
    async function cargarContactos() {
        try {
            const respuesta = await fetch(URL_DATOS);

            if (!respuesta.ok) {
                throw new Error('Error al obtener datos: ' + respuesta.status);
            }

            const contactos = await respuesta.json();

            if (!contactos || contactos.length === 0) {
                elementoInfo.textContent = 'No hay contactos con localizador para mostrar en el mapa.';
                return;
            }

            const limites = [];
            const marcadores = [];

            contactos.forEach(function (contacto) {
                const posicion = [contacto.latitud, contacto.longitud];
                limites.push(posicion);

                // Crear marcador con popup.
                const marcador = L.marker(posicion)
                    .bindPopup(crearPopup(contacto))
                    .addTo(mapa);

                marcadores.push(marcador);

                // Dibujar línea desde el operador al contacto.
                L.polyline(
                    [UBICACION_OPERADOR_DEFECTO, posicion],
                    {
                        color: COLOR_LINEA,
                        weight: 1,
                        opacity: 0.4,
                        dashArray: '5, 10'
                    }
                ).addTo(mapa);
            });

            // Ajustar la vista para mostrar todos los marcadores.
            if (limites.length > 0) {
                // Incluir también la ubicación del operador en los límites.
                limites.push(UBICACION_OPERADOR_DEFECTO);
                mapa.fitBounds(limites, { padding: [30, 30] });
            }

            // Marcador especial para la ubicación del operador.
            const iconoOperador = L.divIcon({
                className: 'marcador-operador',
                html: '<div style="background:#e6a817;width:12px;height:12px;border-radius:50%;border:2px solid #fff;box-shadow:0 0 6px rgba(230,168,23,0.7);"></div>',
                iconSize: [12, 12],
                iconAnchor: [6, 6]
            });

            L.marker(UBICACION_OPERADOR_DEFECTO, { icon: iconoOperador })
                .bindPopup('<strong style="color:#e6a817;">Mi estación</strong>')
                .addTo(mapa);

            elementoInfo.textContent = contactos.length + ' contacto' +
                (contactos.length !== 1 ? 's' : '') +
                ' con localizador mostrado' +
                (contactos.length !== 1 ? 's' : '') +
                ' en el mapa.';

        } catch (error) {
            console.error('Error cargando contactos del mapa:', error);
            elementoInfo.textContent = 'Error al cargar los contactos. Intenta recargar la página.';
        }
    }

    // Cargar contactos al iniciar.
    cargarContactos();
})();
