# Investigacion: Panorama del Software de Radioaficion (Ham Radio)

**Fecha:** 2026-03-22
**Proposito:** Informar la arquitectura de una nueva plataforma integral de radioaficion.

---

## 1. Software de Escritorio — Principales Competidores

### 1.1 WSJT-X (El estandar de facto para modos debiles)

**Que hace:** Software de comunicacion digital que implementa 11 protocolos/modos optimizados para senales debiles.

**Protocolos soportados:**
| Protocolo | Ciclo T/R | Uso principal | Sensibilidad |
|-----------|-----------|---------------|-------------|
| **FT8** | 15 seg | DX general, el mas popular | -20 dB SNR |
| **FT4** | 7.5 seg | Contesting rapido | -17.5 dB SNR |
| **JT65** | 60 seg | EME (rebote lunar), HF debil | -25 dB SNR |
| **JT9** | 60 seg | HF, ancho de banda minimo | -27 dB SNR |
| **JT4** | 60 seg | EME en VHF/UHF | -23 dB SNR |
| **Q65** | 15-300 seg | VHF+ con desvanecimiento rapido | Variable |
| **MSK144** | 15 seg | Meteor scatter | Rapido |
| **WSPR** | 120 seg | Balizas de propagacion | -28 dB SNR |
| **FST4** | 15-1800 seg | LF/MF senales extremadamente debiles | Hasta -40+ dB |
| **FST4W** | 15-1800 seg | WSPR para LF/MF | Similar a FST4 |
| **Echo** | Variable | Test de eco lunar | N/A |

**Arquitectura tecnica:**
- GUI en C++ con Qt Framework
- Nucleo de procesamiento de senales en **Fortran 90**
- Comunicacion entre GUI y decodificador via **memoria compartida** y subproceso `jt9`
- FEC: Codigos LDPC (Low Density Parity Check) optimizados especificamente
- Modulacion: 8-GFSK (Gaussian Frequency-Shift Keying)
- Mensajes de 77 bits con compresion de indicativos (28 bits), localizadores y reportes
- Build system: CMake
- Dependencias: Qt5/Qt6, Boost, FFTW3, PortAudio, Hamlib, libusb
- **Licencia: GPLv3**
- Multiplataforma: Windows, macOS, Linux

**Fuente del codigo:** SourceForge (mirror en GitHub actualizado cada 6h)

---

### 1.2 JTDX (Fork orientado a DX)

**Diferencias clave vs WSJT-X:**
- **Decodificacion en bandas saturadas:** Revela senales debiles cubiertas por senales 15+ dB mas fuertes
- **Filtro de pasabanda:** Limita la decodificacion a una banda estrecha alrededor de la frecuencia RX
- **Seleccion inteligente de QSO:** Elige estaciones por distancia, SNR, o si ya fueron trabajadas
- **Control de intentos:** Limita reintentos antes de volver a CQ
- **Ajuste de tiempo interno:** Sin tocar el reloj del sistema

**Donde WSJT-X es mejor:**
- Soporte de contests (intercambios especificos via FT8)
- MSK144 para meteor scatter
- Funcion "Wait" para estaciones con fading

**Rendimiento comparado (Marzo 2025 - OH7GGX):**
- WSJT-X 2.7.0: 1743 decodificaciones
- MSHV 2.76.1: 1741 decodificaciones
- JTDX Improved 2.2.159: 1727 decodificaciones
- **Nota:** JTDX destaca con aurora o polar flutter en 10m

**Precaucion:** JTDX puede generar "decodificaciones fantasma" (interpretar ruido como senales validas)

---

### 1.3 fldigi (Fast Light Digital)

**Que es:** Modem de software que convierte la tarjeta de sonido del PC en un modem bidireccional de datos.

**Modos soportados (lista extensa):**
- **CW:** 5-200 WPM
- **BPSK/QPSK:** 31, 63, 125, 250, 500
- **PSKR:** 125, 250, 500
- **RTTY:** Diversas velocidades y shifts
- **DominoEX:** 4, 5, 8, 11, 16, 22 (con y sin FEC)
- **Hellschreiber:** Feld Hell, Slow Hell, Hell x5/x9, FSKHell
- **MFSK:** 4, 8, 11, 16, 22, 31, 32, 64
- **MT63:** 500, 1000, 2000
- **Olivia:** Diversas combinaciones de tonos/anchos de banda
- **THOR:** 4, 5, 8, 11, 16, 22
- **Throb/ThrobX:** 1, 2, 4
- **FSQ, IFKP**
- **WWV:** Solo recepcion (calibracion)

**Caracteristicas destacadas:**
- Cross-platform: Windows, macOS, Linux, FreeBSD
- RSID: Deteccion automatica de modo y sintonizacion
- Control de rig via Hamlib o RigCAT
- Puertos TCP/UDP para integracion (7322 ARQ, 7342 KISS, 7362 XML-RPC)
- Usado por el U.S. Army y U.S. Air Force MARS
- **Licencia: GPLv2**

---

### 1.4 Ham Radio Deluxe (HRD)

**Tipo:** Suite comercial integrada. **Precio:** $99 USD (licencia perpetua + 1 ano de actualizaciones), renovacion opcional $49.95/ano.

**5 Modulos:**

1. **HRD Rig Control** — Control de transceptor con GUI completa, TCP/IP remoto. Soporta Alinco, Elecraft, FlexRadio, Icom, Kenwood, Ten-Tec, Yaesu.

2. **HRD Logbook** — Logging con DX cluster, callsign lookup, tracking de 200 premios en 16 programas. Integra LoTW, eQSL, HRDLog.net, ClubLog, QRZ. Soporta MS Access y MySQL. Auto-log desde WSJT-X/JTDX.

