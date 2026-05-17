# Changelog — RadioAficionado

## [3.5.0] — 2026-05-17 — Control remoto completo del rig (ADR-005 Fases 1-5)

### feat: tunelado remoto — API keys, hubs SignalR, cliente relay (Fase 1)
- DTOs compartidos en RadioAficionado.Compartido: ComandoRemotoRig, RespuestaRemotoRig, EstadoRigRemotoDto, TipoComandoRig, LineaEspectroRemotaDto, MensajeDecodificadoRemotoDto, SenalizacionWebRtc
- Entidad ClaveApi en Dominio con tabla "claves_api" (hash SHA-256 + salt, prefijo, expiracion)
- ServicioApiKeys: generar/validar/desactivar claves con FixedTimeEquals
- ApiKeyAuthenticationHandler: esquema "ApiKey" custom para header X-Api-Key
- RegistroServiciosConectados: singleton ConcurrentDictionary (userId -> connectionId)
- HubTunelServicio: hub para conexion del Servicio local (auth ApiKey), reportar estado/respuestas/waterfall/decodificaciones/senalizacion
- HubRelayRig: hub para el browser (auth cookie), enviar comandos y senalizacion WebRTC
- ClienteRelaySignalR: IHostedService con reconexion exponencial, ejecuta comandos, suscribe waterfall/decodificaciones
- ConversorEstadoRemoto: mapeo EstadoRig -> EstadoRigRemotoDto

### feat: vista web de control remoto + gestion API keys (Fase 2)
- ControlRemotoController: vista con display LCD de frecuencia, S-meter, PTT, controles
- ApiKeysController: CRUD de claves API con ValidateAntiForgeryToken
- Vista ControlRemoto/Index.cshtml: display tipo LCD, modos, bandas, boton PTT con countdown 180s
- Vista ApiKeys/Index.cshtml: tabla de claves, generacion, copia, desactivacion
- controlRemoto.js: IIFE con SignalR /hubs/relay-rig, handlers de estado/respuesta/conexion
- Nav links en _Layout.cshtml para Control Remoto y API Keys (usuarios autenticados)

### feat: relay waterfall y decodificaciones (Fase 3)
- IClienteHubRelay ampliada: RecibirLineaEspectro, RecibirMensajeDecodificado, RecibirSenalizacion
- IClienteHubTunel ampliada: RecibirSenalizacion para WebRTC
- HubTunelServicio: relay throttleado de waterfall a 10fps con Stopwatch + decodificaciones
- ClienteRelaySignalR: suscripcion a IServicioWaterfall (8fps throttle) + IRegistroDecodificadores

### feat: senalizacion WebRTC audio stub (Fase 4)
- AdaptadorWebRtcAudio: stub con logging para SDP offer/answer y candidatos ICE
- HubRelayRig.EnviarSenalizacion: relay de senalizacion browser -> servicio
- SenalizacionWebRtc DTO compartido con TipoSenalizacion enum

### feat: hardening — rate limiting, PTT timeout, metricas (Fase 5)
- RateLimitingMiddleware: 20 req/seg por usuario para rutas /hubs/, respuesta 429 con Retry-After
- ControladorTimeoutPtt: singleton con timer cada 5s, desactiva PTT tras 180s via EjecutarComandoRig
- MetricasConexion: contadores atomicos Interlocked (servicios, browsers, comandos, errores)
- Endpoint /api/metricas (solo desarrollo) para snapshot de metricas

## [3.3.0] — 2026-05-16 — Dashboard solar, Mapa QSOs, Espectro, Herramientas, 39 rigs

### feat: dashboard solar con datos NOAA + N0NBH en tiempo real
- Cliente `IClienteDatosSolares` con 6 endpoints NOAA JSON + XML N0NBH en paralelo
- Cache SemaphoreSlim con double-check (5 min actual, 30 min historico)
- Fusionado en pagina Propagacion: indices solares, viento solar, escalas NOAA, condiciones HF/VHF, alertas, grafico historico SFI/Kp
- JS: `solarDashboard.js` IIFE con Chart.js
- 18 tests unitarios para parsing y cache

### feat: mapa QSOs con great circle arcs
- Leaflet + Leaflet.Geodesic para lineas de great circle
- Conversor Maidenhead, calculo Haversine, filtro por banda, toggle grid overlay
- JS: `mapaQsos.js` IIFE

### feat: tabla espectro radioelectrico (0 Hz - 250 GHz)
- 58+ entradas cubriendo todo el espectro sin huecos
- 8 categorias coloreadas: HAM, Broadcasting, Aeronautico, Militar, Telecom, Cientifico, ISM, Pirata
- Filtros: solo HAM, por categoria, busqueda texto
- JS: `espectroRadio.js` IIFE

### feat: herramientas para radioaficionados
- Conversor potencia dBm/Watts/mW con tabla de ejemplos
- Calculadora distancia entre grids Maidenhead (km, millas, azimut)
- Conversor Maidenhead <-> coordenadas con geolocalizacion
- Plan de bandas HF IARU Region 1 (160m a 2m) coloreado por modo
- Tabla codigos RST (R, S, T)
- Alfabeto fonetico NATO completo (letras + numeros)
- JS: `herramientas.js` IIFE, todo client-side

### feat: 39 modelos de radio con control CAT directo
- Yaesu: 16 modelos (FT-991, FT-891, FT-710, FTDX-10, FTDX-101, etc.)
- Icom: 12 modelos con direcciones CI-V correctas (IC-7300, IC-705, IC-9700, etc.)
- Kenwood: 8 modelos (TS-890, TS-590, TS-990, TH-D74, TH-D75, etc.)
- Elecraft: 5 modelos (K3, KX3, K4, KX2, K2)
- FlexRadio: 3 modelos (Flex-6400, 6600, 6700) — stub para soporte futuro

### feat: sidebar ampliado
- Nuevos enlaces: Mapa QSOs, Espectro, Herramientas
- Total 12 secciones en sidebar de operacion

## [3.2.0] — 2026-05-16 — Graficos, ADIF drag-drop, Docker, CI/CD

### feat: mapa mundial DXCC con Leaflet
- Mapa Leaflet 1.9.4 (local) con tema oscuro (CARTO dark_all) debajo de la tabla DXCC
- Marcadores circulares: verde para confirmados, amarillo para trabajados
- Coordenadas deterministas por hash del numero de entidad dentro de limites continentales
- Popups con nombre, prefijo y estado; se actualiza con filtros
- Archivos: `Views/Paneles/_Dxcc.cshtml`, `wwwroot/js/dxcc.js`, `Views/Operacion/Dxcc.cshtml`

### feat: grafico de propagacion con Chart.js
- Chart.js 4.4.8 (local) grafico de barras con condiciones por banda
- Barras verdes (dia) y azules (noche) con mapeo Buena=3, Regular=2, Pobre=1, Cerrada=0
- Tooltips y eje Y con etiquetas legibles, tema oscuro
- Archivos: `Views/Paneles/_Propagacion.cshtml`, `wwwroot/js/propagacion.js`, `Views/Operacion/Propagacion.cshtml`

