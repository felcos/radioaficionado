# Estructura — RadioAficionado

## Solucion: 19 proyectos fuente + 7 proyectos de test

### Capas de la arquitectura

#### Compartido (RadioAficionado.Compartido)
- Sin dependencias externas
- Excepciones: ExcepcionDeValidacion, ExcepcionDeNegocio
- Constantes: ConstantesRadio

#### Dominio (RadioAficionado.Dominio) → Compartido
- ObjetosDeValor: Indicativo, Frecuencia, Localizador, Coordenadas, BandaRadio (24 bandas), ModoOperacion (48 modos + 43 submodos), RegionItu, ClaseLicencia, FiltroQso, ReferenciaPota, ReferenciaSota, EstadoActivacion, TipoActivacion
- Entidades: Qso, UsuarioRadio (hereda IdentityUser), CategoriaForo (enum), HiloForo, RespuestaForo
- Activaciones: Activacion (entidad para POTA/SOTA)
- Aprs: PaqueteAprs, PosicionAprs, MensajeAprs, ObjetoAprs, TipoPaqueteAprs (enum), ConfiguracionAprs
- Compliance: PlanDeBanda, SegmentoBanda, ResultadoCompliance, TipoSegmento, TipoViolacion, PlanDeBandaItu (planes IARU 3 regiones)
- Configuracion: ConfiguracionCompleta, ConfiguracionEstacion, ConfiguracionAudio, ConfiguracionGeneral
- Contests: MotorContest, ReglaContest, ConfiguracionContest, ResultadoContest, TipoContest, TipoIntercambio, MetodoMultiplicador, Intercambio
- Dxcc: EntidadDxcc, CatalogoDxcc (~170 entidades + prefijos alternativos), ConfirmacionQso, TipoConfirmacion, EstadisticasDxcc, ResumenDxcc
- Propagacion: IndicesSolares (record), PrediccionBanda, NivelPropagacion
- Qsl: PlantillaQsl, DatosQsl, FormatoExportacion (enum: PNG, PDF, SVG)
- Satelites: SateliteAmateur, TransponderSatelite, PasoSatelite, PosicionSatelite
- Interfaces (23 interfaces):
  - Repositorios: IRepositorioQso (con paginacion y filtros), IRepositorioActivaciones, IUnidadDeTrabajo
  - Hardware: IControlRig, IControlRotador, IAudioPipeline
  - Modos digitales: IDecodificadorDigital, IRegistroDecodificadores
  - Servicios: IServicioCompliance, IServicioActivaciones, IServicioConfiguracion, IServicioConfirmaciones, IServicioPropagacion, IServicioSincronizacion, IServicioAprs, IServicioSatelites
  - Clientes externos: IDxCluster, IPskReporter, IClienteLoTW, IClienteEQsl, IClienteClubLog
  - Generadores: IGeneradorQsl
  - Records de sincronizacion: ConfiguracionSincronizacion, ResultadoSincronizacion, EstadoSincronizacion

#### Aplicacion (RadioAficionado.Aplicacion) → Dominio, Compartido
- Qsos/RegistrarQso: RegistrarQsoComando, RegistrarQsoHandler, RegistrarQsoValidador
- ConfiguracionServicios (MediatR + FluentValidation)