3. **DM-780 (Digital Master)** — Modos digitales: CW, RTTY, PSK, QPSK, Contestia, DominoEX, Hell, MFSK, MT63, Olivia, Thor, Throb. SuperSweeper: copia hasta 40 QSOs simultaneos.

4. **HRD Satellite Tracking** — Operaciones de satelite con integracion Google Earth.

5. **HRD Rotator Control** — Control de rotores con mapa mundial.

**Limitacion:** Solo Windows. FT8/FT4 requiere WSJT-X/JTDX externo (integrado via puertos).

---

### 1.5 Log4OM

- **Tipo:** Logger gratuito, 64-bit, Windows only
- **Fortalezas:** UI moderna, CAT control integrado, DX cluster, VOACAP propagacion, tracking de premios (DXCC, IOTA, POTA), cloud sync con ClubLog/HRDLog, auto-import desde programas digitales, upload automatico a LoTW

---

### 1.6 CHIRP

- **Tipo:** Programador de radios, gratuito y open-source
- **Plataformas:** Windows, macOS, Linux
- **Radios soportados:** Cientos de modelos — Baofeng, Kenwood, Yaesu, Icom, Wouxun, TYT, AnyTone, Retevis, Quansheng UV-K5, etc.
- **Caracteristicas:** Interfaz unificada independiente del radio, cross-mode export entre radios, integracion con RepeaterBook y RadioReference, multiples formatos de archivo (CSV, ICF, HMK, ITM, etc.)
- **Modelo de soporte:** Depende de ingenieria inversa voluntaria

---

### 1.7 SDR# (SDRSharp)

- **Tipo:** Receptor SDR para Windows
- **Hardware compatible:** RTL-SDR, Airspy, LimeSDR, PlutoSDR
- **Plugin ecosystem extenso:**
  - Satellite tracking (GPredict, Orbitron, WXtrack)
  - Meteor M2 decoder
  - CTCSS/DCS decoders
  - Control remoto por red
  - Time shift
  - Procesamiento de audio digital
  - ListenInfo (base de datos de estaciones de onda corta)
  - IF FFT con filtro, marcadores, analizador
- **Notas:** Compilado con .NET (8/9/10), arquitectura de plugins extensible

---

### 1.8 Winlink (Email por Radio)

- **Que es:** Sistema mundial de mensajeria por radio, opera sin internet usando relay inteligente.
- **Disponibilidad:** 99.99%, precision de mensajes 100%
- **Protocolos:** ARDOP, VARA HF/FM, Pactor, SCS Robust Packet, AX.25, WiFi
- **Clientes:**
  - **Winlink Express** (Windows) — Cliente principal, formularios ICS, mapas
  - **RadioMail** (iOS) — Moderno, 100+ formularios Winlink, Bluetooth/WiFi TNC
  - **Pat** — Cross-platform (Go)
  - **WoAD** — Android
- **Uso critico:** Comunicaciones de emergencia (ICS forms, reportes de hospitales, recursos, evacuaciones)
- **Limitacion FCC:** Sin encriptacion en frecuencias de aficionado

---

### 1.9 JS8Call

- **Que es:** Modo keyboard-to-keyboard basado en la modulacion de FT8 pero con mensajes de texto libre.
- **Diferencias con FT8:**
  - Elimina los slots fijos de 15 segundos
  - Permite conversaciones reales, no solo intercambios de reporte
  - Store-and-forward messaging (tipo "email" por radio)
  - Modo heartbeat automatico
  - Relay de mensajes a traves de otras estaciones
- **Ancho de banda:** ~50 Hz (igual que FT8)
- **Nicho:** Reemplazo moderno de PSK31 para chat, comunicacion off-grid, emergencias
- **Basado en:** Fork de WSJT-X, modulacion JS8 custom

---

### 1.10 GridTracker

- **Tipo:** Freeware, cross-platform (Windows, macOS, Linux)
- **Que hace:** Visualiza trafico en tiempo real de WSJT-X/JTDX en un mapa interactivo
- **Caracteristicas:**
  - Mapa con greyline, posicion lunar, NEXRAD, zonas horarias, PSK Reporter
  - Call Roster con tracking de premios (DXCC, WAS)
  - Alertas de audio/visuales personalizables
  - Lookup de indicativos integrado
  - Integracion con HamClock
  - Spotting en tiempo real de otros usuarios GridTracker/Log4OM

---

### 1.11 N1MM+ (Logger de Contest)

- **Tipo:** Gratuito, Windows only
- **El estandar mundial para contesting**
- **Fortalezas:** Casi todos los contests precargados, formato de archivo listo para enviar, interfaz operable sin raton (todo via teclado), monitoreo de frecuencia en tiempo real, DX cluster integrado, tracking de multiplicadores
- **Limitacion:** Solo Windows, overkill para logging general

---

### 1.12 DXLab Suite

- **Tipo:** Suite gratuita modular, Windows only
- **Modulos:** DXKeeper (logging), Commander (rig control), SpotCollector (alertas), WinWarbler (digital modes), Pathfinder, PropView
- **Fortaleza:** Tracking de premios extremadamente detallado con reportes en tiempo real para cientos de entidades
- **Curva de aprendizaje:** Pronunciada, pero no se queda corto nunca
- **Soporte:** El autor responde directamente en el reflector

---

### 1.13 Otros Software Notable

