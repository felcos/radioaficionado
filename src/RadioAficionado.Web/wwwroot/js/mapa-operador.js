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
                // Los datos del contacto (indicativo, localizador, etc.) los introduce
                // el usuario. Se construye el popup con nodos de texto (textContent) en
                // lugar de innerHTML para evitar XSS almacenado.
                const contenidoPopup = construirPopupContacto(contacto);

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

/**
 * Construye el contenido del popup de un contacto usando nodos de texto.
 * Evita XSS almacenado al no interpolar datos del usuario en innerHTML.
 * @param {object} contacto - Datos del contacto (indicativo, fecha, banda, modo, localizador).
 * @returns {HTMLElement} Elemento div listo para bindPopup.
 */
function construirPopupContacto(contacto) {
    'use strict';

    const div = document.createElement('div');
    div.className = 'text-dark';

    const titulo = document.createElement('strong');
    titulo.textContent = contacto.indicativo || '';
    div.appendChild(titulo);
    div.appendChild(document.createElement('br'));

    const fecha = document.createElement('small');
    fecha.textContent = contacto.fecha || '';
    div.appendChild(fecha);
    div.appendChild(document.createElement('br'));

    if (contacto.banda) {
        const banda = document.createElement('span');
        banda.textContent = 'Banda: ' + contacto.banda;
        div.appendChild(banda);
        div.appendChild(document.createElement('br'));
    }

    const modo = document.createElement('span');
    modo.textContent = 'Modo: ' + (contacto.modo || '');
    div.appendChild(modo);
    div.appendChild(document.createElement('br'));

    const loc = document.createElement('span');
    loc.textContent = 'Loc: ' + (contacto.localizador || '');
    div.appendChild(loc);

    return div;
}
