# Changelog — RadioAficionado

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
- Tabla "usuarios" con indicativo único, CHECK constraint para RegionItu (1-3)
- Traducción de errores de Identity al español
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

### test: 550 tests (308 Dominio + 213 Infraestructura + 29 Aplicacion), todos pasando
- ClienteLoTWTests, ClienteEQslTests, ClienteClubLogTests, ServicioConfirmacionesTests (Infraestructura.Tests/Confirmaciones/)
- ServicioPropagacionTests (Infraestructura.Tests/Propagacion/)

## [0.9.1] — 2026-03-23 — Migración inicial EF Core SQLite

### feat: Migración inicial de base de datos (Infraestructura.Sqlite)
- Migración "Inicial" con tablas Activaciones y Qsos, FK, índices
- FabricaContextoEnDiseño (IDesignTimeDbContextFactory) para EF Core CLI
- MigrationsAssembly configurado en ConfiguracionSqlite y FabricaContextoEnDiseño
- Paquete Microsoft.EntityFrameworkCore.Design añadido al proyecto Sqlite
- Archivos creados: FabricaContextoEnDiseño.cs, Migraciones/20260323100132_Inicial.cs (+Designer +Snapshot)
- Archivos modificados: ConfiguracionSqlite.cs, RadioAficionado.Infraestructura.Sqlite.csproj

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
- Logbook/Index.cshtml: tabla paginada con filtros (indicativo, modo, banda, fechas), paginacion completa con ellipsis
- Logbook/Detalle.cshtml: detalle de QSO con breadcrumb, datos del contacto, senales, potencia, notas, metadatos
- Error.cshtml actualizado a espanol
- Archivos: Views/Shared/_Layout.cshtml, Views/Inicio/Index.cshtml, Views/Logbook/Index.cshtml, Views/Logbook/Detalle.cshtml

### feat: CSS tema oscuro (Web/wwwroot/css)
- sitio.css: variables CSS personalizadas (--ra-*), tema oscuro consistente con colores azul/gris
- Estilos para: navegacion, tarjetas, tablas, badges, botones, formularios, paginacion, responsive
- Archivo: wwwroot/css/sitio.css

### refactor: Program.cs
- Ruta por defecto cambiada de Home a Inicio
- ExceptionHandler apuntando a /Inicio/Error

## [0.8.1] — 2026-03-23 — Panel DXCC UI

### feat: Panel de tracking DXCC (Escritorio)
- PanelDxccViewModel: carga datos desde IRepositorioQso, calcula estadísticas con EstadisticasDxcc
- EntidadDxccVm con indicadores visuales: verde=confirmada, amarillo=trabajada, gris=no trabajada
- Filtros reactivos: continente (Todos/AF/AS/EU/NA/OC/SA), estado (Todas/Trabajadas/NoTrabajadas/Confirmadas)
- ResumenContinenteVm y ResumenBandaVm con barras de progreso
- PanelDxcc.axaml: barra de estadísticas, filtros, DataGrid de entidades, panel lateral con resúmenes
- Tema oscuro consistente, compiled bindings, integrado como pestaña "DXCC" en VentanaPrincipal
- Archivos: Escritorio/ViewModels/PanelDxccViewModel.cs, Escritorio/Vistas/PanelDxcc.axaml(.cs)

## [0.8.0] — 2026-03-23 — Tracking DXCC y Premios

### feat: Catálogo DXCC (Dominio/Dxcc)
- EntidadDxcc (record): Numero, Nombre, Prefijo, Continente, ZonaCq, ZonaItu, Latitud, Longitud, Eliminada
- CatalogoDxcc: catálogo estático con ~170 entidades DXCC (todas las más activas + eliminadas históricas)
- Búsqueda por prefijo (exacta + progresiva), por indicativo, listado completo, solo activas
- Prefijos alternativos registrados para USA, Rusia, Japón, Alemania, China, Canadá, Brasil, Argentina, España, Francia, Italia, Inglaterra, Australia, México, India, Corea
- ConfirmacionQso + TipoConfirmacion (LoTW, QSL física, eQSL, directa, bureau)
- Archivos: Dominio/Dxcc/EntidadDxcc.cs, CatalogoDxcc.cs, ConfirmacionQso.cs

