# Changelog — RadioAficionado

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
