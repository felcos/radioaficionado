'use strict';

/**
 * Script para cargar y renderizar el mapa de contactos de un operador
 * usando Leaflet con datos obtenidos del endpoint MapaDatosOperador.
 */

/**
 * Inicializa el mapa de contactos del operador.
 * @param {string} indicativo - Indicativo del operador cuyos contactos se mostrarán.
 */
function inicializarMapaOperador(indicativo) {
    const contenedorMapa = document.getElementById('mapa-contactos-operador');

    if (!contenedorMapa) {
        return;
    }

    const mapa = L.map('mapa-contactos-operador').setView([20, 0], 2);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 18
    }).addTo(mapa);

    const urlDatos = '/Operadores/MapaDatosOperador?indicativo=' + encodeURIComponent(indicativo);

    fetch(urlDatos)
        .then(function (respuesta) {
            if (!respuesta.ok) {
                throw new Error('Error al obtener datos del mapa: ' + respuesta.status);
            }
            return respuesta.json();
        })
        .then(function (marcadores) {
            if (!marcadores || marcadores.length === 0) {
                contenedorMapa.innerHTML = '<div class="text-center text-ra-suave py-5">No hay contactos con localizador para mostrar en el mapa.</div>';
                return;
            }

            const grupoMarcadores = L.featureGroup();

            marcadores.forEach(function (contacto) {
                const contenidoPopup =
                    '<div class="text-dark">' +
                    '<strong>' + contacto.indicativo + '</strong><br/>' +
                    '<small>' + contacto.fecha + '</small><br/>' +
                    (contacto.banda ? '<span>Banda: ' + contacto.banda + '</span><br/>' : '') +
                    '<span>Modo: ' + contacto.modo + '</span><br/>' +
                    '<span>Loc: ' + contacto.localizador + '</span>' +
                    '</div>';

                const marcador = L.marker([contacto.latitud, contacto.longitud])
                    .bindPopup(contenidoPopup);

                grupoMarcadores.addLayer(marcador);
            });

            grupoMarcadores.addTo(mapa);
            mapa.fitBounds(grupoMarcadores.getBounds().pad(0.1));
        })
        .catch(function (error) {
            console.error('Error cargando mapa de contactos:', error);
            contenedorMapa.innerHTML = '<div class="text-center text-danger py-5">Error al cargar el mapa de contactos.</div>';
        });
}