| Software | Tipo | Plataforma | Notas |
|----------|------|-----------|-------|
| **MSHV** | Modos debiles | Windows | Alternativa a WSJT-X, rendimiento comparable |
| **VarAC** | Chat por HF | Windows | Usa VARA modem, chat tipo WhatsApp por radio |
| **SDR++** | Receptor SDR | Cross-platform | Open source, moderno, rapido |
| **gqrx** | Receptor SDR | Linux/Mac | Basado en GNU Radio + Qt |
| **QLog** | Logger | Cross-platform (Qt) | Open source, moderno, sin ads ni telemetria |
| **not1mm** | Contest logger | Linux/Python | Alternativa Linux a N1MM |
| **Dire Wolf** | TNC/APRS | Cross-platform | Software AX.25 packet modem |
| **MMSSTV** | SSTV | Windows | TV de barrido lento |
| **FreeDATA** | TNC | Cross-platform | TNC libre con GUI para Codec2 |

---

## 2. Plataformas Web

### 2.1 QRZ.com
- **Funcion principal:** Base de datos de indicativos (callsign lookup)
- **Logbook online** con sistema de premios propio
- **API** para lookup de indicativos (XML)
- Las confirmaciones de LoTW cuentan como confirmaciones QRZ (no viceversa)
- Barrera de entrada baja, muy popular

### 2.2 Logbook of The World (LoTW) — ARRL
- **Funcion:** Confirmacion oficial de QSOs de la ARRL
- **Critico para:** DXCC, WAS, VUCC y otros premios ARRL
- Usa certificados digitales para autenticacion (TQSL)
- Upload via ADIF
- Es el "estandar de oro" para confirmaciones

### 2.3 eQSL.cc
- **Funcion:** QSL electronicas con imagenes
- Sistema de premios propio (eDX100, etc.)
- **Limitacion:** Frustrante para operaciones temporales (POTA) por no permitir solapamiento temporal de ubicaciones
- Descarga de imagenes QSL

### 2.4 ClubLog
- **Funcion:** Tracking de DXCC, analisis de log, estadisticas
- Muy facil de usar, upload ADIF directo
- API para determinacion precisa de DXCC/CQ zone
- Usado por HamAlert para sync de entidades no confirmadas
- Upload single-QSO en tiempo real

### 2.5 PSKReporter (pskreporter.info)
- **Funcion:** Reportes de recepcion en tiempo real de estaciones de monitoreo mundiales
- Modos: FT8, FT4, PSK, JT65, etc. (FT8 es la gran mayoria)
- Mapas interactivos con filtrado detallado
- ~23 millones de spots por dia (~250/segundo en promedio)
- Integrado en WSJT-X nativamente
- Excelente para analisis de propagacion y rendimiento de antena

### 2.6 DX Summit / DX Clusters
- **Funcion:** Spots manuales de estaciones DX
- Acceso via Telnet para streaming en tiempo real
- 30,000+ operadores activos
- Multiples clusters interconectados (DX Spider, CC Cluster, AR Cluster)

### 2.7 HamAlert (hamalert.org)
- **Funcion:** Agregador y notificador inteligente
- **Fuentes:** DX cluster, RBN, SOTAwatch, POTA, WWFF, PSK Reporter
- **Filtros:** DXCC, indicativo, referencia SOTA, zona CQ, continente, banda, modo, horario
- **Notificaciones:** Push, Threema, Telnet, URL GET/POST
- Emulacion Telnet/Cluster para integracion con cualquier software
- Sync con Club Log para entidades faltantes

### 2.8 Reverse Beacon Network (RBN)
- Red de receptores automatizados que detectan senales CW y digitales
- Reporta indicativo, frecuencia, SNR, velocidad WPM
- Complementa DX clusters con spots automaticos

### 2.9 SOTA (Summits on the Air)
- Programa de premios basado en puntos para operacion portatil en cumbres
- Base de datos de cumbres con puntos segun elevacion
- SOTAwatch para spots y activaciones programadas
- SOTA mapping project para planificacion
- Leaderboard global
- Requisito: 4+ contactos unicos desde la cumbre

### 2.10 POTA (Parks on the Air)
- **Estado 2025:** 49,000+ cazadores activos, 29,000+ activadores
- Plataforma en pota.app — busqueda de parques, upload ADIF
- Requisito: 10+ QSOs desde dentro del limite del parque
- Soporta "2-fer" (activacion simultanea de 2 parques)
- Premios por bandas, modos, total de contactos

### 2.11 Otras Plataformas
| Plataforma | Funcion |
|-----------|---------|
| **HRDLog.net** | Logbook online, lookup de indicativos |
| **HamQTH** | Lookup de indicativos alternativo |
| **World Radio League** | Gamificacion de radioaficion |
| **QSL Buddy** | Logbook web moderno con logros, diseno de QSL, DX spots |
| **POTAMAP** | Mapa combinado POTA+SOTA con limites de parques |
| **SOTAmat** | App de spotting SOTA/POTA con GPS y bases de datos offline |

---

## 3. Control de Rig / CAT Control

### 3.1 Como Funciona CAT Tecnicamente

**CAT (Computer Aided Transceiver)** es el termino generico para el control remoto de un transceptor desde un PC. Yaesu lo introdujo en los 1980s.

**Conexion fisica:**
- **RS-232** (DB-9): Voltaje +/-12V, mas resistente a RFI. Usado en Elecraft KX, Kenwood, Yaesu antiguos.
- **TTL 3.3V/5V**: Yaesu FT-8xx, Xiegu. Requiere conversion de nivel.
- **USB integrado**: Radios modernos (Yaesu FT-891, Icom IC-705) incluyen USB-serial interno. No necesitan interfaz CAT externa.
- **CI-V (3.5mm jack)**: Icom — bus de un solo cable con hasta 4 radios, usa CSMA/CD.

