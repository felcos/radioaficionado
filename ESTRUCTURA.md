# Estructura — RadioAficionado

## Solucion: 14 proyectos fuente + 5 proyectos de test

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

#### Nativo.Dsp → Compartido
- Interfaces/ITransformadaFourier: contrato para FFT (intercambiable managed ↔ FFTW3)
- TransformadaCooleyTukey: FFT radix-2 DIT managed con twiddle factors y ventana Hann pre-computados
- ProcesadorEspectro: convierte PCM 16-bit → LineaEspectro (waterfall data)
- VentanasDsp: funciones de ventana estaticas (Hann, Hamming, Blackman-Harris)
- LineaEspectro: modelo de datos de espectro (magnitudes dB, resolucion Hz, rango)

#### Nativo.ModosDigitales → Compartido, Dominio
- Cw/DecodificadorCw: decodificacion de Morse a texto usando filtro Goertzel
- Cw/FiltroGoertzel: deteccion de tono a frecuencia especifica (alternativa liviana a FFT)
- Cw/TablaMorse: tabla completa de simbolos Morse internacionales
- Cw/ConfiguracionCw: velocidad WPM, frecuencia de tono, umbral de deteccion

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

#### IA → Compartido, Dominio
- (pendiente: ML.NET + ONNX)

#### Escritorio (RadioAficionado.Escritorio) → todos los proyectos
- Avalonia UI, MVVM con CommunityToolkit.Mvvm, DI
- ViewModels (15):
  - ViewModelBase
  - VentanaPrincipalViewModel (navegacion entre paneles)
  - PanelRigViewModel (polling real al rig con timer)
  - PanelMensajesViewModel (mensajes digitales decodificados)
  - PanelRegistroQsoViewModel (registro de QSOs via MediatR)
  - PanelLogbookViewModel (DataGrid paginado, filtros, import/export ADIF)
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

#### Web (RadioAficionado.Web) → Dominio, Aplicacion, Infraestructura, Infraestructura.Postgres, Compartido
- ASP.NET MVC con Razor Views, Bootstrap 5 local, tema oscuro
- Data: ContextoIdentidadRadioAficionado (IdentityDbContext separado para Identity)
- Controllers MVC (6):
  - HomeController (pagina por defecto ASP.NET)
  - InicioController (homepage con estadisticas generales)
  - LogbookController (logbook publico con paginacion, filtros, detalle, mapa)
  - CuentaController (registro, login, logout, perfil, editar perfil)
  - EstadisticasController (dashboard con datos agregados + endpoints JSON para graficos)
  - ForoController (listado paginado, detalle, crear hilo, responder — con [Authorize])
- API REST (2 controladores):
  - QsoApiController [api/qsos]: CRUD de QSOs, paginacion, filtros, [Authorize]
  - AdifApiController [api/adif]: importacion/exportacion ADIF via API, [Authorize], limite 5 MB
  - Dtos: QsoDto, FiltroQsoDto, ResultadoSincronizacionDto
  - Mapeadores: MapeadorQsoDto (conversion bidireccional Qso ↔ QsoDto)
- ViewModels (17):
  - InicioViewModel, QsoResumenViewModel
  - LogbookIndexViewModel, QsoDetalleViewModel, MapaContactoViewModel
  - RegistrarViewModel, IniciarSesionViewModel, PerfilViewModel, EditarPerfilViewModel
  - EstadisticasViewModel
  - ForoIndexViewModel, HiloDetalleViewModel, HiloResumenViewModel, CrearHiloViewModel, ResponderHiloViewModel, RespuestaViewModel
  - OperadoresIndexViewModel, PerfilPublicoViewModel
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

### Tests (724 tests, todos pasando, 0 fallos)
- Dominio.Tests (308):
  - ObjetosDeValor: IndicativoTests, FrecuenciaTests, LocalizadorTests, CoordenadasTests, BandaRadioTests, ModoOperacionTests
  - Entidades: QsoTests
  - Compliance: PlanDeBandaTests
  - Rig: MapeadorModosTests
  - Activaciones: ReferenciaPotaTests, ReferenciaSotaTests, ActivacionTests
  - Contests: MotorContestTests
  - Dxcc: CatalogoDxccTests, EstadisticasDxccTests
- Infraestructura.Tests (344):
  - Dsp: TransformadaCooleyTukeyTests, ProcesadorEspectroTests, VentanasDspTests
  - Adif: ParserAdifTests, GeneradorAdifTests, ConvertidorAdifQsoTests
  - Aprs: ParserAprsTests
  - Compliance: ServicioComplianceTests
  - Configuracion: ServicioConfiguracionJsonTests
  - Confirmaciones: ClienteLoTWTests, ClienteEQslTests, ClienteClubLogTests, ServicioConfirmacionesTests
  - Contests: GeneradorCabrilloTests
  - DxCluster: ClienteDxClusterTests
  - ModosDigitales: FiltroGoertzelTests, DecodificadorCwTests
  - Propagacion: ServicioPropagacionTests
  - PskReporter: ClientePskReporterTests
  - Qsl: GeneradorQslSkiaTests
  - Satelites: CatalogoSatelitesTests, CalculadorOrbitalTests
  - Sincronizacion: ServicioSincronizacionTests
- Web.Tests (31):
  - Api: QsoApiControllerTests
  - Controllers: InicioControllerTests, LogbookControllerTests
- Aplicacion.Tests (29):
  - Qsos: RegistrarQsoHandlerTests, RegistrarQsoValidadorTests
- Escritorio.Tests (12):
  - ViewModels: PanelLogbookViewModelTests

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
- ✅ 724 tests (308 + 344 + 31 + 29 + 12), 0 fallos
- 🔨 Decodificador FT8 (ft8_lib P/Invoke)
- 🔨 Swap FFT managed → FFTW3 nativa
- 🔨 Migracion EF Core PostgreSQL para Identity
- 🔨 Logbook privado (CRUD de QSOs autenticado)
- 🔨 Waterfall en vivo (ProcesadorEspectro → PipelineAudio → ControlWaterfall via DI)
- 📋 SDR (SoapySDR)
- 📋 Mas modos digitales (FT4, RTTY, PSK31, JS8Call...)
- 📋 IA (ML.NET + ONNX) — analisis de propagacion, identificacion de senales
- 📋 Mobile (MAUI o Avalonia Mobile)