### feat: importar ADIF por drag-and-drop
- Zona de drop con overlay visual sobre el panel de logbook
- Detecta dragenter/dragleave/drop con contador para evitar parpadeo
- Filtra extensiones .adi/.adif antes de enviar al endpoint existente
- Archivos: `Views/Paneles/_Logbook.cshtml`, `wwwroot/js/logbook.js`, `wwwroot/css/paneles.css`

### feat: Docker compose (web + PostgreSQL)
- Dockerfile multi-stage: SDK para compilar, ASP.NET runtime para ejecutar
- docker-compose.yml con servicio web (puerto 5200) y PostgreSQL 17
- .dockerignore para excluir bin/obj/logs/tests
- Archivos: `Dockerfile`, `docker-compose.yml`, `.dockerignore`

### feat: CI/CD con GitHub Actions
- Workflow `ci.yml`: compilar, testear con PostgreSQL, publicar resultados TRX
- Job Docker: compilar imagen en push a main/develop con cache GHA
- Archivos: `.github/workflows/ci.yml`

### test: 17 tests nuevos para API controllers y DX Cluster parser
- 8 tests para ClienteDxClusterTelnet (parseo de spots Telnet)
- 4 tests para PropagacionApiController (indices reales y fallback)
- 5 tests para DxccApiController (entidades, filtros, QSOs reales)
- Archivos: `tests/RadioAficionado.Servicio.Tests/`

## [3.1.0] — 2026-05-15 — Paneles conectados, Log QSO, Tests integracion, Lanzador persistido

### fix: corregir clases CSS en DXCC JS
- dxcc.js usaba `dxcc-confirmado/trabajado/necesitado` pero el CSS define `dxcc-banda-confirmado/trabajado/necesitado`
- Archivos: `wwwroot/js/dxcc.js`

### feat: barra de progreso y filtro por continente en DXCC
- Barra de progreso actualizada dinamicamente (meta 340 entidades)
- Filtro por continente (AF, AS, EU, NA, OC, SA) con botones toggle
- Archivos: `wwwroot/js/dxcc.js`

### feat: colores semaforo en indices solares de propagacion
- SFI, SN, A-Index, K-Index ahora muestran colores verde/amarillo/rojo segun umbrales
- Nueva funcion `aplicarClaseIndice` con umbrales configurables
- Archivos: `wwwroot/js/propagacion.js`

### feat: endpoint POST /api/logbook/registrar para Log QSO
- Nuevo DTO `RegistroQsoDto` con campos: Indicativo, FrecuenciaHz, Modo, RstEnviado, RstRecibido, Grid, Nombre, Comentario
- Validacion de campos obligatorios, parseo de modo, deteccion de duplicados
- Archivos: `Controllers/LogbookApiController.cs`, `Dtos/RegistroQsoDto.cs`

### feat: boton Log QSO conectado en panel de operacion
- Variables `frecuenciaActualHz` y `modoActual` actualizadas desde SignalR
- Funcion `registrarQso()` con POST fetch y notificacion temporal
- Funcion `mostrarNotificacion()` para feedback visual
- Archivos: `wwwroot/js/operacion.js`

### feat: ContestApiController con datos reales del repositorio
- Inyeccion de IRepositorioQso, QSOs de ultimas 48h, agrupacion por banda
- Multiplicadores como entidades DXCC unicas, puntos por continente, rate por hora
- Fallback a datos ejemplo si no hay QSOs
- Archivos: `Controllers/ContestApiController.cs`

### feat: configuracion persistida del lanzador WebView2
- Nueva clase ConfiguracionLanzador con JSON persistence (posicion, tamano, maximizado, puerto, DevTools)
- VentanaPrincipal carga/guarda configuracion en OnFormClosing
- Program.cs usa puerto configurable
- Archivos: `ConfiguracionLanzador.cs`, `VentanaPrincipal.cs`, `Program.cs` (Lanzador)

### test: 22 tests nuevos (1309 total, 0 fallos)
- 10 tests unitarios para RegistrarQso (LogbookApiControllerTests)
- 13 tests unitarios para ContestApiController (ContestApiControllerTests)
- 22 tests de integracion con WebApplicationFactory + InMemory DB (vistas MVC + APIs REST + POST)
- Archivos: `Controllers/LogbookApiControllerTests.cs`, `Controllers/ContestApiControllerTests.cs`, `Integracion/OperacionIntegracionTests.cs`

## [3.0.0] — 2026-05-14 — Migración a UI Web tipo WSJT-X (Fases A-G)

### feat: RadioAficionado.Servicio — Host Kestrel + SignalR (Fase A)
- Nuevo proyecto web .NET 10 con DI idéntica al escritorio Avalonia
- 4 hubs SignalR: HubRig, HubWaterfall, HubDecodificaciones, HubEstado
- ServicioEstadoOperacion: singleton con lógica extraída de PanelRigViewModel
- Dtos: EstadoRigDto, ConfiguracionConexionDto, LineaEspectroDto, MensajeDecodificadoDto, SpotDxDto
- Health check en /health, Kestrel en localhost:5200

### feat: Panel de operación tipo WSJT-X con Canvas 2D (Fase B)
- Vista Razor completa: _BarraRig, _PanelDecodificaciones, _PanelTx, _PanelQso, _Configuracion
- waterfall.js: Canvas 2D con paleta 256 colores, click-to-tune, cursores TX/RX
- operacion.js: conexión a 4 hubs SignalR, bindings UI, atajos F1-F6/Esc
- operacion.css: CSS Grid oscuro, frecuencia LED, S-meter, tema radio

### feat: Auto-sequencing FT8 (Fase C)
- IServicioOperacionDigital: interfaz + FaseQsoFt8 enum (7 estados)
- ServicioOperacionDigital: máquina de estados CQ→respuesta→reporte→RRR→73
- GeneradorAudioFt8: 79 tonos FT8 a PCM 16-bit (6.25Hz spacing, 12.64s)
- ColoreadorIndicativos: colores por estado DXCC (rojo CQ, verde nuevo, etc.)

### feat: Protocolo UDP WSJT-X compatible (Fase D)
- EscritorMensajeWsjtx + LectorMensajeWsjtx: serialización binaria QDataStream big-endian
- ServidorUdpWsjtx: BackgroundService en puerto 2237 (heartbeat, status, decode)
- Compatible con JTAlert, GridTracker, N1MM+

### feat: Split VFO + PTT DTR/RTS (Fase E)
- IControlRig: +ActivarSplitAsync, +CambiarFrecuenciaVfoBAsync, +CambiarPttDtrAsync/RtsAsync
- EstadoRig: +SplitActivo, +FrecuenciaVfoB
- IProtocoloCat: +ComandoActivarSplit/LeerSplit/ParsearSplit/CambiarFrecuenciaVfoB
- Implementado en Yaesu (FT0/FT1/FB), Icom CI-V (0x0F), Kenwood (FT/FB)
- ClienteRigctld: split via comando S, VFO B vía cambio temporal
- ClienteCatSerial: PTT DTR/RTS por control directo de SerialPort

### feat: Lanzador WebView2 (Fase F)
- RadioAficionado.Lanzador: WinForms + Microsoft.Web.WebView2
- Lanza Servicio como proceso hijo, espera health check, abre WebView2
- F11 pantalla completa, sin barra de navegación