### 3.2 Protocolos por Fabricante

#### Yaesu CAT
- Comandos de **5 bytes**: 4 bytes de datos + 1 OpCode
- Lineas separadas TxD/RxD
- Desde FTDX-9000: protocolo nuevo similar a Kenwood
- Mix de puertos: TTL, RS-232, USB segun modelo

#### Icom CI-V (Communications Interface v5)
- Bus de **un solo cable** (half-duplex), 3.5mm jack
- Hasta 4 radios en el mismo bus con direccionamiento
- CSMA/CD para deteccion de colisiones
- Paquetes de 4-5 bytes (4 bytes = modo legacy IC-735)
- **Nota:** Xiegu emula el protocolo CI-V del IC-7000/IC-7100

#### Kenwood CAT
- Comandos de **2 caracteres alfabeticos** + parametros opcionales
- Terminador: punto y coma (;)
- Niveles TTL invertidos respecto a Yaesu
- Modelos modernos: RS-232 o USB

### 3.3 Software de Control de Rig

#### Hamlib (El estandar open-source)
- **Que es:** Biblioteca C de control para radios, rotores y amplificadores
- **API:** Abstrae las diferencias entre fabricantes en una interfaz unificada
- **Daemon:** `rigctld` — servidor TCP con comandos ASCII, permite multiples conexiones simultaneas
- **Soporte:** Cientos de radios (Icom, Kenwood, Yaesu, Elecraft, FlexRadio, Xiegu...)
- **Bindings:** C++, Perl, Python, TCL + cualquier lenguaje via socket TCP
- **Estado:** Hamlib 4.7 estable; Hamlib 5 en desarrollo (breaking changes)
- **En desarrollo:** Soporte de protocolo de red Icom (IC-7610, IC-9700, IC-705) con audio y waterfall
- **Licencia:** LGPL

#### Flrig
- **Cross-platform** (Windows, Linux, macOS)
- Interfaz GUI de transceptor completa
- XML-RPC para control remoto via TCP/IP
- Actualizaciones regulares, documentacion solida
- Soporta casi todos los radios con CAT
- Funciona como middleware: WSJT-X -> Flrig -> Radio

#### OmniRig
- **Solo Windows**, por VE3NEA
- v2.1 soporta hasta 4 rigs, polling cada 20ms
- Extensible via archivos INI
- **Desarrollo estancado (~2019)**
- WSJT-X solo soporta v1, Fldigi no lo soporta
- Menos rigs que Hamlib/Flrig

**Recomendacion:** Hamlib (rigctld) o Flrig para nuevos proyectos. OmniRig solo para workflows Windows legacy.

---

## 4. Decodificacion de Modos Digitales — Detalles Tecnicos

### 4.1 Como Funciona la Decodificacion FT8/FT4

**Pipeline de codificacion:**
1. Mensaje de 77 bits (indicativos comprimidos a 28 bits + localizador + reporte)
2. CRC de 12 bits para deteccion de errores
3. Codificacion LDPC (Forward Error Correction)
4. Modulacion 8-GFSK: 8 tonos separados por 6.25 Hz, ancho de banda total ~50 Hz
5. Costas Array 7x7 para sincronizacion
6. Resultado: 79 simbolos transmitidos en 12.64 segundos

**Pipeline de decodificacion:**
1. **FFT del audio:** 372 FFTs con ventanas de 160ms, solapadas 1/4 de simbolo
   - FFT1: 0-160ms, FFT2: 40-200ms, FFT3: 80-240ms...
   - Zero-padding a 320ms para mayor resolucion
2. **Busqueda de sincronizacion Costas:** Busqueda gruesa en el waterfall de 15 segundos
   - Para audio de 200-2500 Hz: 737 x 125 = **92,125 calculos de sincronizacion** por intervalo
3. **Extraccion de tonos:** Para cada candidato, los 8 bins de frecuencia FT8 se evaluan; el tono con mayor magnitud se asigna
4. **Decodificacion LDPC:** Correccion de errores
5. **Verificacion CRC:** Validacion del mensaje
6. **Decodificacion A Priori (AP):** Usa informacion acumulada del QSO para +4 dB de sensibilidad

**Datos clave:**
- Rate de datos: 6.09 bits/segundo
- Tiempo de decodificacion: ~2.36 segundos (del ciclo de 15 seg)
- Sensibilidad: -20 dB SNR en 2500 Hz de ancho de banda

### 4.2 Waterfall Display

- Eje X: Frecuencia (tipicamente 200-2500 Hz de audio)
- Eje Y: Tiempo (desplazamiento de arriba a abajo)
- Color: Intensidad/potencia de la senal
- **No decodifica** — es solo visualizacion. La decodificacion ocurre en el motor DSP

### 4.3 Interfaces de Audio

**Dispositivos populares:**

| Interfaz | Tipo | CAT incluido | Aislamiento | Precio aprox. |
|----------|------|-------------|-------------|---------------|
| **Digirig Mobile** | USB (CM108 audio + CP2102 serial) | Si | No (galvanico) | ~$50 |
| **SignaLink USB** | USB sound card | No (solo audio) | Si (transformadores) | ~$120 |
| **USB integrado del radio** | Interno | Si | Si | Incluido |

**Digirig Mobile:** Hub USB integrado, conector TRRS 3.5mm, configurable para logic levels, RS-232, CI-V. Compatible con Windows, macOS, Linux.

**SignaLink USB:** Estandar historico, mejor aislamiento, pero necesita cable CAT separado para control de rig.

### 4.4 Conexion Tipica Computador-Radio