#### Infraestructura (RadioAficionado.Infraestructura) → Dominio, Aplicacion, Compartido
- Persistencia: ContextoRadioAficionado, QsoConfiguracion, ActivacionConfiguracion, RepositorioQso (con paginacion y filtros), RepositorioActivaciones (EF Core, Include QSOs), UnidadDeTrabajo
- Adif: RegistroAdif, ParserAdif, GeneradorAdif, ConvertidorAdifQso
- Aprs: ClienteAprsIs (cliente TCP a APRS-IS), ParserAprs (parser de paquetes APRS)
- Compliance: ServicioCompliance (verificacion IARU 3 regiones)
- Configuracion: ServicioConfiguracionJson (persistencia JSON)
- Confirmaciones: ClienteLoTW, ClienteEQsl, ClienteClubLog, ServicioConfirmaciones (orquestador multifuente)
- Contests: GeneradorCabrillo (formato de logs para contests)
- DxCluster: ClienteDxCluster (cliente TCP/Telnet)
- Activaciones: ServicioActivaciones (gestion POTA/SOTA)
- Propagacion: ServicioPropagacion (modelo basado en SFI, predicciones HF)
- PskReporter: ClientePskReporter (envio de spots)
- Qsl: GeneradorQslSkia (generador de tarjetas QSL con SkiaSharp)
- Satelites: CatalogoSatelites (~30 satelites amateur), CalculadorOrbital (prediccion de pasos con TLE), Tle (parser Two-Line Elements), ServicioSatelites
- Sincronizacion: ServicioSincronizacion (cliente HTTP bidireccional escritorio ↔ API web)
- ConfiguracionServicios (DI)

#### Infraestructura.Sqlite → Infraestructura
- ConfiguracionSqlite (con MigrationsAssembly configurado)
- FabricaContextoEnDiseño (IDesignTimeDbContextFactory para EF Core CLI)
- Migraciones/Inicial: tablas Activaciones, Qsos (FK, indices)

#### Infraestructura.Postgres → Infraestructura
- ConfiguracionPostgres

#### Nativo.Dsp → Compartido, Dominio
- Interfaces/ITransformadaFourier: contrato para FFT (intercambiable managed ↔ FFTW3)
- TransformadaCooleyTukey: FFT radix-2 DIT managed con twiddle factors y ventana Hann pre-computados
- Fftw3Nativo: P/Invoke a libfftw3-3 (plan R2C, ejecutar, memoria SIMD-aligned)
- TransformadaFftw3: implementacion ITransformadaFourier con FFTW3 nativa, thread-safe
- FabricaTransformadaFourier: factory estatica FFTW3 → fallback Cooley-Tukey, cache de disponibilidad
- ProcesadorEspectro: convierte PCM 16-bit → LineaEspectro (usa FabricaTransformadaFourier)
- ServicioWaterfall: IServicioWaterfall, suscripcion a IAudioPipeline, buffer con solapamiento 50%
- VentanasDsp: funciones de ventana estaticas (Hann, Hamming, Blackman-Harris)
- LineaEspectro: modelo de datos de espectro (magnitudes dB, resolucion Hz, rango)

#### Nativo.ModosDigitales → Compartido, Dominio
- Cw/DecodificadorCw: decodificacion de Morse a texto usando filtro Goertzel
- Cw/FiltroGoertzel: deteccion de tono a frecuencia especifica (alternativa liviana a FFT)
- Cw/TablaMorse: tabla completa de simbolos Morse internacionales
- Cw/ConfiguracionCw: velocidad WPM, frecuencia de tono, umbral de deteccion
- Ft4/DecodificadorFt4: decodificador FT4 reutilizando Ft8Nativo (ventana 7.5s)
- Ft4/ConfiguracionFt4: configuracion especifica FT4
- Rtty/DecodificadorRtty: decodificador RTTY con filtro Goertzel dual mark/space
- Rtty/TablaBaudot: tabla Baudot ITA2 completa
- Rtty/ConfiguracionRtty: frecuencias mark/space, baudios, shift
- Psk31/DecodificadorPsk31: decodificador PSK31 BPSK con deteccion de fase
- Psk31/TablaVaricode: tabla Varicode completa
- Psk31/ConfiguracionPsk31: configuracion PSK31
- Js8/DecodificadorJs8: decodificador JS8 reutilizando Ft8Nativo (Normal/Turbo/Lento/Ultra)
- Js8/ConfiguracionJs8: configuracion con enum de velocidades
- RegistroDecodificadores: IRegistroDecodificadores con 6 decodificadores (CW, FT8, FT4, RTTY, PSK31, JS8)
- ConfiguracionServiciosModosDigitales: DI de todos los decodificadores