### feat: Paneles secundarios web (Fase G)
- 7 partial views: Logbook, DX Cluster, DXCC, Propagación, POTA/SOTA, Contest, Satélites
- LogbookApiController: API REST paginada con búsqueda por indicativo
- logbook.js + dxcluster.js: interactividad AJAX y SignalR

### test: 127+ tests (59 Servicio + 68 Infraestructura Rig)
- Tests: hubs, dtos, estado operación, auto-sequencing, protocolo UDP, split VFO
- Build limpio: 0 errores

## [2.4.0] — 2026-04-12 — Audio USB + Waterfall en vivo + Conexión CAT robusta

### feat: Integración audio del radio al ciclo de conexión (Escritorio)
- DispositivoAudioVm: ViewModel para selector de dispositivos de audio en UI
- PanelRigViewModel: ahora recibe IAudioPipeline + IServicioWaterfall por DI
- RefrescarDispositivosAudioAsync(): enumera dispositivos NAudio, restaura selección persistida
- IniciarCapturaAudioAsync(): inicia captura + arranca waterfall FFT 2048 automáticamente
- DetenerCapturaAudioAsync(): detiene waterfall + audio al desconectar
- GuardarConfiguracion(): ahora persiste DispositivoAudioEntrada y TasaDeMuestreoHz
- ConfiguracionConexionRig: campos DispositivoAudioEntrada y TasaDeMuestreoHz
- Archivos: ViewModels/DispositivoAudioVm.cs, ViewModels/PanelRigViewModel.cs, ConfiguracionConexionRig.cs

### feat: Waterfall conectado al flujo de audio real (Escritorio)
- VentanaPrincipal.axaml.cs: suscribe waterfall cabecera a LineaEspectroRecibida
- PanelWaterfall.axaml.cs: suscribe waterfall pestaña con DataContextChanged
- Conversión LineaEspectroEventArgs → LineaEspectro + Dispatcher.UIThread.Post
- Flujo: Radio USB → NAudio → ServicioWaterfall FFT → ControlWaterfall.AgregarLinea()
- Archivos: Vistas/VentanaPrincipal.axaml.cs, Vistas/PanelWaterfall.axaml.cs

### feat: Selector de audio en panel de configuración (Escritorio)
- Sección AUDIO: ComboBox dispositivos entrada, tasa muestreo (12k/24k/48k), botón refrescar
- Indicador LED verde/rojo con AUDIO ON/OFF
- DispositivoAudioEntradaVm para binding correcto en Avalonia ComboBox
- Archivos: Vistas/VentanaPrincipal.axaml

### test: 822 tests (658 Infraestructura + 67 Web + 85 IA + 12 Escritorio)
- 1 fallo preexistente (ML.NET flaky SfiAlto_BandaAlta_ProbabilidadAlta)
- Build limpio: 0 errores

## [2.3.0] — 2026-04-11 — WSPR/FT2/Q65 + SDR→Waterfall + Modelos ONNX + Auditoría completa

### feat: Decodificadores WSPR, FT2, Q65 (Nativo.ModosDigitales)
- DecodificadorWspr: 4-FSK ultra-lento (110.6s, 1.4648 Hz spacing), señales débiles
- DecodificadorFt2: 4-GFSK experimental (6s ventana, 6.25 Hz spacing)
- DecodificadorQ65: 65-FSK con submodos A/B/C/D/E (15s-300s), señales hasta -28 dB SNR
- SubModoQ65 enum con extensiones para duración de período
- ModoOperacion actualizado: FT2 añadido, submodos Q65A-E
- Total: 13 decodificadores digitales
- Tests: DecodificadorWsprTests (10), DecodificadorFt2Tests (10), DecodificadorQ65Tests (14+)

### feat: Integración SDR → Waterfall (Nativo.Sdr)
- IConvertidorIqAAudio: convierte muestras IQ a audio mono (magnitud + ganancia + normalización)
- IServicioWaterfallSdr: waterfall alimentado directamente desde SDR
- FuenteDeDatosWaterfall enum: Ninguna, Audio, Sdr
- ConvertidorIqAAudio: sqrt(I²+Q²), ganancia digital, normalización [-1,1]
- ServicioWaterfallSdr: suscripción a SDR, buffer 50% overlap, frecuencias centradas en Fc
- PanelSdrViewModel: inicia/detiene waterfall SDR, comando cambiar fuente Audio↔SDR
- App.axaml.cs: AgregarCapaDeSdr() + PanelSdrViewModel registrados
- Tests: ConvertidorIqAAudioTests (11), ServicioWaterfallSdrTests (10)

### feat: Entrenador de modelos ONNX (RadioAficionado.IA)
- IEntrenadorModelosIa: entrenar y exportar clasificador + analizador como ONNX
- EntrenadorModelosIa: entrenamiento real con 12000+ muestras (clasificador) y 3500+ (analizador)
- GeneradorDatosSinteticos: espectros realistas CW/SSB/FM/FT8/AM/Ruido con ruido gaussiano multi-SNR
- MetricasClasificacion, MetricasRegresion: records con accuracy, R², RMSE, etc.
- Tests: EntrenadorModelosIaTests (10), GeneradorDatosSinteticosTests (8)

### refactor: Auditoría completa de interfaces y DI
- 30 interfaces documentadas con XML completo (qué es, cómo se usa, implementaciones, registro DI, configuración, dependencias)
- 4 decodificadores corregidos en DI (CW, WSPR, FT2, Q65 no estaban registrados)
- IRepositorioActivaciones: añadido EliminarAsync faltante (CRUD completo)
- RepositorioActivaciones: implementado EliminarAsync

### fix: Auditoría — problemas críticos e importantes corregidos
- Error handler roto: acción Error() añadida a InicioController
- DI escritorio: AgregarModosDigitales() + AgregarCapaDeIa() en App.axaml.cs
- DI web: AgregarModosDigitales() + AgregarCapaDeIa() en Program.cs + referencias .csproj
- Connection string: fallback hardcodeado → throw InvalidOperationException
- EstadoSincronizacionViewModel: registrado en DI
- CORS: política PermitirEscritorio configurada para API
- appsettings.json: cadena de conexión añadida

### feat: Vistas de escritorio faltantes
- PanelSdr.axaml: panel SDR con controles de dispositivo, frecuencia, ganancia, fuente waterfall
- PanelWaterfall.axaml: panel con ControlWaterfall, botones iniciar/detener, selector FFT
- VentanaPrincipal.axaml: pestañas SDR y Waterfall añadidas
- VentanaPrincipalViewModel: PanelSdr + EstadoSincronizacion inyectados

### test: 1120 tests (308 Dominio + 618 Infraestructura + 67 Web + 29 Aplicacion + 12 Escritorio + 86 IA)
- +65 tests nuevos: Compartido (15), Audio (8), Rotador (13), Rig (8), Mobile (13), ForoController (8)
- 0 fallos, 0 omitidos

## [2.2.0] — 2026-04-11 — SDR + JT65/JT9/Olivia/SSTV + ONNX Runtime + Vistas web