```
[PC] --- USB --- [Interfaz de audio (DigiRig/SignaLink)] --- Cable de audio --- [Radio]
                                    |
                                    +--- Cable CAT (serial) --- [Radio CAT port]

Para radios modernos con USB integrado:
[PC] --- USB --- [Radio] (audio + CAT por el mismo cable USB)
```

---

## 5. Frameworks de Escritorio Cross-Platform

### 5.1 Avalonia UI (.NET, XAML)

**Que es:** Framework UI cross-platform para .NET, inspirado en WPF, distribuido bajo licencia MIT.

**Pros:**
- **Cross-platform real:** Windows, macOS, Linux, iOS, Android, WebAssembly
- Usa C#/.NET — acceso a `System.IO.Ports` para serial, NAudio para audio
- XAML + MVVM (familiar para desarrolladores WPF)
- Maduro y en produccion (JetBrains, GitHub, Schneider Electric lo usan)
- $3M de patrocinio de Devolutions (Junio 2025)
- **Proyecto real de ham radio existe:** cerkit ClearCast (transcripcion AI de audio de radio)
- Cubierto en podcast "Linux in the Ham Shack" (Ep 564) para uso en radioaficion
- Migrando de Skia a Impeller (Nimpeller) para renderizado
- Microsoft MAUI obtendra soporte Linux/browser via Avalonia backend

**Cons:**
- Ecosistema mas pequeno que WPF
- Rendering custom complejo (waterfall) requiere trabajo con SkiaSharp/Impeller
- Menos documentacion que frameworks maduros

**Evaluacion para ham radio:** EXCELENTE. C# + serial ports + audio + rendering custom. La mejor opcion .NET.

---

### 5.2 .NET MAUI

**Estado actual (2025-2026):**
- Soporta: Android, iOS, Mac Catalyst, Windows (WinUI 3)
- **NO soporta Linux nativamente** (solo via backend Avalonia)
- MAUI 9 soportado hasta Mayo 2026, MAUI 10 llega Noviembre 2025

**Pros:**
- Codebase unico desktop + mobile
- Buen fit para equipos .NET enterprise
- Mejorando en estabilidad (60fps scrolling en 2025)

**Cons:**
- **Sin Linux nativo** — critico para ham radio (muchos usan Linux)
- Problemas de tooling (Rider, VS for Mac)
- Rendimiento inferior a Flutter para UI complejas
- Ciclo de soporte corto (migraciones frecuentes)
- App size overhead por incluir .NET runtime
- CarouselView y otros componentes problematicos

**Evaluacion para ham radio:** INADECUADO. Sin Linux nativo descalifica para una plataforma de radioaficion seria.

---

### 5.3 Electron

**Pros:**
- Ecosistema masivo de librerias web
- Desarrollo rapido con web technologies
- Cross-platform inmediato

**Cons:**
- **Muy pesado:** Apps 100+ MB, cientos de MB de RAM
- Bundlea Chromium completo por app
- Startup lento (1-2 seg)
- Serial port via addon nativo de Node.js (overhead IPC)
- **Inapropiado para audio DSP en tiempo real** — GC pauses de Node.js
- Latencia alta para procesamiento de senales

**Evaluacion para ham radio:** POBRE. Demasiado pesado y lento para DSP/audio en tiempo real.

---

### 5.4 Tauri (Rust + Web Frontend)

**Pros:**
- **Muy ligero:** Apps <10 MB, 30-40 MB RAM idle
- Startup <0.5 seg
- Backend en Rust: sin GC, memoria determinista, ideal para audio DSP
- Serial port via `serialport` crate nativa (sin capas intermedias)
- Caso real: app de dictado AI logro 800ms latencia, 50% menos binario que Electron
- Tauri 2.x soporta iOS y Android
- Seguridad superior (permisos granulares, sandbox WebView)

**Cons:**
- Frontend web (HTML/CSS/JS) limita rendering custom complejo
- Waterfall en tiempo real via canvas/WebGL — posible pero con friction
- Requiere aprender Rust para funcionalidad nativa avanzada
- Ecosistema mas joven que Electron

**Evaluacion para ham radio:** BUENO. Excelente para backend DSP/serial, pero el waterfall en WebView es un compromiso.

---

### 5.5 Qt (C++)

**Pros:**
- **El framework usado por WSJT-X, fldigi, gqrx, SDR++** — probado en ham radio
- Rendimiento nativo C++, ideal para DSP
- Qt SerialPort y Qt SerialBus integrados
- Signals & Slots para arquitectura reactiva
- Maduro (decadas), comunidad enorme
- Qt 6.10 (Octubre 2025)
- Licensing: LGPL/GPL + comercial

**Cons:**
- C++ es dificil de aprender y mantener
- Meta-Object Compiler (moc) anade paso de build
- No es .NET (no encaja con el stack preferido del equipo)
- Curva de aprendizaje pronunciada
- No incluye DSP propio — necesita FFTW, PortAudio, etc.

**Evaluacion para ham radio:** PROBADO Y FUNCIONAL. Es lo que usan los programas existentes. Pero requiere C++.

---

### 5.6 Resumen Comparativo para Ham Radio