#### Nativo.Audio → Compartido, Dominio
- PipelineAudioNAudio: captura/transmision con NAudio WaveInEvent/WaveOutEvent
- Pipeline pub/sub para multiples consumidores simultaneos
- Enumeracion de dispositivos de entrada/salida

#### Nativo.Rig → Compartido, Dominio
- ClienteRigctld: cliente TCP a rigctld (Hamlib), polling 500ms
- MapeadorModos: conversion bidireccional rigctld ↔ ModoOperacion/SubModoOperacion
- ConfiguracionRig: host, puerto, intervalo, potencia maxima

#### Nativo.Rotador → Compartido, Dominio
- ClienteRotctld: cliente TCP a rotctld, polling 1s, AZ/EL
- ConfiguracionRotador: host, puerto, intervalo, umbral de cambio

#### IA (RadioAficionado.IA) → Compartido, Dominio
- AnalizadorPropagacionMlNet: regresion FastTree, ~880 datos sinteticos, prediccion por banda HF, MUF estimado, hora optima
- ClasificadorSenalesMlNet: clasificacion SdcaMaximumEntropy, 800 datos sinteticos, binning de espectro, modos alternativos
- ConfiguracionServiciosIa: AgregarCapaDeIa() extension para DI
- Dominio/IA: PrediccionPropagacionIa (record), ResultadoClasificacion (record)
- Dominio/Interfaces: IAnalizadorPropagacion, IClasificadorSenales

#### Escritorio (RadioAficionado.Escritorio) → todos los proyectos
- Avalonia UI, MVVM con CommunityToolkit.Mvvm, DI
- ViewModels (17):
  - ViewModelBase
  - DispositivoAudioVm (ViewModel para selector de dispositivos de audio en UI)
  - VentanaPrincipalViewModel (navegacion entre paneles)
  - PanelRigViewModel (polling real al rig, CAT serial/rigctld, audio USB, waterfall auto)
  - PanelMensajesViewModel (mensajes digitales decodificados)
  - PanelRegistroQsoViewModel (registro de QSOs via MediatR)
  - PanelLogbookViewModel (DataGrid paginado, filtros, import/export ADIF)
  - PanelWaterfallViewModel (waterfall en vivo, IniciarWaterfall/DetenerWaterfall)
  - PanelDxClusterViewModel (spots en tiempo real, filtros)
  - PanelActivacionesViewModel (POTA/SOTA: crear, iniciar, completar, cancelar, cronometro)
  - PanelContestViewModel (gestion de contests, QSOs en contexto, puntaje en tiempo real)
  - PanelDxccViewModel (estadisticas DXCC, filtros por continente/estado, barras de progreso)
  - PanelPropagacionViewModel (indices solares SFI/K/A, predicciones por banda HF)
  - PanelSatelitesViewModel (prediccion de pasos, tracking en tiempo real)
  - ConfiguracionViewModel (preferencias de estacion, audio, generales)
  - EstadoSincronizacionViewModel (indicador de estado, sincronizacion manual, QSOs pendientes)
- Controles:
  - ControlWaterfall (SkiaSharp, ICustomDrawOperation, SKBitmap con scroll vertical, paleta 256 colores)
- Vistas (8 .axaml):
  - VentanaPrincipal.axaml (layout completo: rig bar, waterfall, mensajes, QSO form, pestanas)
  - VentanaConfiguracion.axaml (ventana de preferencias con pestanas)
  - PanelLogbook.axaml (DataGrid paginado con filtros)
  - PanelDxCluster.axaml (DataGrid de spots en tiempo real)
  - PanelActivaciones.axaml (3 secciones: nueva, en curso, historial)
  - PanelContest.axaml (configuracion, log de QSOs, marcador)
  - PanelDxcc.axaml (estadisticas, DataGrid entidades, panel lateral resumenes)
  - PanelPropagacion.axaml (indicadores SFI/K/A, tabla de bandas HF)

