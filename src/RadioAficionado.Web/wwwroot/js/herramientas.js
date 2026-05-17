'use strict';

/**
 * Modulo de herramientas para radioaficionados.
 * Conversores de potencia, distancia entre grids, Maidenhead, etc.
 * Todo funciona en el cliente sin necesidad de backend.
 */
const Herramientas = (function () {

    // ========== Constantes ==========
    const RADIO_TIERRA_KM = 6371.0;
    const KM_A_MILLAS = 0.621371;

    // ========== Conversor de potencia ==========

    /**
     * Convierte dBm a Watts.
     * @param {number} dbm - Valor en dBm.
     * @returns {number} Valor en Watts.
     */
    function dbmAWatts(dbm) {
        return Math.pow(10, (dbm - 30) / 10);
    }

    /**
     * Convierte Watts a dBm.
     * @param {number} watts - Valor en Watts.
     * @returns {number} Valor en dBm.
     */
    function wattsADbm(watts) {
        if (watts <= 0) return -Infinity;
        return 10 * Math.log10(watts) + 30;
    }

    /**
     * Convierte mW a Watts.
     * @param {number} mw - Valor en milivatios.
     * @returns {number} Valor en Watts.
     */
    function mwAWatts(mw) {
        return mw / 1000;
    }

    /**
     * Convierte Watts a mW.
     * @param {number} watts - Valor en Watts.
     * @returns {number} Valor en milivatios.
     */
    function wattsAMw(watts) {
        return watts * 1000;
    }

    /**
     * Formatea un numero con precision adecuada.
     * @param {number} valor - Numero a formatear.
     * @param {number} decimales - Decimales maximos.
     * @returns {string} Numero formateado.
     */
    function formatearNumero(valor, decimales) {
        if (!isFinite(valor)) return '';
        if (Math.abs(valor) < 0.001) return valor.toExponential(2);
        return parseFloat(valor.toFixed(decimales)).toString();
    }

    let actualizandoPotencia = false;

    function inicializarConversorPotencia() {
        const inputDbm = document.getElementById('potencia-dbm');
        const inputWatts = document.getElementById('potencia-watts');
        const inputMw = document.getElementById('potencia-mw');

        if (!inputDbm || !inputWatts || !inputMw) return;

        inputDbm.addEventListener('input', function () {
            if (actualizandoPotencia) return;
            actualizandoPotencia = true;
            const dbm = parseFloat(this.value);
            if (!isNaN(dbm)) {
                const watts = dbmAWatts(dbm);
                inputWatts.value = formatearNumero(watts, 6);
                inputMw.value = formatearNumero(wattsAMw(watts), 3);
            } else {
                inputWatts.value = '';
                inputMw.value = '';
            }
            actualizandoPotencia = false;
        });

        inputWatts.addEventListener('input', function () {
            if (actualizandoPotencia) return;
            actualizandoPotencia = true;
            const watts = parseFloat(this.value);
            if (!isNaN(watts) && watts > 0) {
                inputDbm.value = formatearNumero(wattsADbm(watts), 2);
                inputMw.value = formatearNumero(wattsAMw(watts), 3);
            } else {
                inputDbm.value = '';
                inputMw.value = '';
            }
            actualizandoPotencia = false;
        });

        inputMw.addEventListener('input', function () {
            if (actualizandoPotencia) return;
            actualizandoPotencia = true;
            const mw = parseFloat(this.value);
            if (!isNaN(mw) && mw > 0) {
                const watts = mwAWatts(mw);
                inputWatts.value = formatearNumero(watts, 6);
                inputDbm.value = formatearNumero(wattsADbm(watts), 2);
            } else {
                inputWatts.value = '';
                inputDbm.value = '';
            }
            actualizandoPotencia = false;
        });
    }

    // ========== Grid Maidenhead ==========

    /**
     * Convierte un grid locator Maidenhead a coordenadas (lat, lng).
     * Soporta 4 y 6 caracteres.
     * @param {string} grid - Grid locator (ej: "IN80", "IN80dk").
     * @returns {{lat: number, lng: number}|null} Coordenadas o null si es invalido.
     */
    function gridACoordenadas(grid) {
        if (!grid || typeof grid !== 'string') return null;
        grid = grid.trim().toUpperCase();

        if (grid.length < 4) return null;
        if (!/^[A-R]{2}[0-9]{2}/.test(grid)) return null;

        const lng = (grid.charCodeAt(0) - 65) * 20 - 180;
        const lat = (grid.charCodeAt(1) - 65) * 10 - 90;
        const lngCuadrado = parseInt(grid[2], 10) * 2;
        const latCuadrado = parseInt(grid[3], 10) * 1;

        let lngFinal = lng + lngCuadrado;
        let latFinal = lat + latCuadrado;

        if (grid.length >= 6 && /^[A-R]{2}[0-9]{2}[A-X]{2}$/.test(grid)) {
            const lngSub = (grid.charCodeAt(4) - 65) * (2 / 24);
            const latSub = (grid.charCodeAt(5) - 65) * (1 / 24);
            lngFinal += lngSub + (1 / 24);
            latFinal += latSub + (0.5 / 24);
        } else {
            lngFinal += 1;
            latFinal += 0.5;
        }

        return { lat: latFinal, lng: lngFinal };
    }

    /**
     * Convierte coordenadas (lat, lng) a grid locator Maidenhead de 6 caracteres.
     * @param {number} lat - Latitud.
     * @param {number} lng - Longitud.
     * @returns {string} Grid locator de 6 caracteres.
     */
    function coordenadasAGrid(lat, lng) {
        let lngAjustada = lng + 180;
        let latAjustada = lat + 90;

        const campo1 = String.fromCharCode(65 + Math.floor(lngAjustada / 20));
        const campo2 = String.fromCharCode(65 + Math.floor(latAjustada / 10));

        lngAjustada = lngAjustada % 20;
        latAjustada = latAjustada % 10;

        const cuadrado1 = Math.floor(lngAjustada / 2).toString();
        const cuadrado2 = Math.floor(latAjustada / 1).toString();

        lngAjustada = lngAjustada % 2;
        latAjustada = latAjustada % 1;

        const sub1 = String.fromCharCode(97 + Math.floor(lngAjustada / (2 / 24)));
        const sub2 = String.fromCharCode(97 + Math.floor(latAjustada / (1 / 24)));

        return campo1 + campo2 + cuadrado1 + cuadrado2 + sub1 + sub2;
    }

    /**
     * Convierte grados a radianes.
     * @param {number} grados - Angulo en grados.
     * @returns {number} Angulo en radianes.
     */
    function gradosARadianes(grados) {
        return grados * Math.PI / 180;
    }

    /**
     * Convierte radianes a grados.
     * @param {number} radianes - Angulo en radianes.
     * @returns {number} Angulo en grados.
     */
    function radianesAGrados(radianes) {
        return radianes * 180 / Math.PI;
    }

    /**
     * Calcula la distancia entre dos puntos usando la formula de Haversine.
     * @param {number} lat1 - Latitud del punto 1.
     * @param {number} lng1 - Longitud del punto 1.
     * @param {number} lat2 - Latitud del punto 2.
     * @param {number} lng2 - Longitud del punto 2.
     * @returns {number} Distancia en kilometros.
     */
    function calcularDistanciaHaversine(lat1, lng1, lat2, lng2) {
        const dLat = gradosARadianes(lat2 - lat1);
        const dLng = gradosARadianes(lng2 - lng1);
        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(gradosARadianes(lat1)) * Math.cos(gradosARadianes(lat2)) *
            Math.sin(dLng / 2) * Math.sin(dLng / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        return RADIO_TIERRA_KM * c;
    }

    /**
     * Calcula el azimut (bearing) del punto 1 al punto 2.
     * @param {number} lat1 - Latitud del punto 1.
     * @param {number} lng1 - Longitud del punto 1.
     * @param {number} lat2 - Latitud del punto 2.
     * @param {number} lng2 - Longitud del punto 2.
     * @returns {number} Azimut en grados (0-360).
     */
    function calcularAzimut(lat1, lng1, lat2, lng2) {
        const lat1Rad = gradosARadianes(lat1);
        const lat2Rad = gradosARadianes(lat2);
        const dLng = gradosARadianes(lng2 - lng1);

        const x = Math.sin(dLng) * Math.cos(lat2Rad);
        const y = Math.cos(lat1Rad) * Math.sin(lat2Rad) -
            Math.sin(lat1Rad) * Math.cos(lat2Rad) * Math.cos(dLng);

        let azimut = radianesAGrados(Math.atan2(x, y));
        return (azimut + 360) % 360;
    }

    function inicializarCalculadoraDistancia() {
        const inputGridA = document.getElementById('distancia-grid-a');
        const inputGridB = document.getElementById('distancia-grid-b');
        const btnCalcular = document.getElementById('btn-calcular-distancia');
        const divResultado = document.getElementById('resultado-distancia');

        if (!inputGridA || !inputGridB || !btnCalcular || !divResultado) return;

        btnCalcular.addEventListener('click', function () {
            const coordA = gridACoordenadas(inputGridA.value);
            const coordB = gridACoordenadas(inputGridB.value);

            if (!coordA || !coordB) {
                divResultado.innerHTML = '<span class="text-danger">Grid locator no valido. Usa formato de 4 o 6 caracteres (ej: IN80 o IN80dk).</span>';
                return;
            }

            const distanciaKm = calcularDistanciaHaversine(coordA.lat, coordA.lng, coordB.lat, coordB.lng);
            const distanciaMillas = distanciaKm * KM_A_MILLAS;
            const azimutAB = calcularAzimut(coordA.lat, coordA.lng, coordB.lat, coordB.lng);
            const azimutBA = calcularAzimut(coordB.lat, coordB.lng, coordA.lat, coordA.lng);

            divResultado.innerHTML =
                '<div class="mb-1"><strong>Distancia:</strong> ' + distanciaKm.toFixed(1) + ' km / ' + distanciaMillas.toFixed(1) + ' mi</div>' +
                '<div class="mb-1"><strong>Azimut A&rarr;B:</strong> ' + azimutAB.toFixed(1) + '&deg;</div>' +
                '<div class="mb-1"><strong>Azimut B&rarr;A:</strong> ' + azimutBA.toFixed(1) + '&deg;</div>' +
                '<div class="mb-1"><strong>Coordenadas A:</strong> ' + coordA.lat.toFixed(4) + ', ' + coordA.lng.toFixed(4) + '</div>' +
                '<div class="mb-1"><strong>Coordenadas B:</strong> ' + coordB.lat.toFixed(4) + ', ' + coordB.lng.toFixed(4) + '</div>';
        });
    }

    // ========== Conversor Maidenhead <-> Coordenadas ==========

    function inicializarConversorMaidenhead() {
        const inputGrid = document.getElementById('maidenhead-grid');
        const btnGridACoord = document.getElementById('btn-grid-a-coord');
        const resultadoGrid = document.getElementById('resultado-grid-coord');

        const inputLat = document.getElementById('maidenhead-lat');
        const inputLng = document.getElementById('maidenhead-lng');
        const btnCoordAGrid = document.getElementById('btn-coord-a-grid');
        const resultadoCoord = document.getElementById('resultado-coord-grid');

        const btnMiUbicacion = document.getElementById('btn-mi-ubicacion');

        if (btnGridACoord && inputGrid && resultadoGrid) {
            btnGridACoord.addEventListener('click', function () {
                const coord = gridACoordenadas(inputGrid.value);
                if (!coord) {
                    resultadoGrid.innerHTML = '<span class="text-danger">Grid no valido.</span>';
                    return;
                }
                resultadoGrid.innerHTML =
                    '<strong>Latitud:</strong> ' + coord.lat.toFixed(6) + '&deg; / <strong>Longitud:</strong> ' + coord.lng.toFixed(6) + '&deg;';
            });
        }

        if (btnCoordAGrid && inputLat && inputLng && resultadoCoord) {
            btnCoordAGrid.addEventListener('click', function () {
                const lat = parseFloat(inputLat.value);
                const lng = parseFloat(inputLng.value);
                if (isNaN(lat) || isNaN(lng) || lat < -90 || lat > 90 || lng < -180 || lng > 180) {
                    resultadoCoord.innerHTML = '<span class="text-danger">Coordenadas no validas (lat: -90 a 90, lng: -180 a 180).</span>';
                    return;
                }
                const grid = coordenadasAGrid(lat, lng);
                resultadoCoord.innerHTML = '<strong>Grid Maidenhead:</strong> ' + grid;
            });
        }

        if (btnMiUbicacion) {
            btnMiUbicacion.addEventListener('click', function () {
                if (!navigator.geolocation) {
                    if (resultadoCoord) {
                        resultadoCoord.innerHTML = '<span class="text-danger">Geolocalizacion no disponible en este navegador.</span>';
                    }
                    return;
                }
                navigator.geolocation.getCurrentPosition(
                    function (posicion) {
                        const lat = posicion.coords.latitude;
                        const lng = posicion.coords.longitude;
                        if (inputLat) inputLat.value = lat.toFixed(6);
                        if (inputLng) inputLng.value = lng.toFixed(6);
                        const grid = coordenadasAGrid(lat, lng);
                        if (resultadoCoord) {
                            resultadoCoord.innerHTML = '<strong>Grid Maidenhead:</strong> ' + grid;
                        }
                    },
                    function (error) {
                        if (resultadoCoord) {
                            resultadoCoord.innerHTML = '<span class="text-danger">Error al obtener ubicacion: ' + error.message + '</span>';
                        }
                    }
                );
            });
        }
    }

    // ========== Inicializacion ==========

    function inicializar() {
        inicializarConversorPotencia();
        inicializarCalculadoraDistancia();
        inicializarConversorMaidenhead();
    }

    // Ejecutar al cargar el DOM
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', inicializar);
    } else {
        inicializar();
    }

    // API publica (por si se necesita desde otros modulos)
    return {
        gridACoordenadas: gridACoordenadas,
        coordenadasAGrid: coordenadasAGrid,
        calcularDistanciaHaversine: calcularDistanciaHaversine,
        calcularAzimut: calcularAzimut,
        dbmAWatts: dbmAWatts,
        wattsADbm: wattsADbm
    };

})();
