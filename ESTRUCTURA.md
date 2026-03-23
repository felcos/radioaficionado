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
- Compliance: PlanDeBanda, SegmentoBanda, ResultadoCompliance, TipoSegmento, TipoViolacion, PlanDeBandaItu (planes IARU 3 regiones)
- Contests: MotorContest, ReglaContest, ConfiguracionContest, ResultadoContest, TipoContest, TipoIntercambio, MetodoMultiplicador, Intercambio
- Dxcc: EntidadDxcc, CatalogoDxcc (~170 entidades + prefijos alternativos), ConfirmacionQso, TipoConfirmacion, EstadisticasDxcc, ResumenDxcc
- Configuracion: ConfiguracionCompleta, ConfiguracionEstacion, ConfiguracionAudio, ConfiguracionGeneral
- Propagacion: IndicesSolares (record), PrediccionBanda, NivelPropagacion
- Interfaces (20 interfaces):
  - Repositorios: IRepositorioQso (con paginacion y filtros), IRepositorioActivaciones, IUnidadDeTrabajo
  - Hardware: IControlRig, IControlRotador, IAudioPipeline
  - Modos digitales: IDecodificadorDigital, IRegistroDecodificadores
  - Servicios: IServicioCompliance, IServicioActivaciones, IServicioConfiguracion, IServicioConfirmaciones, IServicioPropagacion, IServicioSincronizacion
  - Clientes externos: IDxCluster, IPskReporter, IClienteLoTW, IClienteEQsl, IClienteClubLog
  - Records de sincronizacion: ConfiguracionSincronizacion, ResultadoSincronizacion, EstadoSincronizacion

#### Aplicacion (RadioAficionado.Aplicacion) → Dominio, Compartido
- Qsos/RegistrarQso: RegistrarQsoComando, RegistrarQsoHandler, RegistrarQsoValidador
- ConfiguracionServicios (MediatR + FluentValidation)

#### Infraestructura (RadioAficionado.Infraestructura) → Dominio, Aplicacion, Compartido
- Persistencia: ContextoRadioAficionado, QsoConfiguracion, ActivacionConfiguracion, RepositorioQso (con paginacion y filtros), RepositorioActivaciones (EF Core, Include QSOs), UnidadDeTrabajo
- Adif: RegistroAdif, ParserAdif, GeneradorAdif, ConvertidorAdifQso
- DxCluster: ClienteDxCluster (cliente TCP/Telnet)
- Compliance: ServicioCompliance (verificacion IARU 3 regiones)
- Contests: GeneradorCabrillo (formato de logs para contests)
- Activaciones: ServicioActivaciones (gestion POTA/SOTA)
- PskReporter: ClientePskReporter (envio de spots)
- Configuracion: ServicioConfiguracionJson (persistencia JSON)
- Confirmaciones: ClienteLoTW, ClienteEQsl, ClienteClubLog, ServicioConfirmaciones (orquestador multifuente)
- Propagacion: ServicioPropagacion (modelo basado en SFI, predicciones HF)
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
- (pendiente: implementaciones de IDecodificadorDigital — FT8, CW, etc.)

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
- ViewModels (14):
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
- ViewModels (16):
  - InicioViewModel, QsoResumenViewModel
  - LogbookIndexViewModel, QsoDetalleViewModel, MapaContactoViewModel
  - RegistrarViewModel, IniciarSesionViewModel, PerfilViewModel, EditarPerfilViewModel
  - EstadisticasViewModel
  - ForoIndexViewModel, HiloDetalleViewModel, HiloResumenViewModel, CrearHiloViewModel, ResponderHiloViewModel, RespuestaViewModel
- Views:
  - Shared: _Layout.cshtml, Error.cshtml
  - Home: Index.cshtml, Privacy.cshtml
  - Inicio: Index.cshtml
  - Logbook: Index.cshtml, Detalle.cshtml, Mapa.cshtml
  - Cuenta: Registrar.cshtml, IniciarSesion.cshtml, Perfil.cshtml, EditarPerfil.cshtml
  - Foro: Index.cshtml, Detalle.cshtml, CrearHilo.cshtml
- CSS: sitio.css + site.css (variables --ra-*, tema oscuro azul/gris)
- JavaScript: estadisticas.js (Chart.js), mapa-contactos.js (Leaflet), site.js
- Librerias locales: bootstrap, chartjs, leaflet, jquery, jquery-validation, jquery-validation-unobtrusive
- Identity: ASP.NET Identity con UsuarioRadio, cookie auth, password policy
- Models: ErrorViewModel

### Tests (608 tests, todos pasando, 0 fallos)
- Dominio.Tests (308):
  - ObjetosDeValor: IndicativoTests, FrecuenciaTests, LocalizadorTests, CoordenadasTests, BandaRadioTests, ModoOperacionTests
  - Entidades: QsoTests
  - Compliance: PlanDeBandaTests
  - Rig: MapeadorModosTests
  - Activaciones: ReferenciaPotaTests, ReferenciaSotaTests, ActivacionTests
  - Contests: MotorContestTests
  - Dxcc: CatalogoDxccTests, EstadisticasDxccTests
- Infraestructura.Tests (228):
  - Dsp: TransformadaCooleyTukeyTests, ProcesadorEspectroTests, VentanasDspTests
  - Adif: ParserAdifTests, GeneradorAdifTests, ConvertidorAdifQsoTests
  - Compliance: ServicioComplianceTests
  - DxCluster: ClienteDxClusterTests
  - PskReporter: ClientePskReporterTests
  - Contests: GeneradorCabrilloTests
  - Configuracion: ServicioConfiguracionJsonTests
  - Confirmaciones: ClienteLoTWTests, ClienteEQslTests, ClienteClubLogTests, ServicioConfirmacionesTests
  - Propagacion: ServicioPropagacionTests
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
- ✅ Interfaces de dominio completas (20 interfaces)
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
- ✅ 608 tests (308 + 228 + 31 + 29 + 12), 0 fallos
- 🔨 Decodificador FT8 (ft8_lib P/Invoke)
- 🔨 Swap FFT managed → FFTW3 nativa
- 🔨 Migracion EF Core PostgreSQL para Identity
- 🔨 Logbook privado (CRUD de QSOs autenticado)
- 📋 APRS, Satelites
- 📋 SDR (SoapySDR) + mas modos digitales
- 📋 IA (ML.NET + ONNX) + Mobile