#### Mobile compartido (RadioAficionado.Mobile) → Dominio, Aplicacion, Infraestructura, Infraestructura.Sqlite
- Avalonia UI 12.0.0, MVVM con CommunityToolkit.Mvvm, DI
- App.axaml.cs: DI sin servicios de hardware (sin rig, audio, rotador, waterfall)
- ViewModels (5):
  - ViewModelBase
  - VentanaPrincipalMobileViewModel (navegacion por pestanas)
  - PanelLogbookMobileViewModel (lista QSOs, busqueda, filtros, creacion manual)
  - PanelMapaMobileViewModel (estadisticas por continente, DXCC, bandas, modos)
  - PanelPropagacionMobileViewModel (indices solares, predicciones por banda)
- Vistas AXAML (4):
  - VentanaPrincipalMobile.axaml (TabControl inferior con 4 pestanas)
  - PanelLogbookMobile.axaml (lista scrollable + FAB para nuevo QSO)
  - PanelMapaMobile.axaml (cards de estadisticas + lista por continente)
  - PanelPropagacionMobile.axaml (grid indices solares + lista predicciones)

#### Mobile Android (RadioAficionado.Mobile.Android) → Mobile, Dominio, Aplicacion, Infraestructura, Sqlite, Dsp, ModosDigitales, IA
- net10.0-android, Avalonia.Android 12.0.0
- MainActivity: AvaloniaMainActivity<App>
- SplashActivity: splash con redireccion automatica

#### Mobile iOS (RadioAficionado.Mobile.iOS) → Mobile, Dominio, Aplicacion, Infraestructura, Sqlite, Dsp, ModosDigitales, IA
- net10.0-ios, Avalonia.iOS 12.0.0
- AppDelegate: AvaloniaAppDelegate<App>
- Main.cs: punto de entrada

#### Web (RadioAficionado.Web) → Dominio, Aplicacion, Infraestructura, Infraestructura.Postgres, Compartido
- ASP.NET MVC con Razor Views, Bootstrap 5 local, tema oscuro
- Data: ContextoIdentidadRadioAficionado (IdentityDbContext separado para Identity)
- Controllers MVC (8):
  - HomeController (pagina por defecto ASP.NET)
  - InicioController (homepage con estadisticas generales)
  - LogbookController (logbook publico con paginacion, filtros, detalle, mapa)
  - LogbookPrivadoController ([Authorize], CRUD completo de QSOs, filtros por IndicativoPropio)
  - OperadoresController (directorio paginado, perfil publico, mapa de contactos JSON)
  - CuentaController (registro, login, logout, perfil, editar perfil)
  - EstadisticasController (dashboard con datos agregados + endpoints JSON para graficos)
  - ForoController (listado paginado, detalle, crear hilo, responder — con [Authorize])
- API REST (2 controladores):
  - QsoApiController [api/qsos]: CRUD de QSOs, paginacion, filtros, [Authorize]
  - AdifApiController [api/adif]: importacion/exportacion ADIF via API, [Authorize], limite 5 MB
  - Dtos: QsoDto, FiltroQsoDto, ResultadoSincronizacionDto
  - Mapeadores: MapeadorQsoDto (conversion bidireccional Qso ↔ QsoDto)
- ViewModels (22):
  - InicioViewModel, QsoResumenViewModel
  - LogbookIndexViewModel, QsoDetalleViewModel, MapaContactoViewModel
  - CrearQsoViewModel, EditarQsoViewModel, LogbookPrivadoIndexViewModel
  - OperadoresIndexViewModel, OperadorResumenViewModel, PerfilPublicoViewModel, BandaFavoritaViewModel
  - RegistrarViewModel, IniciarSesionViewModel, PerfilViewModel, EditarPerfilViewModel
  - EstadisticasViewModel
  - ForoIndexViewModel, HiloDetalleViewModel, HiloResumenViewModel, CrearHiloViewModel, ResponderHiloViewModel, RespuestaViewModel
