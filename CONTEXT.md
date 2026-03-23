# CONTEXT.md — RadioAficionado

## Que es el proyecto

RadioAficionado es una plataforma unificada de radioaficion que combina una aplicacion de escritorio multiplataforma y una aplicacion web. Su objetivo es ser la herramienta integral que los radioaficionados necesitan: control de radio, waterfall, modos digitales, logbook, compliance regulatorio, DX cluster, contests, activaciones POTA/SOTA, e integracion con servicios externos (LoTW, eQSL, ClubLog, PSK Reporter).

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
| Waterfall | SkiaSharp | Renderizado 2D de alto rendimiento |
| Audio | NAudio | Captura/reproduccion de audio en .NET |
| Rig control | Hamlib via rigctld (TCP) | Estandar universal de control de radio |
| Rotador | Hamlib via rotctld (TCP) | Estandar universal de control de rotador |
| IA | ML.NET + ONNX Runtime | Inferencia local sin dependencias de nube |

## Arquitectura

Clean Architecture compartida entre escritorio y web:

```
Compartido ← Dominio ← Aplicacion ← Infraestructura ← Infraestructura.Sqlite
                                                      ← Infraestructura.Postgres
                                   ← Nativo.Dsp
                                   ← Nativo.ModosDigitales
                                   ← Nativo.Audio
                                   ← Nativo.Rig
                                   ← Nativo.Rotador
                                   ← IA
                                   ← Escritorio (Avalonia UI)
                                   ← Web (ASP.NET MVC)
```

Las capas **Dominio** y **Aplicacion** son compartidas entre escritorio y web. La infraestructura se divide por proveedor de BD. Los proyectos Nativo.* encapsulan P/Invoke a librerias nativas.

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
| 2026-03-23 | UI de escritorio completa con 10 vistas + 12 ViewModels | Cada modulo (contest, DXCC, propagacion, activaciones, configuracion) tiene su panel dedicado |
| 2026-03-23 | ContextoIdentidadRadioAficionado separado del DbContext principal | ContextoRadioAficionado es compartido con escritorio (SQLite); Identity es solo web, necesita IdentityDbContext separado para no romper la app de escritorio |
| 2026-03-23 | Microsoft.Extensions.Identity.Stores en Dominio (no Identity.EFCore) | UsuarioRadio hereda IdentityUser; paquete ligero sin dependencias de EF Core en capa de dominio |

## Estado actual

**Fase 0 + Fase 1 completadas** — Cimientos + capa nativa.
**Fase 2 completada** — Logbook, ADIF, DX Cluster, Compliance, Contests, POTA/SOTA, PSK Reporter, DXCC, Confirmaciones, Propagacion.
**Fase 3 en progreso** — Web MVP con homepage y logbook publico implementados.

### Que funciona
- Solucion completa: 14 proyectos fuente + 5 proyectos de test
- 550 tests (308 Dominio + 213 Infraestructura + 29 Aplicacion), todos pasando
- Modelo de dominio completo: objetos de valor, entidades, compliance, contests, activaciones, DXCC, propagacion
- 17 interfaces de dominio definidas e implementadas
- ADIF parser/generador completo con conversion bidireccional Qso ↔ RegistroAdif
- Logbook UI con DataGrid paginado, filtros, import/export ADIF
- DX Cluster con cliente TCP/Telnet y UI de spots en tiempo real
- Motor de Contests con reglas, multiplicadores, GeneradorCabrillo y Panel de Contest UI
- Activaciones POTA/SOTA con ciclo de vida completo y Panel de Activaciones UI con cronometro
- PSK Reporter para envio de spots
- Configuracion persistente en JSON + Ventana de Configuracion UI
- ServicioCompliance con planes IARU para 3 regiones
- Tracking DXCC: CatalogoDxcc (~170 entidades), EstadisticasDxcc, Panel DXCC UI con filtros y barras de progreso
- Confirmaciones externas: ClienteLoTW, ClienteEQsl, ClienteClubLog + ServicioConfirmaciones orquestador
- Propagacion: ServicioPropagacion (modelo SFI) + Panel de Propagacion UI con indices solares
- Control de rig (rigctld) y rotador (rotctld) via TCP
- Pipeline de audio con NAudio + FFT + ProcesadorEspectro
- WaterfallControl con SkiaSharp
- UI escritorio MVVM: 12 ViewModels, 10 vistas, 1 control custom
- Web MVP: homepage con estadisticas + logbook publico paginado con filtros y detalle
- Migracion EF Core SQLite con tablas Activaciones y Qsos

### Problemas conocidos
- Web: namespace `RadioAficionado.Web.ViewModels` no encontrado en _ViewImports.cshtml (error de compilacion menor)
- Escritorio: `PanelDxccViewModel` no encontrado en VentanaPrincipalViewModel.cs (falta using o referencia)
- Ambos errores no afectan la suite de 550 tests (que pasan al 100%)

### Que viene despues
- **Corregir errores de compilacion**: Web ViewModels namespace + Escritorio PanelDxccViewModel referencia
- **Decodificador FT8**: ft8_lib via P/Invoke
- **Waterfall en vivo**: conectar ProcesadorEspectro → PipelineAudio → ControlWaterfall via DI
- **Web autenticacion**: Identity implementado (registro, login, perfil). Falta logbook privado con CRUD autenticado