| Criterio | Avalonia | MAUI | Electron | Tauri | Qt |
|----------|----------|------|----------|-------|-----|
| **Cross-platform (incl. Linux)** | SI | NO | SI | SI | SI |
| **Rendimiento DSP** | Bueno (C# + P/Invoke) | N/A | Malo | Excelente (Rust) | Excelente (C++) |
| **Serial port** | System.IO.Ports | System.IO.Ports | npm addon | Rust crate | Qt SerialPort |
| **Waterfall rendering** | SkiaSharp/Custom | N/A | Canvas (lento) | WebGL/Canvas | QPainter/OpenGL |
| **Audio** | NAudio/PortAudio interop | N/A | Web Audio (limitado) | CPAL/PortAudio | PortAudio |
| **Lenguaje** | C# | C# | JS/TS | Rust + JS | C++ |
| **Tamano de app** | ~30-50 MB | ~50+ MB | 100+ MB | <10 MB | ~20-40 MB |
| **Ecosistema ham radio** | Emergente | Ninguno | Ninguno | Ninguno | Maduro |
| **Curva aprendizaje (equipo C#)** | Baja | Baja | Media | Alta (Rust) | Alta (C++) |

**Recomendacion:** Para un equipo C#/.NET, **Avalonia UI** es la mejor opcion. Permite usar C# para toda la logica, interoperar con librerias nativas (Hamlib, FFTW, codec2) via P/Invoke, y tiene soporte cross-platform real incluyendo Linux. Para los componentes criticos de rendimiento (decodificacion, FFT), se pueden crear modulos nativos en C o Rust y llamarlos via interop.

---

## 6. Desafios Tecnicos Clave

### 6.1 Audio DSP (Digital Signal Processing)

- **FFT** es el corazon: FFTW3 es la referencia (GPL, con licencia comercial disponible)
- Alternativas permisivas: KissFFT (BSD), PocketFFT
- Para .NET: MathNet.Numerics tiene FFT, pero para rendimiento critico se necesita P/Invoke a FFTW
- Procesamiento en ventanas de 160ms con solapamiento
- Tasas de muestreo tipicas: 12000 Hz para FT8 (audio)

### 6.2 Waterfall/Espectro en Tiempo Real

- Requiere rendering de alta frecuencia (~30-60 FPS)
- Tipicamente implementado con textura scrolling (cada nueva linea de FFT = nueva fila de pixeles)
- En Avalonia: SkiaSharp bitmap manipulation o WriteableBitmap
- Alternativa: OpenGL/Vulkan via interop para maximo rendimiento
- El waterfall muestra 200-2500 Hz tipicamente, con resolucion de ~3 Hz por bin

### 6.3 Comunicacion Serial para Rig Control

- Multiples protocolos (CI-V, Yaesu, Kenwood) con diferencias binarias
- Baud rates tipicos: 4800, 9600, 19200, 38400, 115200
- En .NET: `System.IO.Ports.SerialPort` funciona pero tiene quirks conocidos en Linux
- Mejor opcion para Linux: usar Hamlib/rigctld como middleware (TCP) y no serial directo
- Polling tipico: cada 100-500ms para estado del radio

### 6.4 Sincronizacion de Tiempo (Critico para FT8)

- **Tolerancia:** +/-1 segundo para decodificacion confiable
- **Windows:** W32Time sincroniza 1x/semana por defecto — INSUFICIENTE
- **Soluciones:**
  - Meinberg NTP (1 MB RAM)
  - Chrony (flexible, soporta GPS)
  - BktTimeSync (NTP + GPS)
  - JTSync (usa QSOs de WSJT-X para sync sin internet)
  - GPS con PPS (Pulse Per Second) para Stratum 1 local
- **Oportunidad:** Auto-sync usando senales FT8 recibidas (como KK5JY TweakTime)
- **macOS:** NTP nativo es suficiente
- **Implementacion:** La app deberia monitorear drift y alertar al usuario, o auto-corregir

### 6.5 Integracion con Librerias Existentes

**Estrategia de interop desde C#/.NET:**

| Libreria | Lenguaje | Interop | Notas |
|----------|----------|---------|-------|
| Hamlib | C | P/Invoke o rigctld (TCP) | TCP es mas simple y probado |
| FFTW3 | C | P/Invoke | Critico para rendimiento |
| codec2 | C | P/Invoke | Vocoder de voz |
| liquid-dsp | C | P/Invoke | Modulacion/demodulacion |
| ft8_lib (kgoba) | C | P/Invoke | Decoder FT8 standalone ligero |
| PortAudio | C | P/Invoke | Audio cross-platform |

---

## 7. Oportunidades de IA en Radioaficion

### 7.1 Clasificacion de Senales
- **Estado actual:** CNN identifica 160 tipos de senales de onda corta con ~90% de precision (top-3: ~95%)
- Espectrogramas como entrada a redes neuronales
- **Oportunidad:** Deteccion automatica de modo (CW, SSB, AM, FT8, RTTY, etc.) en el waterfall

### 7.2 Reduccion de Ruido
- CNN para reduccion de ruido CW en HF con mejoras significativas en SNR
- AI identifica el tono especifico y suprime todo lo demas
- **Oportunidad:** Filtro AI adaptativo que mejora con el tiempo, aprendiendo del entorno RF del usuario

### 7.3 Prediccion de Propagacion
- ML reemplaza ray tracing computacionalmente costoso
- Modelos: Random Forest, SVR, KNN, Neural Networks, CNN para prediccion de path loss
- ITU organizo workshop sobre ML en propagacion (Mayo 2025)
- **Oportunidad:** Predicciones minuto-a-minuto basadas en datos solares, geomagneticos e ionosfericos historicos

### 7.4 Asistencia en QSO y Automatizacion
- Reconocimiento de indicativos mas rapido para acelerar logging
- Validacion de log en tiempo real para evitar errores
- Alertas de propagacion por aperturas subitas
- **Oportunidad:** Agente AI que sugiere frecuencia, modo y horario optimo para contactar un DXCC especifico

### 7.5 Decodificacion CW por AI
- **Morse Decoder AI:** Red neuronal entrenada para 10-40 WPM, 200-900 Hz
- Dos configuraciones: Network A (senales estables) y Network B (llave recta, pulsos variables)
- Supera decodificadores clasicos aprendiendo estilos de operacion individuales
- **Oportunidad:** CW decoder integrado que mejora con cada QSO

### 7.6 Voice-to-Text para Modos de Voz
- Google Speech-to-Text funciona parcialmente con audio de radio (errores con ruido)
- cerkit ClearCast: Transcripcion AI en tiempo real (Avalonia + .NET + Gemini API)
- **Oportunidad:** Logging automatico de QSOs de voz, extrayendo indicativo, reporte y nombre

### 7.7 Asistencia en Contests
- AI denoising reduce carga cognitiva durante horas largas de contest
- DeepFT8 y decoders experimentales copian senales 3-10 dB mas debiles
- **Oportunidad:** "Copiloto de contest" que sugiere estaciones por trabajar, multiplicadores faltantes, y estrategia de bandas

### 7.8 Deteccion de Anomalias
- **Oportunidad:** Detectar interferencias inusuales, senales piratas, o comportamiento anomalo en clusters
- Monitoreo de calidad de senal propia (deteccion de splatter, armonicos)

---

## 8. Librerias y Proyectos Open Source Aprovechables

### 8.1 Control de Rig
| Proyecto | Lenguaje | Licencia | Uso |
|----------|----------|---------|-----|
| **Hamlib** | C | LGPL | Control de 300+ radios, rotores, amplificadores |
| **Flrig** | C++ | GPL | GUI de control + middleware XML-RPC |

### 8.2 DSP y Codecs
| Proyecto | Lenguaje | Licencia | Uso |
|----------|----------|---------|-----|
| **FFTW3** | C | GPL (+ comercial) | FFT de alto rendimiento |
| **KissFFT** | C | BSD | FFT ligero, sin dependencias |
| **liquid-dsp** | C | MIT/X11 | FEC, filtros, framing, resampling |
| **codec2** | C | LGPL | Vocoder de voz (450-3200 bps) |
| **ft8_lib** (kgoba) | C | MIT | Encoder/decoder FT8/FT4 standalone |
| **PortAudio** | C | MIT | Audio cross-platform |

### 8.3 SDR
| Proyecto | Lenguaje | Licencia | Uso |
|----------|----------|---------|-----|
| **GNU Radio** | C++/Python | GPL | Toolkit SDR completo |
| **SoapySDR** | C++ | Boost | Abstraccion de hardware SDR |
| **LibSDR** | **C# .NET** | MIT | SDR library para .NET (basado en SDRSharp) |
| **SDR++** | C++ | GPL | Receptor SDR moderno |
| **rtl-sdr** | C | GPL | Driver para dongles RTL-SDR |

### 8.4 WSJT-X y Derivados
| Proyecto | Lenguaje | Licencia | Uso |
|----------|----------|---------|-----|
| **WSJT-X** | C++/Fortran | GPLv3 | Referencia para FT8/FT4/JT65/etc. |
| **JTDX** | C++/Fortran | GPL | Fork optimizado para DX |
| **JS8Call** | C++/Fortran | GPL | Fork para messaging |
| **JTEncode** | Arduino C++ | GPL | Encoder JT65/JT9/WSPR para MCU |

### 8.5 Librerias .NET Relevantes
| Proyecto | Tipo | Uso |
|----------|------|-----|
| **LibSDR** | SDR | FM demod, RTL-SDR, WAV I/O en C# puro |
| **NAudio** | Audio | Captura/reproduccion de audio en Windows |
| **MathNet.Numerics** | DSP | FFT, algebra lineal, estadisticas |
| **System.IO.Ports** | Serial | Comunicacion serial (built-in .NET) |
| **SkiaSharp** | Rendering | 2D graphics cross-platform (waterfall) |

### 8.6 Redes y Protocolos
| Proyecto | Lenguaje | Licencia | Uso |
|----------|----------|---------|-----|
| **Dire Wolf** | C | GPL | AX.25 packet modem/TNC, APRS |
| **Pat** | Go | MIT | Cliente Winlink cross-platform |
| **FreeDATA** | Python | GPL | TNC con GUI para Codec2 |

### 8.7 Repositorios Curados
- **[awesome-hamradio](https://github.com/DD5HT/awesome-hamradio)** — Lista curada de proyectos open source de radioaficion
- **[GitHub Topics: ham-radio](https://github.com/topics/ham-radio)** — 500+ repositorios etiquetados
- **[GitHub Topics: amateur-radio](https://github.com/topics/amateur-radio)** — Repositorios adicionales

---

## 9. Hallazgos Clave y Oportunidades para una Nueva Plataforma

### 9.1 Problemas del Ecosistema Actual

1. **Fragmentacion extrema:** Un operador tipico necesita 5-8 programas separados (WSJT-X + logger + rig control + cluster + mapper + contest logger + CHIRP + etc.)
2. **Solo Windows:** La mayoria del software critico es Windows-only (N1MM, Log4OM, HRD, DXLab, OmniRig)
3. **Sin integracion nativa:** Los programas se comunican via hacks (puertos TCP, archivos compartidos, memoria compartida)
4. **UI anticuada:** La mayoria tiene interfaces de los 2000s
5. **No hay IA:** Ninguna plataforma mainstream integra IA para decodificacion, prediccion o asistencia
6. **No hay mobile real:** Solo apps basicas de logging; la operacion portable depende del PC

### 9.2 La Oportunidad

Una plataforma **unificada, cross-platform, moderna, con IA integrada** que combine:

- Modos digitales (FT8/FT4/JS8 via interop con ft8_lib/codec nativo)
- Logging con tracking de premios
- Control de rig via Hamlib/rigctld
- Waterfall en tiempo real
- DX cluster, PSKReporter, HamAlert integrados
- POTA/SOTA con mapas y GPS
- AI para: clasificacion de senales, reduccion de ruido, prediccion de propagacion, CW decoder, voice-to-text
- Upload automatico a LoTW, eQSL, ClubLog, QRZ

### 9.3 Stack Tecnologico Sugerido

| Capa | Tecnologia | Justificacion |
|------|-----------|---------------|
| **UI** | Avalonia UI (.NET) | Cross-platform, XAML, C#, maduro |
| **Rendering waterfall** | SkiaSharp + WriteableBitmap | Rendimiento suficiente, integrado con Avalonia |
| **Audio I/O** | NAudio (Win) + PortAudio interop (cross) | Captura y reproduccion |
| **DSP/FFT** | P/Invoke a FFTW3 o KissFFT | Rendimiento nativo |
| **Decodificacion FT8** | P/Invoke a ft8_lib (C) | Ligero, MIT license |
| **Rig control** | TCP a Hamlib rigctld | Probado, cientos de radios |
| **AI/ML** | ML.NET + ONNX Runtime | Modelos de clasificacion y prediccion |
| **Base de datos** | SQLite (local) + API REST (cloud) | Portable, sin servidor |
| **Networking** | HttpClient + WebSocket | APIs de QRZ, LoTW, PSKReporter, etc. |

---

## Fuentes

### Software de Escritorio
- [WSJT-X Official](https://wsjt.sourceforge.io/)
- [WSJT-X User Guide](https://wsjt.sourceforge.io/wsjtx-doc/wsjtx-main-2.6.1.html)
- [The FT4 and FT8 Communication Protocols (QEX Paper)](https://wsjt.sourceforge.io/FT4_FT8_QEX.pdf)
- [OH7GGX FT8 Software Comparison 2025](https://oh7ggx.fi/2025/03/09/comparing-ft8-softwares-what-decodes-best/)
- [JTDX Compared - DX.nl](http://www.dx.nl/?p=556)
- [Ham Radio Deluxe](https://www.hamradiodeluxe.com)
- [fldigi - Wikipedia](https://en.wikipedia.org/wiki/Fldigi)
- [CHIRP Home](https://chirpmyradio.com/projects/chirp/wiki/Home)
- [SDR# Plugins List](https://www.rtl-sdr.com/sdrsharp-plugins/)
- [Winlink Global Radio Email](https://winlink.org/)
- [JS8Call Official](https://js8call.com)
- [GridTracker Official](https://gridtracker.org/)
- [N1MM Logger+](https://n1mmwp.hamdocs.com/)

### Plataformas Web
- [PSKReporter](https://pskreporter.info/)
- [HamAlert](https://hamalert.org/about)
- [POTA](https://pota.app/)
- [POTAMAP](https://potamap.us/)
- [SOTAmat](https://sotamat.com/)

### Control de Rig
- [Hamlib GitHub](https://github.com/Hamlib/Hamlib)
- [What is CAT Control - ham-interfaces.com](https://www.ham-interfaces.com/ham-radio-info-and-guides/what-is-cat-control)
- [Hamlib or Flrig or OmniRig - VK Ham Radio](https://www.vkhamradio.com/hamlib-or-flrig-or-omnirig-for-transceiver-cat-control/)
- [Digirig Mobile](https://digirig.net/product/digirig-mobile/)

### Frameworks
- [Avalonia UI](https://avaloniaui.net/)
- [Avalonia Deep Dive for Ham Radio (LHS Ep 564)](https://www.amateurradio.com/lhs-episode-564-avalonia-ui-deep-dive/)
- [cerkit ClearCast](https://cerkit.com/cerkit-clearcast-ai-powered-radio-transcription/)
- [Tauri vs Electron - DoltHub](https://www.dolthub.com/blog/2025-11-13-electron-vs-tauri/)
- [.NET MAUI State 2025](https://appisto.app/blog/state-of-dotnet-maui)
- [Qt Framework](https://www.qt.io/development/qt-framework)

### IA en Ham Radio
- [ML for Signal Classification in Amateur Radio (arXiv)](https://arxiv.org/abs/2402.17771)
- [160 Shortwave Signal Types with Deep Learning](https://panoradio-sdr.de/automatic-identification-of-160-shortwave-rf-signals-with-deep-learning/)
- [AI in Modern Amateur Radio Technologies](https://nexttechworld.com/hobby-radio/ai-ham-radio-technologies/)

### Librerias Open Source
- [LibSDR (.NET)](https://github.com/Roman-Port/LibSDR)
- [liquid-dsp](https://github.com/jgaeddert/liquid-dsp)
- [ft8_lib](https://github.com/kgoba/ft8_lib)
- [awesome-hamradio](https://github.com/DD5HT/awesome-hamradio)
- [WSJT-X Source (GitHub Mirror)](https://github.com/LFGSaito/WSJT-X)
- [Calling WSJT-X Encoding/Decoding from C](https://www.hydrogen18.com/blog/calling-wsjtx-encoding-decoding-from-c.html)

### Sincronizacion de Tiempo
- [FT8 Time Sync Myths Debunked](https://ham-radio-apps.com/debunking-the-top-myths-about-time-synchronization-in-ft8/)
- [Time Sync Software for Ham Radio](https://ve2hew.com/posts/time-sync/)
- [KK5JY TweakTime](http://www.kk5jy.net/tweaktime/)