- Views (19 .cshtml):
  - Shared: _Layout.cshtml, Error.cshtml, _ValidationScriptsPartial.cshtml
  - Home: Index.cshtml, Privacy.cshtml
  - Inicio: Index.cshtml
  - Logbook: Index.cshtml, Detalle.cshtml, Mapa.cshtml
  - Cuenta: Registrar.cshtml, IniciarSesion.cshtml, Perfil.cshtml, EditarPerfil.cshtml
  - Foro: Index.cshtml, Detalle.cshtml, CrearHilo.cshtml
  - Estadisticas: Index.cshtml
  - _ViewStart.cshtml, _ViewImports.cshtml
- CSS: sitio.css + site.css (variables --ra-*, tema oscuro azul/gris)
- JavaScript: estadisticas.js (Chart.js), mapa-contactos.js (Leaflet), site.js
- Librerias locales: bootstrap, chartjs, leaflet, jquery, jquery-validation, jquery-validation-unobtrusive
- Identity: ASP.NET Identity con UsuarioRadio, cookie auth, password policy
- Models: ErrorViewModel

#### Servicio (RadioAficionado.Servicio) → todos los proyectos
- ASP.NET Core 10 con SignalR, Kestrel en localhost:5200
- Controllers API (7): OperacionController (MVC), LogbookApiController, DxccApiController, PropagacionApiController, ActivacionesApiController, ContestApiController, SatelitesApiController
- Hubs SignalR (4): HubRig, HubWaterfall, HubDecodificaciones, HubEstado
- Dtos (6): EstadoRigDto, LineaEspectroDto, MensajeDecodificadoDto, SpotDxDto, ConfiguracionConexionDto, RegistroQsoDto
- Servicios (4): ServicioEstadoOperacion, ServicioOperacionDigital, ServidorUdpWsjtx, ClienteDxClusterTelnet
- Protocolo WSJT-X: EscritorMensajeWsjtx, LectorMensajeWsjtx, TiposMensajeWsjtx
- Vistas Razor: 8 vistas + 12 partials + layout
- JavaScript: 11 modulos (operacion, waterfall, logbook, dxcc, dxcluster, propagacion, activaciones, contest, satelites)
- CSS: sitio.css, operacion.css, paneles.css

#### Lanzador (RadioAficionado.Lanzador)
- WinForms + WebView2, lanza Servicio como proceso hijo
- ConfiguracionLanzador: persistencia JSON (posicion, tamano, maximizado, puerto, DevTools)
- Health check polling, F11 pantalla completa

### Tests (1309 tests, todos pasando, 0 fallos)
- Dominio.Tests (308):
  - ObjetosDeValor: IndicativoTests, FrecuenciaTests, LocalizadorTests, CoordenadasTests, BandaRadioTests, ModoOperacionTests
  - Entidades: QsoTests
  - Compliance: PlanDeBandaTests
  - Rig: MapeadorModosTests
  - Activaciones: ReferenciaPotaTests, ReferenciaSotaTests, ActivacionTests
  - Contests: MotorContestTests
  - Dxcc: CatalogoDxccTests, EstadisticasDxccTests
- Infraestructura.Tests (676):
  - Dsp: TransformadaCooleyTukeyTests, ProcesadorEspectroTests, VentanasDspTests, FabricaTransformadaFourierTests, ServicioWaterfallTests
  - Adif: ParserAdifTests, GeneradorAdifTests, ConvertidorAdifQsoTests
  - Aprs: ParserAprsTests
  - Compliance: ServicioComplianceTests
  - Configuracion: ServicioConfiguracionJsonTests
  - Confirmaciones: ClienteLoTWTests, ClienteEQslTests, ClienteClubLogTests, ServicioConfirmacionesTests
  - Contests: GeneradorCabrilloTests
  - DxCluster: ClienteDxClusterTests
  - ModosDigitales: FiltroGoertzelTests, DecodificadorCwTests, DecodificadorRttyTests, DecodificadorPsk31Tests, RegistroDecodificadoresTests
  - Propagacion: ServicioPropagacionTests
  - PskReporter: ClientePskReporterTests
  - Qsl: GeneradorQslSkiaTests
  - Satelites: CatalogoSatelitesTests, CalculadorOrbitalTests
  - Sincronizacion: ServicioSincronizacionTests
