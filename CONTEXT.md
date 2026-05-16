# CONTEXT.md — RadioAficionado

## Que es el proyecto

RadioAficionado es una plataforma unificada de radioaficion que combina una aplicacion de escritorio multiplataforma y una aplicacion web. Su objetivo es ser la herramienta integral que los radioaficionados necesitan: control de radio, waterfall, modos digitales (CW, FT8...), logbook, compliance regulatorio, DX cluster, contests, activaciones POTA/SOTA, tracking DXCC, confirmaciones (LoTW/eQSL/ClubLog), propagacion, APRS, satelites amateur, generacion de tarjetas QSL, foro comunitario, estadisticas, mapa de contactos e integracion con servicios externos.

## Por que existe

El ecosistema actual de software de radioaficion esta fragmentado:

- **Fragmentacion**: los operadores necesitan 5-10 programas distintos (WSJT-X, Log4OM, DX Lab Suite, fldigi, CHIRP...) que no se integran bien entre si.
- **Solo Windows**: la mayoria del software es exclusivo de Windows, dejando fuera a usuarios de Linux y macOS.
- **Sin IA**: ninguna herramienta aprovecha inteligencia artificial para analisis de propagacion, identificacion de señales o asistencia al operador.
- **UX pobre**: interfaces anticuadas, configuracion compleja, dependencia de cables de audio virtuales.

RadioAficionado pretende resolver todo esto con una plataforma moderna, multiplataforma, extensible y con IA integrada.

## Stack elegido

| Componente | Tecnologia | Por que |
|---|---|---|
| Lenguaje | C# / .NET 10 (LTS) | Rendimiento, ecosistema maduro, multiplataforma |
| Escritorio | Avalonia UI | Multiplataforma real (Windows, Linux, macOS) con XAML nativo |
| Web | ASP.NET MVC con Razor Views | Server-side rendering, sin SPA, sin npm |
| ORM | Entity Framework Core (Code First) | Migraciones, conversiones de value objects |
| BD local | SQLite | Sin servidor, ideal para escritorio |
| BD web | PostgreSQL | Robusta, escalable para multiusuario |
| Mediador | MediatR | CQRS, desacoplamiento de handlers |
| Validacion | FluentValidation | Validacion declarativa de comandos |
| Logging | Serilog | Structured logging, multiples sinks |
| DSP / FFT | FFTW3 (P/Invoke) | Estandar de la industria para FFT |
| FT8 | ft8_lib (P/Invoke) | Libreria C ligera, decodificacion FT8 pura |
| CW | FiltroGoertzel managed | Deteccion de tono liviana, sin FFT completa |
| Waterfall | SkiaSharp | Renderizado 2D de alto rendimiento |
| QSL | SkiaSharp | Generacion de tarjetas QSL en PNG/PDF/SVG |
| Audio | NAudio | Captura/reproduccion de audio en .NET |
| Rig control | Hamlib via rigctld (TCP) | Estandar universal de control de radio |
| Rotador | Hamlib via rotctld (TCP) | Estandar universal de control de rotador |
| APRS | APRS-IS (TCP) | Acceso al sistema global APRS via internet |
| Satelites | TLE + calculo orbital | Prediccion de pasos sin dependencias externas |
| Mapas | Leaflet (local) | Mapas interactivos, open source, sin CDN |
| Graficos | Chart.js (local) | Graficos interactivos, sin CDN |
| IA | ML.NET + ONNX Runtime | Inferencia local sin dependencias de nube |

## Arquitectura

Clean Architecture compartida entre escritorio, mobile y web:

```
Compartido ← Dominio ← Aplicacion ← Infraestructura ← Infraestructura.Sqlite
                                                      ← Infraestructura.Postgres
                                   ← Nativo.Dsp
                                   ← Nativo.ModosDigitales (CW decoder)
                                   ← Nativo.Audio
                                   ← Nativo.Rig
                                   ← Nativo.Rotador
                                   ← IA
                                   ← Escritorio (Avalonia UI)
                                   ← Mobile (Avalonia Mobile) ← Mobile.Android
                                                               ← Mobile.iOS
                                   ← Web (ASP.NET MVC + API REST)
```

Las capas **Dominio** y **Aplicacion** son compartidas entre escritorio, mobile y web. La infraestructura se divide por proveedor de BD. Los proyectos Nativo.* encapsulan P/Invoke a librerias nativas. La sincronizacion bidireccional conecta escritorio y web via API REST. El proyecto mobile comparte Dominio/Aplicacion/Infraestructura con escritorio pero sin servicios de hardware (rig, audio, rotador, waterfall).

