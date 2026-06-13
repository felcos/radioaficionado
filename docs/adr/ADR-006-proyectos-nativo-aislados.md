# ADR-006: Interop nativo aislado en proyectos RadioAficionado.Nativo.*

**Fecha:** 2026-06-12
**Estado:** Aceptado (documenta decisión ya implementada)

## Contexto

El control de hardware y el procesamiento de señal de RadioAficionado requieren:
- P/Invoke a librerías nativas (NAudio para audio, librerías DSP, decodificadores de modos digitales).
- I/O de bajo nivel (puertos COM/serie para CAT, control de rotadores).
- Código `unsafe` para rendimiento crítico (SDR, FFT/waterfall).

Estas necesidades no deben contaminar las capas de Dominio, Aplicación ni las presentaciones (Web, Servicio, Escritorio, Mobile).

## Decisión

Crear proyectos separados `RadioAficionado.Nativo.*` (Audio, Dsp, ModosDigitales, Rig, Rotador, Sdr) que:
- Solo implementan interfaces definidas en `Dominio` (`IControlRig`, `IDecodificadorDigital`, `IRegistroDecodificadores`, `IDispositivoAudio`, etc.).
- No referencian Aplicación, Infraestructura ni ninguna presentación.
- Pueden habilitar `AllowUnsafeBlocks` y dependencias nativas localizadas.
- Acoplamiento horizontal permitido y mínimo (p. ej. `Nativo.Sdr` → `Nativo.Dsp`).
- Se registran en DI mediante módulos de configuración (p. ej. `ConfiguracionServiciosModosDigitales`, con 18 decodificadores).

## Alternativas descartadas

1. **P/Invoke inline en Aplicación** — violaría la dirección de dependencias de Clean Architecture.
2. **Una sola librería "Nativo" monolítica** — mezclaría audio, DSP, CAT y SDR con dependencias y `unsafe` innecesariamente acoplados.
3. **Librerías externas sin wrapper** — difíciles de testear y de sustituir.

## Consecuencias

- (+) Complejidad nativa aislada y reutilizable en todas las presentaciones (Web, Servicio, Escritorio, Mobile).
- (+) Sustituible y testeable tras interfaces de Dominio.
- (-) 6 proyectos adicionales en la solución.

## Criterio para crear un nuevo `Nativo.*`
Cuando una capacidad requiera P/Invoke, I/O de hardware o `unsafe`, y tenga una interfaz clara en Dominio. En caso contrario, va en `Infraestructura`.

## Nota de auditoría (2026-06-12)
`RadioAficionado.Web` referencia `Nativo.ModosDigitales` sin necesitarlo (solo usa enums de modos, ya presentes en Dominio). Eliminar esa referencia para respetar este ADR. Ver DEUDA.md.