### feat: SDR con SoapySDR (Nativo.Sdr — proyecto nuevo)
- IReceptorSdr: interfaz de dominio con eventos MuestrasRecibidas, control de frecuencia/ganancia/ancho de banda
- DispositivoSdr, ConfiguracionSdr: records de dominio en Dominio/Sdr
- SoapySdrNativo: P/Invoke completo a SoapySDR (enumerate, make, stream, read)
- ReceptorSoapySdr: implementacion con hilo dedicado de lectura IQ, thread-safe, IDisposable
- PanelSdrViewModel: ViewModel escritorio con Conectar/Desconectar/BuscarDispositivos
- ConfiguracionServiciosSdr: AgregarCapaDeSdr() extension DI
- Tests: ReceptorSoapySdrTests (10), ConfiguracionSdrTests (5), DispositivoSdrTests (5)

### feat: Modos digitales — JT65, JT9, Olivia, SSTV (Nativo.ModosDigitales)
- DecodificadorJt65: 65-FSK, simbolos de 0.372s, analisis Goertzel multi-tono (65 frecuencias)
- DecodificadorJt9: 9-FSK, simbolos de 0.576s, tonos espaciados 1.7361 Hz
- DecodificadorOlivia: MFSK con Walsh-Hadamard, modos 4/8/16/32/64/128/256 tonos × 125-2000 Hz BW
- DecodificadorSstv: Scottie1/2, Martin1/2, Robot36, deteccion VIS, mapeo frecuencia→luminancia
- RegistroDecodificadores actualizado: 10 decodificadores totales
- ConfiguracionServiciosModosDigitales actualizado con DI
- Tests: DecodificadorJt65Tests (10), DecodificadorJt9Tests (10), DecodificadorOliviaTests (10), DecodificadorSstvTests (10)

### feat: ONNX Runtime (RadioAficionado.IA)
- IMotorInferenciaOnnx: interfaz de dominio para inferencia con modelos ONNX
- ResultadoInferencia, ConfiguracionOnnx: records de dominio
- MotorInferenciaOnnx: cache de InferenceSession, thread-safe con SemaphoreSlim por modelo
- ClasificadorSenalesOnnx: clasificador con fallback a ML.NET si no hay modelo ONNX
- ExportadorModeloOnnx: exportacion de modelos ML.NET a formato ONNX
- Paquetes: ML.NET 5.0.0, OnnxConverter 0.23.0, OnnxTransformer 5.0.0
- Tests: MotorInferenciaOnnxTests (12), ClasificadorSenalesOnnxTests (8), ExportadorModeloOnnxTests (5)

### feat: Vistas web completadas
- Views/Operadores/Perfil.cshtml: breadcrumb, tarjetas estadisticas, mapa Leaflet
- Views/Operadores/_OperadorCard.cshtml: partial view para directorio
- wwwroot/js/mapa-operador.js: carga datos via fetch, marcadores Leaflet con popups
- Tests: OperadoresControllerIndexTests (5)

### fix: Mobile Android/iOS
- MainActivity.cs: corregido AvaloniaMainActivity (no genérica en Avalonia 12.x)
- AppDelegate.cs: removido WithInterFont() inexistente

### test: 978 tests (308 Dominio + 502 Infraestructura + 59 Web + 29 Aplicacion + 12 Escritorio + 68 IA)
- +95 tests nuevos respecto a v2.1.0
- 0 fallos, 0 omitidos
- Build limpio: 0 errores

## [2.1.0] — 2026-04-11 — Proyecto Mobile (Android + iOS) con Avalonia

