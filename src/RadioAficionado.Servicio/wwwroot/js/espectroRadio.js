'use strict';

/**
 * Modulo Espectro Radio — tabla visual del espectro radioeléctrico completo (0 Hz a THz).
 * Datos basados en el plan de bandas IARU Region 1 y regulaciones internacionales ITU.
 */
const EspectroRadio = (function () {

    // Cada entrada: [frecInicio, frecFin, banda, longOnda, uso, esHam, restricciones, categoria]
    // categoria: 'ham', 'broadcasting', 'aeronautico', 'militar', 'telecom', 'cientifico', 'ism', 'pirata'
    const espectro = [
        { ini: 0, fin: 9e3, banda: 'ELF', onda: '>30.000 km', uso: 'Comunicaciones militares submarinas. Ondas que atraviesan agua salada y corteza terrestre.', ham: false, restriccion: 'Uso militar estratégico', cat: 'militar' },
        { ini: 9e3, fin: 148.5e3, banda: 'VLF', onda: '33.000–2.000 km', uso: 'Radionavegación y sincronización horaria (DCF77 a 77,5 kHz). Comunicaciones submarinas.', ham: false, restriccion: 'Servicios críticos de tiempo y navegación', cat: 'cientifico' },
        { ini: 135.7e3, fin: 137.8e3, banda: '2200 m', onda: '~2.200 m', uso: 'HAM experimental. CW y digitales ultraestrechos (QRSS, WSPR). Propagación estable nocturna.', ham: true, restriccion: 'Máx. 1 W EIRP. Solo CW/digital.', cat: 'ham' },
        { ini: 148.5e3, fin: 283.5e3, banda: 'LF (Onda Larga)', onda: '2.000–1.000 m', uso: 'Radiodifusión en onda larga (France Inter, Polonia).', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 283.5e3, fin: 526.5e3, banda: 'MF baja', onda: '1.000–570 m', uso: 'NDB: balizas aeronáuticas con identificador CW para navegación (ADF).', ham: false, restriccion: 'Seguridad aérea', cat: 'aeronautico' },
        { ini: 472e3, fin: 479e3, banda: '630 m', onda: '~630 m', uso: 'HAM experimental. Propagación nocturna excelente. WSPR para experimentos.', ham: true, restriccion: 'Máx. 1 W EIRP. Solo CW/digital.', cat: 'ham' },
        { ini: 526.5e3, fin: 1800e3, banda: 'Onda Media (AM)', onda: '570–167 m', uso: 'Radiodifusión comercial AM. Emisoras locales/regionales.', ham: false, restriccion: 'Broadcasting internacional', cat: 'broadcasting' },
        { ini: 1800e3, fin: 2000e3, banda: '160 m', onda: '~160 m', uso: 'HAM. Propagación nocturna. Mucho QRN. DX de invierno. NVIS para cobertura regional.', ham: true, restriccion: 'Libre (según licencia)', cat: 'ham' },
        { ini: 2000e3, fin: 3500e3, banda: 'HF baja', onda: '150–85 m', uso: 'Radiodifusión tropical. Emisoras de onda corta para zonas rurales.', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 3500e3, fin: 4000e3, banda: '80 m', onda: '~80 m', uso: 'HAM. Ideal para NVIS (200–500 km). Redes de emergencia y DX nocturno.', ham: true, restriccion: 'Libre', cat: 'ham' },
        { ini: 4000e3, fin: 5351e3, banda: 'HF media', onda: '85–56 m', uso: 'Radiodifusión internacional (BBC, Radio Habana, Radio China).', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 5351e3, fin: 5406e3, banda: '60 m', onda: '~60 m', uso: 'HAM compartida. Propagación regional excelente. Muy valiosa en emergencias.', ham: true, restriccion: '5 canales. Máx. 15 W EIRP. SSB/digital.', cat: 'ham' },
        { ini: 5406e3, fin: 7000e3, banda: 'HF media', onda: '56–43 m', uso: 'Radiodifusión internacional (Voice of America).', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 7000e3, fin: 7300e3, banda: '40 m', onda: '~40 m', uso: 'HAM. La "banda universal". Propagación mundial día/noche. DX, concursos, emergencias.', ham: true, restriccion: 'Libre', cat: 'ham' },
        { ini: 7300e3, fin: 10100e3, banda: 'HF alta', onda: '41–30 m', uso: 'Radiodifusión internacional (Radio Exterior de España).', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 10100e3, fin: 10150e3, banda: '30 m', onda: '~30 m', uso: 'HAM. Silenciosa y estable. Solo CW y digitales (FT8, PSK31). Banda WARC.', ham: true, restriccion: 'No SSB. Máx. 200 W. No concursos.', cat: 'ham' },
        { ini: 10150e3, fin: 14000e3, banda: 'HF alta', onda: '30–21 m', uso: 'Radiodifusión internacional clásica de onda corta.', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 14000e3, fin: 14350e3, banda: '20 m', onda: '~20 m', uso: 'HAM. La "reina de HF". Abierta casi 24h. DX mundial y concursos. Todos los modos.', ham: true, restriccion: 'Libre', cat: 'ham' },
        { ini: 14350e3, fin: 18068e3, banda: 'HF muy alta', onda: '21–16 m', uso: 'Radiodifusión internacional.', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 18068e3, fin: 18168e3, banda: '17 m', onda: '~17 m', uso: 'HAM (WARC). Equilibrada, menos saturada. Excelente para DX discreto.', ham: true, restriccion: 'WARC: no concursos internacionales.', cat: 'ham' },
        { ini: 18168e3, fin: 21000e3, banda: 'HF muy alta', onda: '16–14 m', uso: 'Radiodifusión internacional.', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 21000e3, fin: 21450e3, banda: '15 m', onda: '~15 m', uso: 'HAM. Excelente en ciclos solares altos. DX mundial en picos de actividad.', ham: true, restriccion: 'Libre', cat: 'ham' },
        { ini: 21450e3, fin: 24890e3, banda: 'HF extrema', onda: '14–12 m', uso: 'Radiodifusión y servicios auxiliares.', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 24890e3, fin: 24990e3, banda: '12 m', onda: '~12 m', uso: 'HAM (WARC). Buena en propagación esporádica E y picos solares.', ham: true, restriccion: 'WARC: no concursos.', cat: 'ham' },
        { ini: 24990e3, fin: 26965e3, banda: 'HF libre', onda: '12–11 m', uso: 'Pirata. Emisoras clandestinas y radioaficionados ilegales.', ham: false, restriccion: 'Ilegal', cat: 'pirata' },
        { ini: 26965e3, fin: 27405e3, banda: '11 m (CB)', onda: '~11 m', uso: 'Banda Ciudadana. AM/FM/SSB. Popular entre camioneros. DX en ciclos solares.', ham: false, restriccion: '4 W AM/FM, 12 W SSB. Solo CB.', cat: 'ism' },
        { ini: 27405e3, fin: 28000e3, banda: 'HF libre', onda: '~11 m', uso: 'Pirata. Radioaficionados sin licencia.', ham: false, restriccion: 'Ilegal', cat: 'pirata' },
        { ini: 28000e3, fin: 29700e3, banda: '10 m', onda: '~10 m', uso: 'HAM. Muy versátil: CW, SSB, AM, FM, digitales, satélites, repetidores.', ham: true, restriccion: 'Libre', cat: 'ham' },
        { ini: 29700e3, fin: 50000e3, banda: 'VHF baja', onda: '10–6 m', uso: 'Servicios civiles y militares. Radio policía, seguridad, transporte.', ham: false, restriccion: 'Servicios de seguridad', cat: 'militar' },
        { ini: 50000e3, fin: 52000e3, banda: '6 m', onda: '~6 m', uso: 'HAM. La "magic band": DX en esporádica E y F2 solar. Concursos.', ham: true, restriccion: 'Libre', cat: 'ham' },
        { ini: 52000e3, fin: 70000e3, banda: 'VHF baja', onda: '6–4 m', uso: 'Uso militar y broadcasting. TV analógica antigua.', ham: false, restriccion: 'Gubernamental/broadcasting', cat: 'militar' },
        { ini: 70000e3, fin: 70500e3, banda: '4 m', onda: '~4 m', uso: 'HAM (solo Europa). Propagación esporádica E en verano.', ham: true, restriccion: 'No en USA. Solo modos estrechos.', cat: 'ham' },
        { ini: 70500e3, fin: 88000e3, banda: 'VHF media', onda: '4–3,4 m', uso: 'Servicios móviles, seguridad, radiotaxi.', ham: false, restriccion: 'Comercial/militar', cat: 'militar' },
        { ini: 88000e3, fin: 108000e3, banda: 'FM Comercial', onda: '3,4–2,7 m', uso: 'Radiodifusión FM. Música, noticias. DX de FM en esporádica E.', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 108000e3, fin: 137000e3, banda: 'Aeronáutica', onda: '2,7–2,2 m', uso: 'Comunicaciones aeronáuticas AM. Torre de control, aproximaciones.', ham: false, restriccion: 'Seguridad aérea crítica', cat: 'aeronautico' },
        { ini: 137000e3, fin: 144000e3, banda: 'Meteo/Satélites', onda: '2,2–2,0 m', uso: 'Satélites meteorológicos (NOAA), radiosondas, telemetría.', ham: false, restriccion: 'Satelital/meteorología', cat: 'cientifico' },
        { ini: 144000e3, fin: 146000e3, banda: '2 m', onda: '~2 m', uso: 'HAM. Banda VHF más popular. FM, repetidores, satélites LEO, SSB, EME.', ham: true, restriccion: 'Libre (EU: 144-146, USA: 144-148)', cat: 'ham' },
        { ini: 146000e3, fin: 174000e3, banda: 'VHF alta', onda: '2–1,7 m', uso: 'Servicios de emergencia, policía, radiotaxi, marítimo VHF.', ham: false, restriccion: 'Seguridad pública', cat: 'militar' },
        { ini: 174000e3, fin: 216000e3, banda: 'TV VHF', onda: '1,7–1,4 m', uso: 'TV analógica VHF alta. Hoy TDT en muchos países.', ham: false, restriccion: 'Broadcasting regulado', cat: 'broadcasting' },
        { ini: 216000e3, fin: 222000e3, banda: 'VHF alta', onda: '1,4–1,3 m', uso: 'Servicios marítimos, radiolocalización y uso gubernamental.', ham: false, restriccion: 'Gobierno/marítimo', cat: 'militar' },
        { ini: 222000e3, fin: 225000e3, banda: '1,25 m', onda: '~1,25 m', uso: 'HAM (solo ITU Reg. 2). FM, repetidores, enlaces de datos.', ham: true, restriccion: 'Solo USA/Canadá. No en Europa.', cat: 'ham' },
        { ini: 225000e3, fin: 300000e3, banda: 'UHF militar', onda: '1,3–1,0 m', uso: 'Comunicaciones militares aéreas y satelitales MILSTAR.', ham: false, restriccion: 'Uso militar exclusivo', cat: 'militar' },
        { ini: 300000e3, fin: 380000e3, banda: 'UHF bajo', onda: '1,0–0,79 m', uso: 'Comunicaciones militares y gubernamentales tácticas.', ham: false, restriccion: 'Uso militar', cat: 'militar' },
        { ini: 380000e3, fin: 430000e3, banda: 'TETRA', onda: '0,79–0,70 m', uso: 'Seguridad pública: policía, bomberos, emergencias. Digital cifrado.', ham: false, restriccion: 'Seguridad pública', cat: 'militar' },
        { ini: 430000e3, fin: 440000e3, banda: '70 cm', onda: '~70 cm', uso: 'HAM. FM, repetidores, DMR, D-STAR, C4FM, satélites, ATV, EME.', ham: true, restriccion: 'Libre (EU: 430-440, USA: 420-450)', cat: 'ham' },
        { ini: 440000e3, fin: 512000e3, banda: 'UHF móvil', onda: '0,70–0,59 m', uso: 'Trunking analógico/digital, taxis, servicios industriales.', ham: false, restriccion: 'Comercial', cat: 'telecom' },
        { ini: 512000e3, fin: 698000e3, banda: 'TV UHF', onda: '0,59–0,43 m', uso: 'Televisión UHF. TDT.', ham: false, restriccion: 'Broadcasting', cat: 'broadcasting' },
        { ini: 698000e3, fin: 960000e3, banda: 'Banda 700-900', onda: '0,43–0,31 m', uso: 'Telefonía 4G/5G, seguridad pública, celular.', ham: false, restriccion: 'Telecom regulada', cat: 'telecom' },
        { ini: 902000e3, fin: 928000e3, banda: '33 cm', onda: '~33 cm', uso: 'HAM (solo USA/Canadá). Enlaces digitales, ATV. Comparte con ISM.', ham: true, restriccion: 'Solo ITU Reg. 2. No en Europa.', cat: 'ham' },
        { ini: 960000e3, fin: 1240000e3, banda: 'Radar/Aero', onda: '31–24 cm', uso: 'Radar aeronáutico, DME, TACAN, SSR. Seguridad aérea crítica.', ham: false, restriccion: 'Seguridad aérea', cat: 'aeronautico' },
        { ini: 1240000e3, fin: 1300000e3, banda: '23 cm', onda: '~23 cm', uso: 'HAM. ATV, enlaces digitales, EME, satélites LEO. Antenas direccionales.', ham: true, restriccion: 'Solapa con Galileo GNSS en EU.', cat: 'ham' },
        { ini: 1300000e3, fin: 1525000e3, banda: 'Radar/Telecom', onda: '23–20 cm', uso: 'Radares, radioastronomía (línea H 1420 MHz), LTE.', ham: false, restriccion: 'Militar/científico/telecom', cat: 'cientifico' },
        { ini: 1525000e3, fin: 1660000e3, banda: 'Banda L', onda: '20–18 cm', uso: 'Servicios satelitales (Inmarsat, GPS L1 a 1575 MHz).', ham: false, restriccion: 'GPS/satelital crítico', cat: 'telecom' },
        { ini: 1660000e3, fin: 1710000e3, banda: 'Radioastronomía', onda: '~18 cm', uso: 'Radioastronomía (línea OH a 1667 MHz). Protección estricta.', ham: false, restriccion: 'Protegida', cat: 'cientifico' },
        { ini: 1710000e3, fin: 2300000e3, banda: 'Banda AWS/PCS', onda: '18–13 cm', uso: 'Telefonía celular 4G/5G bandas AWS, PCS (1800-2100 MHz).', ham: false, restriccion: 'Telecom regulada', cat: 'telecom' },
        { ini: 2300000e3, fin: 2450000e3, banda: '13 cm', onda: '~13 cm', uso: 'HAM. ATV, enlaces digitales, EME. Comparte con WiFi/Bluetooth.', ham: true, restriccion: 'Compartida con ISM (WiFi 2.4 GHz).', cat: 'ham' },
        { ini: 2450000e3, fin: 2500000e3, banda: 'ISM 2.4 GHz', onda: '~12 cm', uso: 'WiFi, Bluetooth, microondas domésticos, ZigBee, IoT.', ham: false, restriccion: 'ISM sin licencia', cat: 'ism' },
        { ini: 2500000e3, fin: 3300000e3, banda: 'Banda S', onda: '12–9 cm', uso: 'Radar meteorológico, WiMAX, 5G n78, enlace descendente satélites.', ham: false, restriccion: 'Telecom/científico', cat: 'telecom' },
        { ini: 3300000e3, fin: 3500000e3, banda: '9 cm', onda: '~9 cm', uso: 'HAM. Concursos de microondas y enlaces digitales.', ham: true, restriccion: 'Compartida con 5G. Retirándose.', cat: 'ham' },
        { ini: 3500000e3, fin: 5650000e3, banda: 'Banda C', onda: '9–5 cm', uso: 'Radar ATC, satélites (banda C), 5G FR1 (3.5-4.2 GHz), WiFi 5 GHz.', ham: false, restriccion: 'Telecom/radar', cat: 'telecom' },
        { ini: 5650000e3, fin: 5850000e3, banda: '6 cm', onda: '~6 cm', uso: 'HAM. ATV, concursos microondas. Comparte con WiFi 5 GHz.', ham: true, restriccion: 'Compartida con ISM.', cat: 'ham' },
        { ini: 5850000e3, fin: 10000000e3, banda: 'Banda X', onda: '5–3 cm', uso: 'Radar militar/marítimo, comunicaciones satelitales militares, banda X.', ham: false, restriccion: 'Militar/radar', cat: 'militar' },
        { ini: 10000000e3, fin: 10500000e3, banda: '3 cm', onda: '~3 cm', uso: 'HAM. Récords DX en microondas, ductos troposféricos, EME. Parábolas.', ham: true, restriccion: 'Libre en segmentos HAM.', cat: 'ham' },
        { ini: 10500000e3, fin: 24000000e3, banda: 'Banda Ku/K', onda: '3–1,2 cm', uso: 'Satélites TV directa (Ku 12-18 GHz), radar velocidad policial (K 24 GHz).', ham: false, restriccion: 'Telecom/satelital', cat: 'telecom' },
        { ini: 24000000e3, fin: 24250000e3, banda: '1,2 cm', onda: '~1,2 cm', uso: 'HAM. Concursos de microondas. Línea directa (pocos km).', ham: true, restriccion: 'Potencias muy bajas.', cat: 'ham' },
        { ini: 24250000e3, fin: 47000000e3, banda: 'Banda Ka/EHF', onda: '1,2 cm–6 mm', uso: 'Satélites Ka (26-40 GHz), 5G FR2 mmWave (28/39 GHz), radar científico.', ham: false, restriccion: 'Telecom/científico', cat: 'telecom' },
        { ini: 47000000e3, fin: 47200000e3, banda: '6 mm', onda: '~6 mm', uso: 'HAM. Récords de microondas. Muy afectada por atmósfera.', ham: true, restriccion: 'Solo algunos países.', cat: 'ham' },
        { ini: 47200000e3, fin: 76000000e3, banda: 'EHF', onda: '6–4 mm', uso: 'Radar automotriz (77 GHz), radioastronomía, comunicaciones militares.', ham: false, restriccion: 'Científico/militar', cat: 'cientifico' },
        { ini: 76000000e3, fin: 81000000e3, banda: '4 mm', onda: '~4 mm', uso: 'HAM. Banda experimental de microondas extremas.', ham: true, restriccion: 'Experimental, pocos países.', cat: 'ham' },
        { ini: 81000000e3, fin: 122000000e3, banda: 'THF baja', onda: '4–2,4 mm', uso: 'Radar automotriz alta resolución, escaneo seguridad aeropuertos, radioastronomía.', ham: false, restriccion: 'Científico/industrial', cat: 'cientifico' },
        { ini: 122000000e3, fin: 123000000e3, banda: '2,4 mm', onda: '~2,4 mm', uso: 'HAM experimental. Absorbida por vapor de agua.', ham: true, restriccion: 'Experimental, potencias ultrabajas.', cat: 'ham' },
        { ini: 123000000e3, fin: 241000000e3, banda: 'Sub-THz', onda: '2,4–1,2 mm', uso: 'Radioastronomía, espectroscopia molecular, imágenes médicas experimentales.', ham: false, restriccion: 'Científico', cat: 'cientifico' },
        { ini: 241000000e3, fin: 250000000e3, banda: '1,24 mm', onda: '~1,2 mm', uso: 'HAM experimental. Récords en ultracorta distancia.', ham: true, restriccion: 'Experimental, autorizaciones especiales.', cat: 'ham' },
        { ini: 250000000e3, fin: 3000000000e3, banda: 'Infrarrojo', onda: '1,2 mm–100 µm', uso: 'Astronomía infrarroja, comunicaciones ópticas, espectroscopia. Fin del espectro radio.', ham: false, restriccion: 'Científico', cat: 'cientifico' }
    ];

    // Colores por categoría
    const colores = {
        'ham': { fondo: '#064e3b', borde: '#10b981', texto: '#6ee7b7' },
        'broadcasting': { fondo: '#1e1b4b', borde: '#818cf8', texto: '#a5b4fc' },
        'aeronautico': { fondo: '#7c2d12', borde: '#f97316', texto: '#fdba74' },
        'militar': { fondo: '#450a0a', borde: '#ef4444', texto: '#fca5a5' },
        'telecom': { fondo: '#172554', borde: '#3b82f6', texto: '#93c5fd' },
        'cientifico': { fondo: '#3b0764', borde: '#a855f7', texto: '#d8b4fe' },
        'ism': { fondo: '#422006', borde: '#d97706', texto: '#fcd34d' },
        'pirata': { fondo: '#1c1917', borde: '#78716c', texto: '#a8a29e' }
    };

    function init() {
        renderizarTabla();
        configurarFiltros();
    }

    function formatearFrecuencia(hz) {
        if (hz >= 1e12) { return (hz / 1e12).toFixed(1) + ' THz'; }
        if (hz >= 1e9) { return (hz / 1e9).toFixed(hz >= 10e9 ? 0 : 1) + ' GHz'; }
        if (hz >= 1e6) { return (hz / 1e6).toFixed(hz >= 100e6 ? 0 : 1) + ' MHz'; }
        if (hz >= 1e3) { return (hz / 1e3).toFixed(hz >= 100e3 ? 0 : 1) + ' kHz'; }
        return hz + ' Hz';
    }

    function renderizarTabla() {
        const tbody = document.getElementById('espectro-tbody');
        if (!tbody) { return; }
        tbody.innerHTML = '';

        const filtroHam = document.getElementById('espectro-filtro-ham');
        const soloHam = filtroHam ? filtroHam.checked : false;

        const filtroCat = document.getElementById('espectro-filtro-cat');
        const catSeleccionada = filtroCat ? filtroCat.value : '';

        const filtroBuscar = document.getElementById('espectro-buscar');
        const textoBusqueda = filtroBuscar ? filtroBuscar.value.toLowerCase() : '';

        let contadorHam = 0;
        let contadorTotal = 0;

        for (let i = 0; i < espectro.length; i++) {
            const e = espectro[i];

            if (soloHam && !e.ham) { continue; }
            if (catSeleccionada && e.cat !== catSeleccionada) { continue; }
            if (textoBusqueda && !(
                e.banda.toLowerCase().includes(textoBusqueda) ||
                e.uso.toLowerCase().includes(textoBusqueda) ||
                e.restriccion.toLowerCase().includes(textoBusqueda)
            )) { continue; }

            contadorTotal++;
            if (e.ham) { contadorHam++; }

            const color = colores[e.cat] || colores['militar'];
            const tr = document.createElement('tr');
            tr.style.backgroundColor = color.fondo;
            tr.style.borderLeft = '3px solid ' + color.borde;

            const frecTexto = formatearFrecuencia(e.ini) + ' – ' + formatearFrecuencia(e.fin);
            const hamBadge = e.ham
                ? '<span class="espectro-badge espectro-badge-ham">HAM</span>'
                : '<span class="espectro-badge espectro-badge-no">NO</span>';
            const catBadge = '<span class="espectro-badge-cat" style="color:' + color.texto + '">' + e.cat + '</span>';

            tr.innerHTML =
                '<td class="espectro-freq">' + frecTexto + '</td>' +
                '<td><strong style="color:' + color.texto + '">' + e.banda + '</strong></td>' +
                '<td class="espectro-onda">' + e.onda + '</td>' +
                '<td class="espectro-uso">' + e.uso + '</td>' +
                '<td class="espectro-ham-col">' + hamBadge + '</td>' +
                '<td class="espectro-restriccion">' + e.restriccion + ' ' + catBadge + '</td>';

            tbody.appendChild(tr);
        }

        const elemTotal = document.getElementById('espectro-total');
        const elemHam = document.getElementById('espectro-ham-count');
        if (elemTotal) { elemTotal.textContent = contadorTotal; }
        if (elemHam) { elemHam.textContent = contadorHam; }
    }

    function configurarFiltros() {
        const filtroHam = document.getElementById('espectro-filtro-ham');
        const filtroCat = document.getElementById('espectro-filtro-cat');
        const filtroBuscar = document.getElementById('espectro-buscar');

        if (filtroHam) { filtroHam.addEventListener('change', renderizarTabla); }
        if (filtroCat) { filtroCat.addEventListener('change', renderizarTabla); }
        if (filtroBuscar) { filtroBuscar.addEventListener('input', renderizarTabla); }
    }

    return { init: init };
})();

document.addEventListener('DOMContentLoaded', EspectroRadio.init);
