# CONTEXT.md — RadioAficionado

## Qué es el proyecto

RadioAficionado es una plataforma unificada de radioafición que combina una aplicación de escritorio multiplataforma y una aplicación web. Su objetivo es ser la herramienta integral que los radioaficionados necesitan: control de radio, waterfall, modos digitales, logbook, compliance regulatorio, DX cluster, e integración con servicios externos (LoTW, eQSL, ClubLog, POTA, SOTA).

## Por qué existe

El ecosistema actual de software de radioafición está fragmentado:

- **Fragmentación**: los operadores necesitan 5-10 programas distintos (WSJT-X, Log4OM, DX Lab Suite, fldigi, CHIRP...) que no se integran bien entre sí.
- **Solo Windows**: la mayoría del software es exclusivo de Windows, dejando fuera a usuarios de Linux y macOS.
- **Sin IA**: ninguna herramienta aprovecha inteligencia artificial para análisis de propagación, identificación de señales o asistencia al operador.
- **UX pobre**: interfaces anticuadas, configuración compleja, dependencia de cables de audio virtuales.

RadioAficionado pretende resolver todo esto con una plataforma moderna, multiplataforma, extensible y con IA integrada.

## Stack elegido

| Componente | Tecnología | Por qué |
|---|---|---|
| Lenguaje | C# / .NET 10 (LTS) | Rendimiento, ecosistema maduro, multiplataforma |
| Escritorio | Avalonia UI | Multiplataforma real (Windows, Linux, macOS) con XAML nativo |
| Web | ASP.NET MVC con Razor Views | Server-side rendering, sin SPA, sin npm |
| ORM | Entity Framework Core (Code First) | Migraciones, conversiones de value objects |
| BD local | SQLite | Sin servidor, ideal para escritorio |
| BD web | PostgreSQL | Robusta, escalable para multiusuario |
| Mediador | MediatR | CQRS, desacoplamiento de handlers |
| Validación | FluentValidation | Validación declarativa de comandos |
| Logging | Serilog | Structured logging, múltiples sinks |
| DSP / FFT | FFTW3 (P/Invoke) | Estándar de la industria para FFT |
| FT8 | ft8_lib (P/Invoke) | Librería C ligera, decodificación FT8 pura |
| Waterfall | SkiaSharp | Renderizado 2D de alto rendimiento |
| Audio | NAudio | Captura/reproducción de audio en .NET |
| Rig control | Hamlib vía rigctld (TCP) | Estándar universal de control de radio |
| Rotador | Hamlib vía rotctld (TCP) | Estándar universal de control de rotador |
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

Las capas **Dominio** y **Aplicacion** son compartidas entre escritorio y web. La infraestructura se divide por proveedor de BD. Los proyectos Nativo.* encapsulan P/Invoke a librerías nativas.

## Decisiones tomadas

| Fecha | Decisión | Motivo |
|---|---|---|
| 2026-03-22 | .NET 10 LTS | Versión LTS más reciente, soporte a largo plazo |
| 2026-03-22 | Avalonia UI para escritorio | Multiplataforma real con XAML, alternativa madura a MAUI |
| 2026-03-22 | Hamlib vía rigctld (TCP) para control de radio | Estándar universal, soporta 200+ radios, comunicación TCP desacoplada |
| 2026-03-22 | ft8_lib para decodificación FT8 | Librería C ligera, sin dependencias de Fortran (vs WSJT-X) |
| 2026-03-22 | Renombrado Nativo.Ft8 → Nativo.ModosDigitales | Extensibilidad para 50+ modos digitales |
| 2026-03-22 | FFTW3 para FFT | Estándar de la industria, rendimiento óptimo |
| 2026-03-22 | FFT managed Cooley-Tukey como implementación inicial | FFTW3 necesita binarios nativos; la interfaz ITransformadaFourier permite swap posterior |
| 2026-03-22 | SQLite local + PostgreSQL web | SQLite sin servidor para escritorio, PostgreSQL escalable para web |
| 2026-03-22 | SkiaSharp para waterfall | Renderizado 2D de alto rendimiento, multiplataforma |
| 2026-03-22 | Soporte completo ADIF 3.1.4 (48 modos + 43 submodos) | Compatibilidad total con el estándar de la industria |
| 2026-03-22 | Modelo de compliance por región ITU | Alertas de borde de banda, restricciones por licencia |
| 2026-03-22 | Interfaz extensible IDecodificadorDigital | Añadir modos digitales = implementar una interfaz |
| 2026-03-22 | Pipeline de audio interno (sin cables virtuales) | Eliminar configuración compleja para el usuario |
| 2026-03-22 | Control de rotador vía rotctld | Consistente con rigctld, mismo patrón TCP |

## Estado actual

**Fase 0 completada** — Cimientos de la plataforma.

### Qué funciona
- Solución completa: 14 proyectos fuente + 5 proyectos de test
- La solución compila sin errores ni warnings
- Modelo de dominio completo: objetos de valor (Indicativo, Frecuencia, Localizador, Coordenadas, BandaRadio, ModoOperacion), entidades (Qso), compliance regulatorio (PlanDeBanda, SegmentoBanda, ResultadoCompliance)
- Handler MediatR para registro de QSOs
- EF Core configurado con conversiones para value objects, proveedores SQLite y PostgreSQL
- Shell de Avalonia UI (escritorio) con DI, ventana principal y tema Fluent oscuro
- Shell ASP.NET MVC (web) estándar
- 89 tests unitarios del dominio pasando

### Qué viene después
**Fase 1**: Control de rig (rigctld) + Waterfall (FFTW3 + SkiaSharp) + Decodificación FT8 (ft8_lib)