- Servicio.Tests (131):
  - Controllers: OperacionControllerTests, LogbookApiControllerTests (con RegistrarQso), ContestApiControllerTests
  - Hubs: HubRigTests, HubWaterfallTests
  - Servicios: ServicioEstadoOperacionTests, ServicioOperacionDigitalTests, ColoreadorIndicativosTests
  - Protocolo: EscritorMensajeWsjtxTests, LectorMensajeWsjtxTests
  - Dtos: LineaEspectroDtoTests
  - Integracion: OperacionIntegracionTests (WebApplicationFactory + InMemory DB, 25 tests)
- Web.Tests (67):
  - Api: QsoApiControllerTests
  - Controllers: InicioControllerTests, LogbookControllerTests, LogbookPrivadoControllerTests, OperadoresControllerTests
- Aplicacion.Tests (29):
  - Qsos: RegistrarQsoHandlerTests, RegistrarQsoValidadorTests
- Escritorio.Tests (12):
  - ViewModels: PanelLogbookViewModelTests
- IA.Tests (86):
  - AnalizadorPropagacionMlNetTests, ClasificadorSenalesMlNetTests, MotorInferenciaOnnxTests, EntrenadorModelosIaTests

### Features

- ✅ Estructura de solucion completa (14+5 proyectos)
- ✅ Objetos de valor del dominio (24 bandas, 48+43 modos ADIF)
- ✅ Modelo de compliance regulatorio (PlanDeBandaItu, IARU 3 regiones)
- ✅ ServicioCompliance con verificacion de frecuencia/modo
- ✅ Interfaces de dominio completas (23 interfaces)
- ✅ Entidad Qso + handler MediatR
- ✅ EF Core con SQLite + PostgreSQL + migracion inicial
- ✅ ClienteRigctld (control de radio via TCP)
- ✅ ClienteRotctld (control de rotador via TCP)
- ✅ PipelineAudioNAudio (captura/transmision)
- ✅ FFT managed Cooley-Tukey + ProcesadorEspectro
- ✅ UI escritorio MVVM (rig bar, waterfall, mensajes, QSO form)
- ✅ WaterfallControl con SkiaSharp
- ✅ ViewModels conectados a DI real (PanelRig polling, PanelRegistroQso con MediatR)
- ✅ ADIF parser/generador completo (RegistroAdif, ParserAdif, GeneradorAdif, ConvertidorAdifQso)
- ✅ Logbook UI (PanelLogbook con DataGrid paginado, filtros, import/export ADIF)
- ✅ DX Cluster (IDxCluster, ClienteDxCluster, PanelDxCluster)
- ✅ IRepositorioQso ampliado con paginacion y filtros (FiltroQso)
- ✅ Motor de Contests (MotorContest, ReglaContest, ConfiguracionContest)
- ✅ GeneradorCabrillo (formato de logs para contests)
- ✅ Panel de Contest UI (PanelContest con marcador en tiempo real)
- ✅ Activaciones POTA/SOTA (Activacion, ReferenciaPota, ReferenciaSota, ServicioActivaciones)
- ✅ Panel de Activaciones UI (PanelActivaciones con cronometro)
- ✅ PSK Reporter (IPskReporter, ClientePskReporter)
- ✅ Configuracion persistente JSON (ConfiguracionCompleta, ServicioConfiguracionJson)
- ✅ Ventana de Configuracion UI (VentanaConfiguracion con pestanas)
- ✅ Tracking DXCC (CatalogoDxcc ~170 entidades, EstadisticasDxcc, ConfirmacionQso)
- ✅ Panel DXCC UI (PanelDxcc con filtros, barras de progreso, indicadores visuales)
- ✅ Confirmaciones externas (ClienteLoTW, ClienteEQsl, ClienteClubLog, ServicioConfirmaciones)
- ✅ Propagacion (ServicioPropagacion basado en SFI, predicciones HF)
- ✅ Panel de Propagacion UI (PanelPropagacion con indices solares y tabla de bandas)
- ✅ Web MVP: homepage con estadisticas + logbook publico paginado con filtros
- ✅ ASP.NET Identity (registro, login, perfil, editar perfil, UsuarioRadio)
- ✅ API REST (QsoApiController + AdifApiController, DTOs, Mapeadores, [Authorize])
- ✅ Sincronizacion bidireccional escritorio ↔ web (IServicioSincronizacion, ServicioSincronizacion, EstadoSincronizacionViewModel)
- ✅ Mapa de contactos con Leaflet (Logbook/Mapa, mapa-contactos.js, marcadores interactivos)
- ✅ Dashboard de estadisticas con Chart.js (EstadisticasController, graficos por banda/modo/continente)
- ✅ Foro de la comunidad (CategoriaForo, HiloForo, RespuestaForo, ForoController, vistas)
- ✅ Decodificador CW (DecodificadorCw, FiltroGoertzel, TablaMorse, ConfiguracionCw)
- ✅ APRS (PaqueteAprs, ClienteAprsIs, ParserAprs, ConfiguracionAprs)
- ✅ Satelites amateur (CatalogoSatelites, CalculadorOrbital, Tle, ServicioSatelites, PanelSatelitesViewModel)
- ✅ Generador de tarjetas QSL (PlantillaQsl, DatosQsl, GeneradorQslSkia con SkiaSharp)
- ✅ Waterfall en vivo (IServicioWaterfall → ServicioWaterfall → PanelWaterfallViewModel)
- ✅ FFTW3 nativa (FabricaTransformadaFourier con fallback Cooley-Tukey)
- ✅ Decodificador FT8 (ft8_lib P/Invoke)
- ✅ Migracion EF Core PostgreSQL para Identity (FabricaContextoIdentidadEnDiseño + MigrationsAssembly)
- ✅ Logbook privado (LogbookPrivadoController [Authorize], CRUD completo)
- ✅ Modos digitales: FT4, RTTY (Goertzel dual), PSK31 (BPSK), JS8 (multi-velocidad) + RegistroDecodificadores
- ✅ IA con ML.NET (AnalizadorPropagacion FastTree + ClasificadorSenales SdcaMaximumEntropy)
- ✅ Perfiles publicos de operadores (OperadoresController: directorio, perfil, mapa JSON)
- ✅ 1309 tests (308 + 676 + 131 + 67 + 29 + 12 + 86), 0 fallos
- ✅ SDR (SoapySDR)
- ✅ Mas modos digitales (SSTV, Olivia, JT65, JT9, WSPR, FT2, Q65)
- ✅ ONNX Runtime (modelos pre-entrenados)
- ✅ Mobile (Avalonia Mobile Android + iOS)
- ✅ UI Web tipo WSJT-X: Kestrel + 4 hubs SignalR + 7 APIs REST + Canvas waterfall
- ✅ Auto-sequencing FT8: maquina de estados, GeneradorAudioFt8, ColoreadorIndicativos
- ✅ Protocolo UDP WSJT-X compatible (JTAlert, GridTracker, N1MM+)
- ✅ Split VFO + PTT DTR/RTS en 3 protocolos CAT + rigctld
- ✅ Lanzador WebView2 con configuracion persistida (posicion, tamano, puerto)
- ✅ Log QSO desde panel operacion (POST /api/logbook/registrar)
- ✅ Contest con datos reales del repositorio (QSOs 48h, multiplicadores DXCC, rate)
- ✅ DXCC: barra progreso, filtro continente, colores semaforo
- ✅ Propagacion: indices solares con colores (SFI/SN/A/K)
- ✅ Tests integracion WebApplicationFactory + InMemory DB (25 tests)
- 🔨 DX Cluster real via telnet al HubEstado
- 📋 Graficos Chart.js para propagacion, mapa DXCC con Leaflet
- 📋 Docker compose (web + PostgreSQL)
- 📋 CI/CD (GitHub Actions)
