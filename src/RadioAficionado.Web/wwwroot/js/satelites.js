'use strict';

/**
 * Modulo de Satelites — informacion estatica de satelites de radioaficion.
 * Datos de satelites comunes con frecuencias, modos y estado.
 */
const Satelites = (function () {

    const listaSatelites = [
        {
            nombre: 'ISS (ZARYA)',
            norad: 25544,
            tipo: 'Tripulado',
            modo: 'FM Voice, APRS, SSTV',
            uplink: '145.990 MHz',
            downlink: '145.800 MHz',
            estado: 'Activo',
            orbita: 'LEO ~408 km',
            descripcion: 'Estacion Espacial Internacional. ARISS permite contactos con astronautas. APRS digipeater en 145.825 MHz.'
        },
        {
            nombre: 'AO-91 (Fox-1B)',
            norad: 43017,
            tipo: 'FM Repeater',
            modo: 'FM U/V',
            uplink: '435.250 MHz (67.0 Hz CTCSS)',
            downlink: '145.960 MHz',
            estado: 'Activo',
            orbita: 'LEO ~450 km, SSO',
            descripcion: 'Repetidor FM amateur. Activado por iluminacion solar. Popular para primeros contactos via satelite.'
        },
        {
            nombre: 'AO-92 (Fox-1D)',
            norad: 43137,
            tipo: 'FM Repeater / L-band',
            modo: 'FM U/V, L/V',
            uplink: '435.350 MHz (67.0 Hz CTCSS)',
            downlink: '145.880 MHz',
            estado: 'Intermitente',
            orbita: 'LEO ~450 km, SSO',
            descripcion: 'Repetidor FM con modo L-band experimental (1267.350 MHz uplink).'
        },
        {
            nombre: 'SO-50 (SaudiSat-1C)',
            norad: 27607,
            tipo: 'FM Repeater',
            modo: 'FM V/U',
            uplink: '145.850 MHz (67.0 Hz CTCSS)',
            downlink: '436.795 MHz',
            estado: 'Activo',
            orbita: 'LEO ~700 km',
            descripcion: 'Uno de los satelites FM mas antiguos activos. Requiere tono 74.4 Hz para activar (10s) y luego 67.0 Hz.'
        },
        {
            nombre: 'RS-44 (DOSAAF-85)',
            norad: 44909,
            tipo: 'Transpondedor Lineal',
            modo: 'SSB/CW V/U',
            uplink: '145.935-145.995 MHz',
            downlink: '435.610-435.670 MHz (invertido)',
            estado: 'Activo',
            orbita: 'LEO ~1500 km',
            descripcion: 'Transpondedor lineal invertido. Orbita alta permite pases largos (hasta 20 min). Excelente para SSB/CW.'
        },
        {
            nombre: 'CAS-4A (ZHUHAI-1 01)',
            norad: 42761,
            tipo: 'Transpondedor Lineal',
            modo: 'SSB/CW V/U',
            uplink: '145.855-145.875 MHz',
            downlink: '435.210-435.230 MHz',
            estado: 'Activo',
            orbita: 'LEO ~500 km, SSO',
            descripcion: 'Transpondedor lineal chino. Ancho de banda de 20 kHz. Buenos para CW y SSB.'
        },
        {
            nombre: 'CAS-4B (ZHUHAI-1 02)',
            norad: 42759,
            tipo: 'Transpondedor Lineal',
            modo: 'SSB/CW V/U',
            uplink: '145.880-145.900 MHz',
            downlink: '435.280-435.300 MHz',
            estado: 'Activo',
            orbita: 'LEO ~500 km, SSO',
            descripcion: 'Transpondedor lineal chino identico a CAS-4A. Ideal para contactos SSB/CW.'
        },
        {
            nombre: 'QO-100 (Es\'hail 2)',
            norad: 43700,
            tipo: 'Transpondedor GEO',
            modo: 'SSB/CW/Digital',
            uplink: '2400.050-2400.300 MHz',
            downlink: '10489.550-10489.800 MHz',
            estado: 'Activo',
            orbita: 'GEO 25.9E',
            descripcion: 'Primer satelite geoestacionario amateur. Cobertura desde Brasil hasta Tailandia. Banda estrecha y DATV.'
        },
        {
            nombre: 'IO-117 (GreenCube)',
            norad: 53106,
            tipo: 'Digipeater',
            modo: 'Digipeater UHF',
            uplink: '435.310 MHz',
            downlink: '435.310 MHz',
            estado: 'Activo',
            orbita: 'MEO ~6000 km',
            descripcion: 'Digipeater en orbita media. Pases muy largos. Protocolo store-and-forward.'
        },
        {
            nombre: 'TEVEL (1-8)',
            norad: 0,
            tipo: 'FM Repeater',
            modo: 'FM V/U',
            uplink: '145.970 MHz (67.0 Hz CTCSS)',
            downlink: '436.400 MHz',
            estado: 'Activo',
            orbita: 'LEO ~550 km',
            descripcion: 'Constelacion de 8 cubesats israelies con repetidores FM. Se turnan activacion.'
        },
        {
            nombre: 'NOAA-15',
            norad: 25338,
            tipo: 'Meteorologico',
            modo: 'APT / HRPT',
            uplink: 'N/A',
            downlink: '137.620 MHz (APT)',
            estado: 'Activo (degradado)',
            orbita: 'LEO ~810 km, SSO',
            descripcion: 'Satelite meteorologico. Transmite imagenes APT que pueden recibirse con antena simple y RTL-SDR.'
        },
        {
            nombre: 'NOAA-18',
            norad: 28654,
            tipo: 'Meteorologico',
            modo: 'APT / HRPT',
            uplink: 'N/A',
            downlink: '137.9125 MHz (APT)',
            estado: 'Activo',
            orbita: 'LEO ~850 km, SSO',
            descripcion: 'Satelite meteorologico con APT. Excelente para recepcion con SDR y antena V-dipole.'
        },
        {
            nombre: 'NOAA-19',
            norad: 33591,
            tipo: 'Meteorologico',
            modo: 'APT / HRPT',
            uplink: 'N/A',
            downlink: '137.100 MHz (APT)',
            estado: 'Activo',
            orbita: 'LEO ~870 km, SSO',
            descripcion: 'Ultimo satelite NOAA con APT. Imagenes visibles/infrarrojas en tiempo real.'
        }
    ];

    function init() {
        renderizarTabla();
        configurarFiltros();
    }

    function renderizarTabla() {
        const tbody = document.getElementById('satelites-tbody');
        if (!tbody) { return; }
        tbody.innerHTML = '';

        const filtroTipo = document.getElementById('satelites-filtro-tipo');
        const tipoSeleccionado = filtroTipo ? filtroTipo.value : '';

        const filtroBuscar = document.getElementById('satelites-buscar');
        const textoBusqueda = filtroBuscar ? filtroBuscar.value.toLowerCase() : '';

        for (let i = 0; i < listaSatelites.length; i++) {
            const s = listaSatelites[i];

            if (tipoSeleccionado && s.tipo.toLowerCase().indexOf(tipoSeleccionado.toLowerCase()) === -1) { continue; }
            if (textoBusqueda && !(
                s.nombre.toLowerCase().includes(textoBusqueda) ||
                s.modo.toLowerCase().includes(textoBusqueda) ||
                s.descripcion.toLowerCase().includes(textoBusqueda)
            )) { continue; }

            const tr = document.createElement('tr');

            const estadoBadge = s.estado === 'Activo'
                ? '<span class="badge bg-success">Activo</span>'
                : (s.estado === 'Intermitente'
                    ? '<span class="badge bg-warning text-dark">Intermitente</span>'
                    : '<span class="badge bg-secondary">' + s.estado + '</span>');

            tr.innerHTML =
                '<td><strong>' + s.nombre + '</strong></td>' +
                '<td><span class="badge bg-info text-dark">' + s.tipo + '</span></td>' +
                '<td style="font-size:0.85rem;">' + s.modo + '</td>' +
                '<td style="font-size:0.85rem; font-family:monospace;">' + s.uplink + '</td>' +
                '<td style="font-size:0.85rem; font-family:monospace;">' + s.downlink + '</td>' +
                '<td>' + estadoBadge + '</td>' +
                '<td style="font-size:0.85rem;">' + s.orbita + '</td>';

            // Tooltip con descripcion
            tr.setAttribute('title', s.descripcion);
            tr.style.cursor = 'pointer';

            tbody.appendChild(tr);
        }
    }

    function configurarFiltros() {
        const filtroTipo = document.getElementById('satelites-filtro-tipo');
        const filtroBuscar = document.getElementById('satelites-buscar');

        if (filtroTipo) { filtroTipo.addEventListener('change', renderizarTabla); }
        if (filtroBuscar) { filtroBuscar.addEventListener('input', renderizarTabla); }
    }

    return {
        init: init,
        satelites: listaSatelites
    };
})();

document.addEventListener('DOMContentLoaded', Satelites.init);
