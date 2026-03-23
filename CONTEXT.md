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

## Estado actual

**Fase 0 + Fase 1 completadas** — Cimientos + capa nativa.
**Fase 2 completada** — Logbook, ADIF, DX Cluster, Compliance, Contests, POTA/SOTA, PSK Reporter.

### Que funciona
- Solucion completa: 14 proyectos fuente + 5 proyectos de test
- La solucion compila sin errores ni warnings
- 321 tests (161 Dominio + 131 Infraestructura + 29 Aplicacion), todos pasando
- Modelo de dominio completo: objetos de valor, entidades, compliance, contests, activaciones
- ADIF parser/generador completo con conversion bidireccional Qso ↔ RegistroAdif
- Logbook UI con DataGrid paginado, filtros, import/export ADIF
- DX Cluster con cliente TCP/Telnet y UI de spots en tiempo real
- Motor de Contests con reglas, multiplicadores y GeneradorCabrillo
- Activaciones POTA/SOTA con ciclo de vida completo
- PSK Reporter para envio de spots
- Configuracion persistente en JSON
- ServicioCompliance con planes IARU para 3 regiones
- Control de rig (rigctld) y rotador (rotctld) via TCP
- Pipeline de audio con NAudio + FFT + ProcesadorEspectro
- WaterfallControl con SkiaSharp
- UI escritorio MVVM con ViewModels conectados a DI real

### Que viene despues
- **Decodificador FT8**: ft8_lib via P/Invoke
- **Waterfall en vivo**: conectar ProcesadorEspectro → PipelineAudio → ControlWaterfall via DI
- **Fase 3**: Web con autenticacion + logbook online