### feat: Proyecto mobile compartido (RadioAficionado.Mobile)
- App.axaml.cs: Application Avalonia con DI (sin servicios de hardware: rig, audio, rotador)
- VentanaPrincipalMobileViewModel: navegacion por pestanas (Logbook, Mapa, Propagacion, Config)
- PanelLogbookMobileViewModel: lista de QSOs con busqueda, filtros, paginacion, creacion manual de QSO
- PanelMapaMobileViewModel: estadisticas de contactos por continente (QSOs, DXCC, bandas, modos)
- PanelPropagacionMobileViewModel: indices solares (SFI, Kp, Ap, SSN) y predicciones por banda
- 4 vistas AXAML: VentanaPrincipalMobile (TabControl inferior), PanelLogbookMobile (FAB para nuevo QSO), PanelMapaMobile, PanelPropagacionMobile
- Tema oscuro consistente con escritorio (#1a1a2e, #16213e, #e94560)

### feat: Proyecto Android (RadioAficionado.Mobile.Android)
- MainActivity: AvaloniaMainActivity<App> con soporte de orientacion y UI mode
- SplashActivity: splash screen simple con redireccion automatica
- TargetFramework: net10.0-android, ApplicationId: com.radioaficionado.app

### feat: Proyecto iOS (RadioAficionado.Mobile.iOS)
- AppDelegate: AvaloniaAppDelegate<App>
- Main.cs: punto de entrada iOS
- TargetFramework: net10.0-ios

### infra: Solucion actualizada
- RadioAficionado.slnx: 3 proyectos mobile añadidos en carpeta /mobile/

## [2.0.0] — 2026-04-11 — Fase 5: Waterfall + FFTW3 + FT8 + Modos digitales + IA + Logbook privado + Perfiles publicos

### feat: Waterfall en vivo (Nativo.Dsp + Escritorio)
- IServicioWaterfall: interfaz con eventos LineaEspectroGenerada, IniciarAsync/DetenerAsync
- ServicioWaterfall: suscripcion a IAudioPipeline, buffer con solapamiento 50%, ProcesadorEspectro
- PanelWaterfallViewModel: comandos IniciarWaterfall/DetenerWaterfall, binding a UI
- Registrado en DI (App.axaml.cs): IServicioWaterfall → ServicioWaterfall
- Tests: ServicioWaterfallTests (10 tests)
- Archivos: Dominio/Interfaces/IServicioWaterfall.cs, Nativo.Dsp/ServicioWaterfall.cs, Escritorio/ViewModels/PanelWaterfallViewModel.cs

### feat: FFTW3 nativa con fallback (Nativo.Dsp)
- Fftw3Nativo: P/Invoke a libfftw3-3 (PlanearRealAComplejo, Ejecutar, DestruirPlan, AsignarMemoria, LiberarMemoria)
- TransformadaFftw3: implementacion ITransformadaFourier con buffers SIMD-aligned, plan R2C FFTW_ESTIMATE, thread-safe
- FabricaTransformadaFourier: factory estatica FFTW3 → fallback Cooley-Tukey, cache de disponibilidad
- ProcesadorEspectro actualizado para usar FabricaTransformadaFourier en vez de Cooley-Tukey directo
- Tests: FabricaTransformadaFourierTests (10 tests)
- Archivos: Nativo.Dsp/Fftw3Nativo.cs, TransformadaFftw3.cs, FabricaTransformadaFourier.cs

### feat: Migracion PostgreSQL Identity (Web + Infraestructura.Postgres)
- FabricaContextoIdentidadEnDiseño: IDesignTimeDbContextFactory en Web/Data para EF Core CLI
- Program.cs: UseNpgsql con MigrationsAssembly("RadioAficionado.Infraestructura.Postgres")
- Infraestructura.Postgres.csproj: paquetes Identity.EntityFrameworkCore y EF Design añadidos
- Archivos: Web/Data/FabricaContextoIdentidadEnDiseño.cs

### feat: Logbook privado (Web)
- LogbookPrivadoController: [Authorize], CRUD completo, filtros por IndicativoPropio del usuario autenticado
- ViewModels: CrearQsoViewModel, EditarQsoViewModel, LogbookPrivadoIndexViewModel
- Vistas Razor: Index (tabla paginada con filtros), Crear, Editar, Detalle
- Link "Mi Logbook" en _Layout.cshtml para usuarios autenticados
- Tests: LogbookPrivadoControllerTests (15 tests)
- Archivos: Controllers/LogbookPrivadoController.cs, ViewModels/Crear+Editar+LogbookPrivadoIndexViewModel.cs, Views/LogbookPrivado/*.cshtml

### feat: Modos digitales — FT4, RTTY, PSK31, JS8 (Nativo.ModosDigitales)
- DecodificadorFt4: reutiliza Ft8Nativo con ventana de 7.5s
- DecodificadorRtty: filtro Goertzel dual mark/space, tabla Baudot ITA2
- DecodificadorPsk31: deteccion de fase BPSK, tabla Varicode
- DecodificadorJs8: reutiliza Ft8Nativo con multiples velocidades (Normal/Turbo/Lento/Ultra)
- RegistroDecodificadores: IRegistroDecodificadores con 5 decodificadores registrados (CW, FT8, FT4, RTTY, PSK31, JS8)
- ConfiguracionServiciosModosDigitales actualizado con DI de todos los decodificadores
- Tests: DecodificadorRttyTests (14), DecodificadorPsk31Tests (17), RegistroDecodificadoresTests (10)
- Archivos: Nativo.ModosDigitales/Ft4/*.cs, Rtty/*.cs, Psk31/*.cs, Js8/*.cs, RegistroDecodificadores.cs

### feat: IA con ML.NET (RadioAficionado.IA — proyecto nuevo)
- IAnalizadorPropagacion: interfaz para prediccion de propagacion por banda HF
- IClasificadorSenales: interfaz para clasificacion de tipo de senal (CW, SSB, FM, FT8, ruido)
- AnalizadorPropagacionMlNet: regresion FastTree con ~880 datos sinteticos, prediccion por banda, MUF estimado, hora optima
- ClasificadorSenalesMlNet: clasificacion SdcaMaximumEntropy con 800 datos sinteticos, binning de espectro, modos alternativos
- PrediccionPropagacionIa, ResultadoClasificacion: records en Dominio/IA
- ConfiguracionServiciosIa: AgregarCapaDeIa() extension para DI
- Tests: AnalizadorPropagacionMlNetTests (15), ClasificadorSenalesMlNetTests (14) — nuevo proyecto RadioAficionado.IA.Tests
- Archivos: IA/*.cs, Dominio/IA/*.cs, Dominio/Interfaces/IAnalizadorPropagacion.cs, IClasificadorSenales.cs

### feat: Perfiles publicos de operadores (Web)
- OperadoresController: Index (directorio paginado con busqueda), Perfil (detalle con estadisticas, bandas favoritas, ultimos QSOs), MapaDatosOperador (JSON para mapa)
- ViewModels: OperadoresIndexViewModel, OperadorResumenViewModel, PerfilPublicoViewModel, BandaFavoritaViewModel
- Tests: OperadoresControllerTests (5 tests)
- Archivos: Controllers/OperadoresController.cs, ViewModels/Operadores*.cs

### test: 883 tests (308 Dominio + 437 Infraestructura + 54 Web + 29 Aplicacion + 12 Escritorio + 43 IA)
- +159 tests nuevos respecto a v1.3.0
- 0 fallos, 0 omitidos
- Build limpio: 0 errores

## [1.3.0] — 2026-03-23 — CW Decoder + APRS + Satelites + QSL Generator

### feat: Decodificador CW (Nativo.ModosDigitales/Cw)
- DecodificadorCw: decodificacion de Morse a texto usando filtro Goertzel
- FiltroGoertzel: deteccion de tono a frecuencia especifica (alternativa liviana a FFT)
- TablaMorse: tabla completa de simbolos Morse internacionales
- ConfiguracionCw: velocidad WPM, frecuencia de tono, umbral de deteccion
- Archivos: Nativo.ModosDigitales/Cw/DecodificadorCw.cs, FiltroGoertzel.cs, TablaMorse.cs, ConfiguracionCw.cs

### feat: APRS — Automatic Packet Reporting System (Dominio + Infraestructura)
- Dominio/Aprs: PaqueteAprs, PosicionAprs, MensajeAprs, ObjetoAprs, TipoPaqueteAprs (enum), ConfiguracionAprs
- IServicioAprs: interfaz en Dominio/Interfaces
- ClienteAprsIs: cliente TCP al servidor APRS-IS (Infraestructura/Aprs)
- ParserAprs: parser de paquetes APRS en formato AX.25/TCP
- Archivos: Dominio/Aprs/*.cs, Dominio/Interfaces/IServicioAprs.cs, Infraestructura/Aprs/ClienteAprsIs.cs, Infraestructura/Aprs/ParserAprs.cs

### feat: Satelites amateur (Dominio + Infraestructura + Escritorio)
- Dominio/Satelites: SateliteAmateur, TransponderSatelite, PasoSatelite, PosicionSatelite
- IServicioSatelites: interfaz en Dominio/Interfaces
- Infraestructura/Satelites: CatalogoSatelites (~30 satelites amateur), CalculadorOrbital (prediccion de pasos), Tle (parser Two-Line Elements), ServicioSatelites
- PanelSatelitesViewModel: prediccion de pasos, tracking en tiempo real
- Archivos: Dominio/Satelites/*.cs, Infraestructura/Satelites/*.cs, Escritorio/ViewModels/PanelSatelitesViewModel.cs

### feat: Generador de tarjetas QSL (Dominio + Infraestructura)
- Dominio/Qsl: PlantillaQsl, DatosQsl, FormatoExportacion (enum: PNG, PDF, SVG)
- IGeneradorQsl: interfaz en Dominio/Interfaces
- GeneradorQslSkia: generador de tarjetas QSL con SkiaSharp (Infraestructura/Qsl)
- Archivos: Dominio/Qsl/*.cs, Dominio/Interfaces/IGeneradorQsl.cs, Infraestructura/Qsl/GeneradorQslSkia.cs

### test: 724 tests (308 Dominio + 344 Infraestructura + 31 Web + 29 Aplicacion + 12 Escritorio)
- Nuevos: FiltroGoertzelTests, DecodificadorCwTests, ParserAprsTests, CatalogoSatelitesTests, CalculadorOrbitalTests, GeneradorQslSkiaTests
- 0 fallos, 0 omitidos

## [1.2.0] — 2026-03-23 — Identity + API REST + Sincronizacion + Mapa + Foro + Estadisticas + Tests completos

### feat: API REST para sincronizacion escritorio-web (Web/Api)
- QsoApiController: CRUD completo de QSOs via REST, [Authorize], paginacion, filtros
- AdifApiController: importacion/exportacion de archivos ADIF via API, limite 5 MB
- DTOs: QsoDto, FiltroQsoDto, ResultadoSincronizacionDto
- MapeadorQsoDto: conversion bidireccional entre Qso y QsoDto
- Archivos: Api/QsoApiController.cs, Api/AdifApiController.cs, Api/Dtos/*.cs, Api/Mapeadores/MapeadorQsoDto.cs

### feat: Servicio de sincronizacion bidireccional (Dominio + Infraestructura + Escritorio)
- IServicioSincronizacion: interfaz con SincronizarAsync, ObtenerEstadoAsync, ConfigurarAsync
- ConfiguracionSincronizacion, ResultadoSincronizacion, EstadoSincronizacion (records en Dominio)
- ServicioSincronizacion: cliente HTTP que sincroniza QSOs locales con la API web
- EstadoSincronizacionViewModel: indicador de estado en escritorio, sincronizacion manual
- Archivos: Dominio/Interfaces/IServicioSincronizacion.cs, Infraestructura/Sincronizacion/ServicioSincronizacion.cs, Escritorio/ViewModels/EstadoSincronizacionViewModel.cs

### feat: Mapa de contactos con Leaflet (Web)
- Logbook/Mapa.cshtml: mapa interactivo con marcadores de contactos
- MapaContactoViewModel: datos de QSOs con coordenadas para el mapa
- mapa-contactos.js: logica Leaflet con marcadores, popups, clustering
- Leaflet local en wwwroot/lib/leaflet (sin CDN)
- Archivos: Views/Logbook/Mapa.cshtml, ViewModels/MapaContactoViewModel.cs, wwwroot/js/mapa-contactos.js

### feat: Dashboard de estadisticas con Chart.js (Web)
- EstadisticasController: datos agregados + endpoints JSON para graficos
- EstadisticasViewModel: estadisticas por banda, modo, continente, hora, mes
- estadisticas.js: graficos Chart.js (barras, donut, lineas)
- Chart.js local en wwwroot/lib/chartjs (sin CDN)
- Archivos: Controllers/EstadisticasController.cs, ViewModels/EstadisticasViewModel.cs, wwwroot/js/estadisticas.js

### feat: Foro de la comunidad (Web + Dominio)
- Entidades de dominio: CategoriaForo (enum), HiloForo, RespuestaForo
- ForoController: listado paginado, detalle, crear hilo, responder (con [Authorize])
- ViewModels: ForoIndexViewModel, HiloDetalleViewModel, HiloResumenViewModel, CrearHiloViewModel, ResponderHiloViewModel, RespuestaViewModel
- Vistas: Foro/Index, Foro/Detalle, Foro/CrearHilo
- Archivos: Dominio/Entidades/CategoriaForo.cs+HiloForo.cs+RespuestaForo.cs, Controllers/ForoController.cs, ViewModels/Foro*.cs, Views/Foro/*.cshtml

### test: 608 tests (308 Dominio + 228 Infraestructura + 31 Web + 29 Aplicacion + 12 Escritorio)
- Nuevos: ServicioSincronizacionTests, QsoApiControllerTests, InicioControllerTests, LogbookControllerTests, PanelLogbookViewModelTests
- 0 fallos, 0 omitidos

## [1.1.0] — 2026-03-23 — ASP.NET Identity: Registro, Login, Perfil de usuario

### feat: ASP.NET Identity con UsuarioRadio (Web + Dominio)
- UsuarioRadio (hereda IdentityUser): Indicativo, Localizador, Nombre, FechaRegistro, Biografia, RegionItu
- ContextoIdentidadRadioAficionado: DbContext separado para Identity (no modifica el compartido con escritorio)
- CuentaController: Registrar, IniciarSesion, CerrarSesion, Perfil, EditarPerfil
- ViewModels: RegistrarViewModel, IniciarSesionViewModel, PerfilViewModel, EditarPerfilViewModel
- Vistas Razor: Registrar, IniciarSesion, Perfil, EditarPerfil (tema oscuro consistente)
- _Layout.cshtml: navbar con Login/Register o indicativo del usuario logueado
- Program.cs: Identity configurado con password policy, lockout, cookie auth
- Paquetes: Microsoft.Extensions.Identity.Stores (Dominio), Microsoft.AspNetCore.Identity.EntityFrameworkCore (Web)
- Tabla "usuarios" con indicativo unico, CHECK constraint para RegionItu (1-3)
- Traduccion de errores de Identity al espanol
- Archivos: Dominio/Entidades/UsuarioRadio.cs, Web/Data/ContextoIdentidadRadioAficionado.cs, Web/Controllers/CuentaController.cs, Web/ViewModels/Registrar+IniciarSesion+Perfil+EditarPerfilViewModel.cs, Web/Views/Cuenta/*.cshtml

## [1.0.0] — 2026-03-23 — Confirmaciones externas + Propagacion + Contest UI + Propagacion UI + Configuracion UI

### feat: Clientes LoTW/eQSL/ClubLog + ServicioConfirmaciones (Dominio + Infraestructura)
- IClienteLoTW, IClienteEQsl, IClienteClubLog: interfaces en Dominio/Interfaces
- IServicioConfirmaciones: orquestador de confirmaciones multifuente
- ClienteLoTW: cliente HTTP para subir/descargar QSOs de Logbook of The World
- ClienteEQsl: cliente HTTP para subir/descargar QSOs de eQSL.cc
- ClienteClubLog: cliente HTTP para subir QSOs a Club Log
- ServicioConfirmaciones: coordina las 3 fuentes de confirmacion
- Archivos: Dominio/Interfaces/ICliente*.cs, IServicioConfirmaciones.cs, Infraestructura/Confirmaciones/*.cs

### feat: ServicioPropagacion (Dominio + Infraestructura)
- IndicesSolares (record): SFI, indices K/A, manchas solares
- PrediccionBanda: prediccion de apertura por banda HF
- NivelPropagacion: enum con niveles de propagacion
- IServicioPropagacion en Dominio/Interfaces
- ServicioPropagacion en Infraestructura/Propagacion (modelo basado en SFI)
- Archivos: Dominio/Propagacion/*.cs, Infraestructura/Propagacion/ServicioPropagacion.cs

### feat: Panel de Contest UI (Escritorio)
- PanelContestViewModel: gestion de contests activos, QSOs en contexto, puntaje en tiempo real
- PanelContest.axaml: vista con tema oscuro, configuracion de contest, log de QSOs, marcador
- Archivos: Escritorio/ViewModels/PanelContestViewModel.cs, Escritorio/Vistas/PanelContest.axaml(.cs)

### feat: Panel de Propagacion UI (Escritorio)
- PanelPropagacionViewModel: indices solares, predicciones por banda, actualizacion periodica
- PanelPropagacion.axaml: vista con indicadores SFI/K/A, tabla de bandas HF
- Archivos: Escritorio/ViewModels/PanelPropagacionViewModel.cs, Escritorio/Vistas/PanelPropagacion.axaml(.cs)

### feat: Ventana de Configuracion (Escritorio)
- ConfiguracionViewModel: gestion de preferencias de estacion, audio y generales
- VentanaConfiguracion.axaml: ventana con pestanas para cada seccion de configuracion
- Archivos: Escritorio/ViewModels/ConfiguracionViewModel.cs, Escritorio/Vistas/VentanaConfiguracion.axaml(.cs)

## [0.9.1] — 2026-03-23 — Migracion inicial EF Core SQLite

### feat: Migracion inicial de base de datos (Infraestructura.Sqlite)
- Migracion "Inicial" con tablas Activaciones y Qsos, FK, indices
- FabricaContextoEnDiseño (IDesignTimeDbContextFactory) para EF Core CLI
- MigrationsAssembly configurado en ConfiguracionSqlite y FabricaContextoEnDiseño
- Paquete Microsoft.EntityFrameworkCore.Design añadido al proyecto Sqlite
- Archivos: FabricaContextoEnDiseño.cs, Migraciones/20260323100132_Inicial.cs (+Designer +Snapshot)

## [0.9.0] — 2026-03-23 — Web MVP: Homepage + Logbook Publico

### feat: Controladores MVC (Web/Controllers)
- InicioController: pagina de inicio con estadisticas generales (total QSOs, indicativos unicos, bandas, modos, ultimos 5 contactos)
- LogbookController: logbook publico con paginacion (25/pagina) y filtros (indicativo, modo, banda, rango de fechas) + vista de detalle de QSO
- Archivos: Controllers/InicioController.cs, Controllers/LogbookController.cs

### feat: ViewModels (Web/ViewModels)
- InicioViewModel, QsoResumenViewModel: estadisticas y listado resumido
- LogbookIndexViewModel: paginacion, filtros, listas de modos/bandas disponibles
- QsoDetalleViewModel: detalle completo con duracion calculada
- Archivos: ViewModels/InicioViewModel.cs, LogbookIndexViewModel.cs, QsoDetalleViewModel.cs

### feat: Vistas Razor con tema oscuro (Web/Views)
- _Layout.cshtml: layout responsive con navbar, footer, Bootstrap 5 local, tema oscuro
- Inicio/Index.cshtml: hero section, 4 tarjetas de estadisticas, tabla de ultimos QSOs, seccion de features
- Logbook/Index.cshtml: tabla paginada con filtros, paginacion completa con ellipsis
- Logbook/Detalle.cshtml: detalle de QSO con breadcrumb, datos del contacto, senales, potencia, notas, metadatos
- Archivos: Views/Shared/_Layout.cshtml, Views/Inicio/Index.cshtml, Views/Logbook/Index.cshtml, Views/Logbook/Detalle.cshtml

### feat: CSS tema oscuro (Web/wwwroot/css)
- sitio.css: variables CSS personalizadas (--ra-*), tema oscuro consistente con colores azul/gris
- Archivo: wwwroot/css/sitio.css

## [0.8.1] — 2026-03-23 — Panel DXCC UI

### feat: Panel de tracking DXCC (Escritorio)
- PanelDxccViewModel: carga datos desde IRepositorioQso, calcula estadisticas con EstadisticasDxcc
- EntidadDxccVm con indicadores visuales: verde=confirmada, amarillo=trabajada, gris=no trabajada
- Filtros reactivos: continente (Todos/AF/AS/EU/NA/OC/SA), estado (Todas/Trabajadas/NoTrabajadas/Confirmadas)
- ResumenContinenteVm y ResumenBandaVm con barras de progreso
- PanelDxcc.axaml: barra de estadisticas, filtros, DataGrid de entidades, panel lateral con resumenes
- Tema oscuro consistente, compiled bindings, integrado como pestana "DXCC" en VentanaPrincipal
- Archivos: Escritorio/ViewModels/PanelDxccViewModel.cs, Escritorio/Vistas/PanelDxcc.axaml(.cs)

## [0.8.0] — 2026-03-23 — Tracking DXCC y Premios

### feat: Catalogo DXCC (Dominio/Dxcc)
- EntidadDxcc (record): Numero, Nombre, Prefijo, Continente, ZonaCq, ZonaItu, Latitud, Longitud, Eliminada
- CatalogoDxcc: catalogo estatico con ~170 entidades DXCC (todas las mas activas + eliminadas historicas)
- Busqueda por prefijo (exacta + progresiva), por indicativo, listado completo, solo activas
- Prefijos alternativos registrados para USA, Rusia, Japon, Alemania, China, Canada, Brasil, Argentina, Espana, Francia, Italia, Inglaterra, Australia, Mexico, India, Corea
- ConfirmacionQso + TipoConfirmacion (LoTW, QSL fisica, eQSL, directa, bureau)
- Archivos: Dominio/Dxcc/EntidadDxcc.cs, CatalogoDxcc.cs, ConfirmacionQso.cs

### feat: Estadisticas DXCC (Dominio/Dxcc)
- EstadisticasDxcc: calculo de entidades trabajadas, confirmadas, por banda, por modo, faltantes
- ResumenDxcc (record): TotalTrabajadas, TotalConfirmadas, PorBanda, PorModo, PorContinente
- Archivo: Dominio/Dxcc/EstadisticasDxcc.cs

### test: Tests DXCC (27 tests)
- CatalogoDxccTests: 15 tests (busqueda por prefijo principal/alternativo, por indicativo, case-insensitive, eliminadas)
- EstadisticasDxccTests: 12 tests (trabajadas, confirmadas, por banda, por modo, faltantes, resumen)
- Archivos: Dominio.Tests/Dxcc/CatalogoDxccTests.cs, EstadisticasDxccTests.cs

## [0.7.1] — 2026-03-23 — Panel de Activaciones POTA/SOTA UI

### feat: Panel de Activaciones (Escritorio)
- PanelActivacionesViewModel con comandos Crear/Iniciar/Completar/Cancelar
- ActivacionVm para representacion visual del historial
- Cronometro en tiempo real para activacion en curso
- PanelActivaciones.axaml con tema oscuro, 3 secciones: nueva, en curso, historial
- Archivos: Escritorio/ViewModels/PanelActivacionesViewModel.cs, Escritorio/Vistas/PanelActivaciones.axaml(.cs)

### feat: RepositorioActivaciones (Infraestructura)
- Implementacion EF Core de IRepositorioActivaciones con Include de QSOs
- ObtenerTodasAsync, ObtenerActivaAsync, ObtenerPorTipoAsync, ObtenerPorIdAsync
- Archivo: Infraestructura/Persistencia/RepositorioActivaciones.cs

## [0.7.0] — 2026-03-23 — Motor de Contests + POTA/SOTA + PSK Reporter + Configuracion

### feat: Motor de Contests (Dominio/Contests)
- MotorContest: evaluacion completa de QSOs en contexto de contest
- ReglaContest, ConfiguracionContest, ResultadoContest
- TipoContest, TipoIntercambio, MetodoMultiplicador, Intercambio
- Archivos: Dominio/Contests/*.cs

### feat: GeneradorCabrillo (Infraestructura/Contests)
- Generador de logs en formato Cabrillo para envio a contests
- Archivos: Infraestructura/Contests/GeneradorCabrillo.cs

### feat: Activaciones POTA/SOTA (Dominio + Infraestructura)
- Entidad Activacion (Dominio/Activaciones/Activacion.cs)
- Objetos de valor: ReferenciaPota, ReferenciaSota, EstadoActivacion, TipoActivacion
- Interfaces: IRepositorioActivaciones, IServicioActivaciones
- Implementacion: ServicioActivaciones (Infraestructura/Activaciones/)
- Archivos: Dominio/Activaciones/*, ObjetosDeValor/ReferenciaPota.cs, ReferenciaSota.cs, EstadoActivacion.cs, TipoActivacion.cs

### feat: PSK Reporter (Dominio + Infraestructura)
- Interfaz IPskReporter en Dominio/Interfaces
- ClientePskReporter en Infraestructura/PskReporter/
- Archivos: Dominio/Interfaces/IPskReporter.cs, Infraestructura/PskReporter/ClientePskReporter.cs

### feat: Configuracion persistente (Dominio + Infraestructura)
- Modelos: ConfiguracionCompleta, ConfiguracionEstacion, ConfiguracionAudio, ConfiguracionGeneral
- IServicioConfiguracion en Dominio/Interfaces
- ServicioConfiguracionJson en Infraestructura/Configuracion/
- Archivos: Dominio/Configuracion/*.cs, Infraestructura/Configuracion/ServicioConfiguracionJson.cs

## [0.6.0] — 2026-03-23 — Logbook + DX Cluster + Compliance + ADIF

### feat: ADIF parser/generador (Infraestructura/Adif)
- RegistroAdif: modelo de registro ADIF con campos tipados
- ParserAdif: parser completo de archivos ADIF (.adi)
- GeneradorAdif: generador de archivos ADIF con header
- ConvertidorAdifQso: conversion bidireccional entre RegistroAdif y Qso
- Archivos: Infraestructura/Adif/*.cs

### feat: Logbook UI (Escritorio)
- PanelLogbookViewModel: DataGrid paginado, filtros, import/export ADIF
- PanelLogbook.axaml: vista con DataGrid, controles de paginacion y filtros
- FiltroQso: objeto de valor para filtrado de QSOs
- IRepositorioQso ampliado con paginacion (ContarAsync, ObtenerPaginadoAsync)
- Archivos: Escritorio/ViewModels/PanelLogbookViewModel.cs, Escritorio/Vistas/PanelLogbook.axaml*

### feat: DX Cluster (Dominio + Infraestructura + Escritorio)
- IDxCluster: interfaz en Dominio/Interfaces
- ClienteDxCluster: cliente TCP/Telnet en Infraestructura/DxCluster
- PanelDxClusterViewModel: ViewModel con filtros, spots en tiempo real
- PanelDxCluster.axaml: vista con DataGrid de spots
- Archivos: Dominio/Interfaces/IDxCluster.cs, Infraestructura/DxCluster/ClienteDxCluster.cs, Escritorio/ViewModels+Vistas

### feat: ServicioCompliance regulatorio (Infraestructura/Compliance)
- PlanDeBandaItu: planes de banda IARU para 3 regiones
- ServicioCompliance: verificacion de frecuencia/modo contra plan de banda
- Archivos: Dominio/Compliance/PlanDeBandaItu.cs, Infraestructura/Compliance/ServicioCompliance.cs

### feat: ViewModels conectados a DI real (Escritorio)
- PanelRigViewModel: polling real al rig con timer
- PanelRegistroQsoViewModel: registro de QSOs via MediatR
- VentanaPrincipalViewModel: navegacion entre paneles
- Archivos: Escritorio/ViewModels/*.cs

## [0.5.0] — 2026-03-23 — ControlWaterfall con SkiaSharp

### feat: ControlWaterfall (Escritorio/Controles)
- Control Avalonia custom con renderizado SkiaSharp via ICustomDrawOperation
- SKBitmap interno con scroll vertical (unsafe Buffer.MemoryCopy)
- Paleta de 256 colores precalculada: negro → azul → verde → amarillo → rojo
- Metodo AgregarLinea(LineaEspectro) thread-safe (~25 FPS)
- Propiedades: AnchoFft, DbMinimo, DbMaximo
- Archivos: Controles/ControlWaterfall.cs, Vistas/VentanaPrincipal.axaml

## [0.4.0] — 2026-03-22 — Tests capa nativa + UI escritorio

### test: 201 tests (161 Dominio + 40 Infraestructura)
- TransformadaCooleyTukeyTests: 10 tests (seno puro, silencio, tamanos, dispose)
- ProcesadorEspectroTests: 10 tests (PCM, bloques, solapamiento, validacion)
- VentanasDspTests: 10 tests (Hann, Hamming, Blackman-Harris extremos/centro/simetria)
- MapeadorModosTests: 30 tests (29 modos rigctld, S-meter dBm→S, VFO)

### feat: UI escritorio completa
- ViewModels MVVM: VentanaPrincipal, PanelRig, PanelMensajes, PanelRegistroQso
- Layout: barra rig (frecuencia, modo, S-meter, PTT), waterfall placeholder, mensajes digitales, formulario QSO
- Tema oscuro personalizado, compiled bindings con x:DataType

## [0.3.0] — 2026-03-22 — Capa nativa: Rig, DSP, Audio, Rotador

### feat: ClienteRigctld (Nativo.Rig)
- Cliente TCP a rigctld con polling 500ms, SemaphoreSlim thread-safe
- MapeadorModos: conversion bidireccional rigctld ↔ ModoOperacion/SubModoOperacion
- S-meter: conversion dBm → unidades S
- ConfiguracionRig: host, puerto, intervalo, potencia maxima

### feat: TransformadaCooleyTukey (Nativo.Dsp)
- FFT managed radix-2 DIT con twiddle factors pre-computados
- ITransformadaFourier: interfaz para swap futuro a FFTW3 nativa
- ProcesadorEspectro: PCM 16-bit → LineaEspectro (magnitudes dB, resolucion)
- VentanasDsp: Hann, Hamming, Blackman-Harris

### feat: PipelineAudioNAudio (Nativo.Audio)
- Captura/transmision con NAudio WaveInEvent/WaveOutEvent
- Pipeline pub/sub para multiples consumidores simultaneos
- Enumeracion de dispositivos de entrada/salida

### feat: ClienteRotctld (Nativo.Rotador)
- Cliente TCP a rotctld con polling 1s
- Soporte AZ/EL, deteccion de cambio por umbral

## [0.1.0] — 2026-03-22 — Fase 0: Cimientos

### feat: Estructura de solucion completa (14 proyectos + 5 test)
- Clean Architecture compartida entre escritorio y web
- Proyectos: Compartido, Dominio, Aplicacion, Infraestructura (+Sqlite, +Postgres), Nativo.Dsp, Nativo.ModosDigitales, Nativo.Audio, Nativo.Rig, Nativo.Rotador, IA, Escritorio, Web

### feat: Objetos de valor del dominio
- Indicativo, Frecuencia, Localizador, Coordenadas
- BandaRadio (24 bandas 2200m→1.2cm), ModoOperacion (48 modos ADIF + 43 submodos)
- RegionItu, NivelLicencia, LicenciaOperador

### feat: Modelo de compliance regulatorio
- PlanDeBanda, SegmentoBanda, ResultadoCompliance, IServicioCompliance

### feat: Interfaces del dominio
- IControlRig, IControlRotador, IAudioPipeline, IDecodificadorDigital, IRegistroDecodificadores
- IRepositorioQso, IUnidadDeTrabajo

### feat: Entidad Qso + MediatR + EF Core
- Qso.Crear/Completar + RegistrarQsoComando/Handler/Validador
- ContextoRadioAficionado + RepositorioQso + UnidadDeTrabajo
- Proveedores SQLite (escritorio) + PostgreSQL (web)

### test: 89 tests unitarios del dominio