## Decisiones tomadas

| Fecha | Decision | Motivo |
|---|---|---|
| 2026-03-22 | .NET 10 LTS | Version LTS mas reciente, soporte a largo plazo |
| 2026-03-22 | Avalonia UI para escritorio | Multiplataforma real con XAML, alternativa madura a MAUI |
| 2026-03-22 | Hamlib via rigctld (TCP) para control de radio | Estandar universal, soporta 200+ radios, comunicacion TCP desacoplada |
| 2026-03-22 | ft8_lib para decodificacion FT8 | Libreria C ligera, sin dependencias de Fortran (vs WSJT-X) |
| 2026-03-22 | Renombrado Nativo.Ft8 → Nativo.ModosDigitales | Extensibilidad para 50+ modos digitales |
| 2026-03-22 | FFTW3 para FFT | Estandar de la industria, rendimiento optimo |
| 2026-03-22 | FFT managed Cooley-Tukey como implementacion inicial | FFTW3 necesita binarios nativos; la interfaz ITransformadaFourier permite swap posterior |
| 2026-03-22 | SQLite local + PostgreSQL web | SQLite sin servidor para escritorio, PostgreSQL escalable para web |
| 2026-03-22 | SkiaSharp para waterfall | Renderizado 2D de alto rendimiento, multiplataforma |
| 2026-03-22 | Soporte completo ADIF 3.1.4 (48 modos + 43 submodos) | Compatibilidad total con el estandar de la industria |
| 2026-03-22 | Modelo de compliance por region ITU | Alertas de borde de banda, restricciones por licencia |
| 2026-03-22 | Interfaz extensible IDecodificadorDigital | Añadir modos digitales = implementar una interfaz |
| 2026-03-22 | Pipeline de audio interno (sin cables virtuales) | Eliminar configuracion compleja para el usuario |
| 2026-03-22 | Control de rotador via rotctld | Consistente con rigctld, mismo patron TCP |
| 2026-03-23 | ADIF como formato de import/export del logbook | Estandar universal de intercambio de QSOs en radioaficion |
| 2026-03-23 | DX Cluster via TCP/Telnet | Protocolo estandar de los clusters DX (AR-Cluster, DX Spider) |
| 2026-03-23 | PlanDeBandaItu con 3 regiones IARU | Compliance regulatorio correcto segun ubicacion geografica del operador |
| 2026-03-23 | Motor de Contests en capa de Dominio | Logica pura de evaluacion de QSOs, reglas y multiplicadores sin dependencias externas |
| 2026-03-23 | Cabrillo como formato de envio de logs de contests | Formato estandar aceptado por todos los organizadores de contests |
| 2026-03-23 | POTA/SOTA como entidades de primera clase | Activaciones con ciclo de vida propio, no solo metadatos del QSO |
| 2026-03-23 | PSK Reporter como servicio de infraestructura | Envio de spots al servicio centralizado de monitoreo de propagacion |
| 2026-03-23 | Configuracion persistente en JSON local | Formato legible, editable manualmente, sin necesidad de BD para preferencias |
| 2026-03-23 | Web MVP: homepage + logbook publico | Controladores MVC con Razor, tema oscuro, paginacion, filtros |
| 2026-03-23 | Migraciones EF Core en proyecto Sqlite separado | MigrationsAssembly apunta a Infraestructura.Sqlite; DesignTimeFactory para EF CLI sin startup Avalonia |
| 2026-03-23 | LoTW/eQSL/ClubLog como clientes HTTP independientes | Cada servicio tiene su interfaz + implementacion; ServicioConfirmaciones orquesta las 3 fuentes |
| 2026-03-23 | Modelo de propagacion basado en SFI | IndicesSolares como record inmutable; predicciones por banda HF con NivelPropagacion |
| 2026-03-23 | UI de escritorio completa con 8 vistas + 15 ViewModels | Cada modulo tiene su panel dedicado |
| 2026-03-23 | ContextoIdentidadRadioAficionado separado del DbContext principal | ContextoRadioAficionado es compartido con escritorio (SQLite); Identity es solo web, necesita IdentityDbContext separado |
| 2026-03-23 | Microsoft.Extensions.Identity.Stores en Dominio | UsuarioRadio hereda IdentityUser; paquete ligero sin dependencias de EF Core en capa de dominio |
| 2026-03-23 | API REST para sincronizacion escritorio ↔ web | QsoApiController + AdifApiController con DTOs y [Authorize]; ServicioSincronizacion como cliente HTTP bidireccional |
| 2026-03-23 | Leaflet local para mapa de contactos | Mapa interactivo sin CDN, marcadores con coordenadas de QSOs |
| 2026-03-23 | Chart.js local para estadisticas | Graficos interactivos sin CDN, datos via endpoints JSON del controller |
| 2026-03-23 | Foro con entidades de dominio propias | CategoriaForo, HiloForo, RespuestaForo; gestionado via ContextoIdentidadRadioAficionado |
| 2026-03-23 | Sincronizacion bidireccional como servicio de infraestructura | IServicioSincronizacion con records inmutables para configuracion, resultado y estado |
| 2026-03-23 | FiltroGoertzel para deteccion de tono CW | Alternativa liviana a FFT completa; solo necesita detectar un tono a frecuencia conocida |
| 2026-03-23 | APRS via APRS-IS (TCP) | Acceso al sistema global APRS sin necesidad de TNC fisico ni modem de paquetes |
| 2026-03-23 | Calculo orbital con TLE propio | Prediccion de pasos de satelites sin dependencias externas; TLE actualizables desde CelesTrak/AMSAT |
| 2026-03-23 | SkiaSharp para generacion de tarjetas QSL | Reutiliza la dependencia existente; exportacion en PNG, PDF y SVG |
| 2026-04-11 | Avalonia Mobile para Android e iOS | Reutiliza capas Dominio/Aplicacion/Infraestructura; sin servicios de hardware (rig, audio, rotador, waterfall) |
| 2026-04-11 | Proyecto compartido Mobile + Android + iOS separados | Patron estandar Avalonia: proyecto compartido (net10.0) + head projects por plataforma (net10.0-android, net10.0-ios) |
| 2026-04-12 | Audio USB del radio integrado al ciclo de conexion CAT | Al conectar: CAT + captura audio + waterfall FFT. Al desconectar: todo se detiene. Sin cables de audio virtuales |
| 2026-04-12 | PanelRigViewModel crea IControlRig en runtime (no por DI) | ClienteCatSerial o ClienteRigctld se instancian segun la seleccion del usuario al conectar |
| 2026-04-12 | Waterfall se inicia automaticamente al conectar | ServicioWaterfall.IniciarAsync(2048) se llama despues de iniciar la captura de audio |

