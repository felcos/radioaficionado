# Sesion — RadioAficionado

## Ultima sesion: 2026-03-23

### Lo que se hizo

#### Fase 0 — Cimientos (2026-03-22)
- Estructura de solucion: 14 proyectos fuente + 5 test (Clean Architecture)
- Objetos de valor, entidades, compliance, interfaces de dominio
- Entidad Qso + MediatR + EF Core con SQLite y PostgreSQL

#### Fase 1 — Capa nativa + UI escritorio (2026-03-22)
- ClienteRigctld, ClienteRotctld, PipelineAudioNAudio, TransformadaCooleyTukey
- ProcesadorEspectro, VentanasDsp (Hann, Hamming, Blackman-Harris)
- ViewModels MVVM: PanelRig, PanelMensajes, PanelRegistroQso, VentanaPrincipal

#### Fase 2 — Features completas (2026-03-23)
- ADIF parser/generador completo con conversion bidireccional
- Logbook UI con DataGrid paginado, filtros, import/export ADIF
- DX Cluster con cliente TCP/Telnet y UI de spots
- ServicioCompliance con planes IARU para 3 regiones
- ControlWaterfall con SkiaSharp (paleta 256 colores, scroll vertical)
- ViewModels conectados a DI real (polling rig, MediatR)
- Motor de Contests (MotorContest, ReglaContest, GeneradorCabrillo)
- Activaciones POTA/SOTA (Activacion, ServicioActivaciones, RepositorioActivaciones)
- PSK Reporter (IPskReporter, ClientePskReporter)
- Configuracion persistente JSON (ConfiguracionCompleta, ServicioConfiguracionJson)
- Tracking DXCC (CatalogoDxcc ~170 entidades, EstadisticasDxcc)
- Panel DXCC UI con filtros por continente/estado, barras de progreso
- Confirmaciones externas (ClienteLoTW, ClienteEQsl, ClienteClubLog, ServicioConfirmaciones)
- Propagacion (ServicioPropagacion basado en SFI, predicciones HF)
- Paneles UI escritorio: Contest, Propagacion, Activaciones, DXCC, Configuracion

#### Fase 3 — Web + API + Sincronizacion (2026-03-23)
- Web MVP: homepage con estadisticas + logbook publico paginado con filtros
- Migracion inicial EF Core SQLite (tablas Activaciones, Qsos)
- ASP.NET Identity: UsuarioRadio, registro, login, perfil, editar perfil
- ContextoIdentidadRadioAficionado separado del DbContext compartido
- Entidades de foro: CategoriaForo, HiloForo, RespuestaForo
- ForoController con listado paginado, detalle, crear hilo, responder
- EstadisticasController con dashboard de datos agregados + Chart.js
- Mapa de contactos con Leaflet (marcadores interactivos)
- API REST: QsoApiController (CRUD QSOs) + AdifApiController (import/export ADIF)
- DTOs: QsoDto, FiltroQsoDto, ResultadoSincronizacionDto + MapeadorQsoDto
- IServicioSincronizacion: sincronizacion bidireccional escritorio ↔ API web
- ServicioSincronizacion: cliente HTTP con envio/recepcion de QSOs
- EstadoSincronizacionViewModel: indicador de estado en escritorio
- Tests: ServicioSincronizacionTests, QsoApiControllerTests, InicioControllerTests, LogbookControllerTests, PanelLogbookViewModelTests

### Estado de tests
- **608 tests totales** (308 Dominio + 228 Infraestructura + 31 Web + 29 Aplicacion + 12 Escritorio)
- Todos pasando, 0 fallos, 0 omitidos
- 5 proyectos de test cubriendo todas las capas

### Pendiente
- Crear migracion EF Core para las tablas de Identity en PostgreSQL
- Implementar logbook privado (CRUD de QSOs autenticado)
- Implementar decodificador FT8 con ft8_lib (P/Invoke)
- Swap FFT managed → FFTW3 nativa cuando haya binarios
- Conectar DI completa: ProcesadorEspectro → PipelineAudio → ControlWaterfall para waterfall en vivo

### Problemas encontrados
- Web: errores de compilacion menores en namespaces de ViewModels — no afectan tests
- Escritorio: referencia PanelDxccViewModel pendiente en VentanaPrincipalViewModel — no afecta tests
- Escritorio: lock de DLL de Avalonia ocasional en compilacion paralela (AVLN9999)

### Siguiente paso sugerido
- Crear migracion EF Core PostgreSQL para tablas de Identity (usuarios, roles, claims)
- Implementar logbook privado: CRUD de QSOs asociado al usuario autenticado
- O implementar decodificador FT8 con ft8_lib (P/Invoke)