### feat: Estadísticas DXCC (Dominio/Dxcc)
- EstadisticasDxcc: cálculo de entidades trabajadas, confirmadas, por banda, por modo, faltantes
- ResumenDxcc (record): TotalTrabajadas, TotalConfirmadas, PorBanda, PorModo, PorContinente
- Archivo: Dominio/Dxcc/EstadisticasDxcc.cs

### test: Tests DXCC (56 tests nuevos)
- CatalogoDxccTests: 15 tests (búsqueda por prefijo principal/alternativo, por indicativo, nulo/vacío/inexistente, eliminadas, continentes, zonas CQ/ITU, case-insensitive)
- EstadisticasDxccTests: 12 tests (trabajadas, confirmadas, por banda, por modo, faltantes, resumen, null checks)
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

### feat: CancelarAsync + ObtenerTodasAsync en servicio de activaciones
- Añadidos a IServicioActivaciones y ServicioActivaciones
- Añadido ObtenerTodasAsync a IRepositorioActivaciones
- Registros de DI: IRepositorioActivaciones, IServicioActivaciones

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

### test: 321 tests (161 Dominio + 131 Infraestructura + 29 Aplicacion)
- MotorContestTests: tests del motor de contests (Dominio.Tests/Contests/)
- ReferenciaPotaTests: validacion de referencias POTA (Dominio.Tests/Activaciones/)
- GeneradorCabrilloTests: tests del generador Cabrillo (Infraestructura.Tests/Contests/)
- ClientePskReporterTests: tests del cliente PSK Reporter (Infraestructura.Tests/PskReporter/)

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
- Archivos: Dominio/Interfaces/IDxCluster.cs, Infraestructura/DxCluster/ClienteDxCluster.cs, Escritorio/ViewModels/PanelDxClusterViewModel.cs, Escritorio/Vistas/PanelDxCluster.axaml*

### feat: ServicioCompliance regulatorio (Infraestructura/Compliance)
- PlanDeBandaItu: planes de banda IARU para 3 regiones
- ServicioCompliance: verificacion de frecuencia/modo contra plan de banda
- Archivos: Dominio/Compliance/PlanDeBandaItu.cs, Infraestructura/Compliance/ServicioCompliance.cs

### feat: ViewModels conectados a DI real (Escritorio)
- PanelRigViewModel: polling real al rig con timer
- PanelRegistroQsoViewModel: registro de QSOs via MediatR
- VentanaPrincipalViewModel: navegacion entre paneles
- Archivos: Escritorio/ViewModels/*.cs

### test: Tests ADIF + Compliance + DX Cluster + Aplicacion
- ConvertidorAdifQsoTests, GeneradorAdifTests, ParserAdifTests (Infraestructura.Tests/Adif/)
- ServicioComplianceTests (Infraestructura.Tests/Compliance/)
- ClienteDxClusterTests (Infraestructura.Tests/DxCluster/)
- RegistrarQsoHandlerTests, RegistrarQsoValidadorTests (Aplicacion.Tests/Qsos/)

## [0.5.0] — 2026-03-23 — ControlWaterfall con SkiaSharp

### feat: ControlWaterfall (Escritorio/Controles)
- Control Avalonia custom con renderizado SkiaSharp via ICustomDrawOperation
- SKBitmap interno con scroll vertical (unsafe Buffer.MemoryCopy)
- Paleta de 256 colores precalculada: negro → azul → verde → amarillo → rojo
- Metodo AgregarLinea(LineaEspectro) thread-safe (~25 FPS)
- Propiedades: AnchoFft, DbMinimo, DbMaximo
- Reemplazado placeholder en VentanaPrincipal.axaml
- Archivos: Controles/ControlWaterfall.cs, Vistas/VentanaPrincipal.axaml

## [0.4.0] — 2026-03-22 — Tests capa nativa + UI escritorio

### test: 201 tests (161 Dominio + 40 Infraestructura)
- TransformadaCooleyTukeyTests: 10 tests (seno puro, silencio, tamaños, dispose)
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