## Estado actual

**Fases 0-7 completadas. Integracion final verificada 2026-04-11.**

- **Fase 0** — Cimientos: estructura, dominio, EF Core, MediatR
- **Fase 1** — Capa nativa: rig, audio, DSP, rotador, UI MVVM
- **Fase 2** — Features: ADIF, logbook, DX Cluster, compliance, contests, POTA/SOTA, PSK Reporter, DXCC, confirmaciones, propagacion
- **Fase 3** — Web: homepage, logbook publico, Identity, API REST, sincronizacion, mapa, estadisticas, foro
- **Fase 4** — Avanzado: CW decoder, APRS, satelites, generador QSL
- **Fase 5** — Integracion: Waterfall en vivo, FFTW3, FT8, migracion PostgreSQL Identity, logbook privado, modos digitales (FT4/RTTY/PSK31/JS8), IA (ML.NET), perfiles publicos
- **Fase 6** — Expansion: SDR (SoapySDR), modos digitales (JT65/JT9/Olivia/SSTV), ONNX Runtime, Mobile (Android+iOS), vistas web completadas
- **Fase 7** — Completitud: WSPR/FT2/Q65, SDR→Waterfall, modelos ONNX entrenables, auditoría completa (DI, CRUD, documentación interfaces)
- **Fase 8 (en curso)** — Rig + Waterfall + FT8: conexión CAT serial robusta, audio USB del radio, waterfall en vivo conectado al audio real
- **Fase 9 (completada 2026-05-15)** — Paneles conectados: DXCC progreso/filtro, propagación colores, Log QSO, Contest datos reales, Lanzador persistido, tests integración

### Que funciona
- Solucion completa: 19 proyectos fuente + 7 proyectos de test
- **1309 tests** (308 Dominio + 676 Infraestructura + 67 Web + 29 Aplicacion + 12 Escritorio + 86 IA + 131 Servicio), **todos pasando, 0 fallos**
- Build limpio: 0 errores, 0 warnings propios
- Modelo de dominio completo: objetos de valor, 5 entidades, compliance, contests, activaciones, DXCC, propagacion, foro, APRS, satelites, QSL, IA
- 30 interfaces de dominio definidas, implementadas, registradas en DI y documentadas con XML completo
- ADIF parser/generador completo con conversion bidireccional Qso ↔ RegistroAdif
- Logbook UI escritorio con DataGrid paginado, filtros, import/export ADIF
- **Logbook privado web**: CRUD completo de QSOs con [Authorize], filtros, paginacion
- DX Cluster con cliente TCP/Telnet y UI de spots en tiempo real
- Motor de Contests con reglas, multiplicadores, GeneradorCabrillo y Panel de Contest UI
- Activaciones POTA/SOTA con ciclo de vida completo y Panel de Activaciones UI con cronometro
- PSK Reporter para envio de spots
- Configuracion persistente en JSON + Ventana de Configuracion UI
- ServicioCompliance con planes IARU para 3 regiones
- Tracking DXCC: CatalogoDxcc (~170 entidades), EstadisticasDxcc, Panel DXCC UI con filtros y barras de progreso
- Confirmaciones externas: ClienteLoTW, ClienteEQsl, ClienteClubLog + ServicioConfirmaciones orquestador
- Propagacion: ServicioPropagacion (modelo SFI) + Panel de Propagacion UI con indices solares
- **IA con ML.NET + ONNX**: AnalizadorPropagacionMlNet + ClasificadorSenalesMlNet + MotorInferenciaOnnx + ClasificadorSenalesOnnx (con fallback) + ExportadorModeloOnnx
- Decodificador CW: DecodificadorCw con FiltroGoertzel, TablaMorse, ConfiguracionCw
- **Decodificador FT8**: ft8_lib via P/Invoke
- **13 decodificadores digitales**: CW, FT8, FT4, FT2, RTTY, PSK31, JS8, JT65, JT9, Q65, Olivia, SSTV, WSPR con RegistroDecodificadores
- APRS: PaqueteAprs, PosicionAprs, MensajeAprs, ObjetoAprs, ClienteAprsIs, ParserAprs
- Satelites amateur: CatalogoSatelites (~30 satelites), CalculadorOrbital con TLE, PanelSatelitesViewModel + PanelSatelites.axaml
- Generador de tarjetas QSL: PlantillaQsl, DatosQsl, GeneradorQslSkia (PNG/PDF/SVG)
- Control de rig (rigctld) y rotador (rotctld) via TCP
- Pipeline de audio con NAudio + FFT + ProcesadorEspectro
- **Waterfall en vivo**: IServicioWaterfall → ServicioWaterfall con solapamiento 50%, PanelWaterfallViewModel
- **FFTW3 nativa**: FabricaTransformadaFourier con fallback Cooley-Tukey, TransformadaFftw3 via P/Invoke
- WaterfallControl con SkiaSharp
- **SDR**: IReceptorSdr, SoapySdrNativo (P/Invoke), ReceptorSoapySdr, PanelSdrViewModel
- UI escritorio MVVM: 17 ViewModels, 9 vistas, 1 control custom
- **Mobile (Avalonia)**: proyecto compartido + Android + iOS, 5 ViewModels, 4 vistas
- Web completa: homepage, logbook publico, logbook privado, perfiles publicos, mapa, estadisticas con Chart.js
- ASP.NET Identity: registro, login, perfil, editar perfil
- **Migracion PostgreSQL Identity**: FabricaContextoIdentidadEnDiseño + MigrationsAssembly configurado
- **Perfiles publicos de operadores**: OperadoresController con directorio paginado, perfil detallado, mapa de contactos JSON
- API REST: QsoApiController + AdifApiController con DTOs, mapeadores y [Authorize]
- Sincronizacion bidireccional: ServicioSincronizacion + EstadoSincronizacionViewModel
- Foro comunitario: categorias, hilos, respuestas, paginacion
- Mapa de contactos con Leaflet (marcadores interactivos)
- Migracion EF Core SQLite con tablas Activaciones y Qsos

### Problemas conocidos
- Escritorio: lock de DLL de Avalonia ocasional en compilacion paralela (error AVLN9999)
- Warning NU1903: paquete transitivo Tmds.DBus.Protocol 0.20.0 tiene vulnerabilidad conocida (dependencia de Avalonia, no afecta funcionalidad)

### Que viene despues
- **DX Cluster real**: conectar ClienteDxClusterTelnet al HubEstado con spots en tiempo real
- **Graficos interactivos**: propagacion con Chart.js, mapa DXCC con Leaflet
- **Docker compose**: web + PostgreSQL para deployment
- **CI/CD**: GitHub Actions para build + tests automaticos
- **SDR hardware testing**: probar con dispositivos RTL-SDR reales
- **Mobile testing**: probar en emuladores Android/iOS
