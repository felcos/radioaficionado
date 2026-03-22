# ADR-002: Sistema extensible de modos digitales

## Fecha
2026-03-22

## Contexto
Existen 50+ modos digitales activos en radioafición. Empezamos con FT8 pero necesitamos soportar muchos más (RTTY, PSK31, JS8, Olivia, FreeDV, M17, APRS...).

## Decisión
- Renombrar Nativo.Ft8 → Nativo.ModosDigitales
- Crear interfaz IDecodificadorDigital que cada modo implementa
- Crear IRegistroDecodificadores para descubrimiento dinámico
- El enum ModoOperacion incluye todos los modos ADIF 3.1.4 (48 modos + 43 submodos)

## Alternativas consideradas
1. **Un proyecto por modo**: demasiados proyectos, compilación lenta.
2. **Integrar WSJT-X completo**: es Fortran, imposible interoperar directamente.
3. **Solo FT8 sin extensibilidad**: descartado por requisito de producto.

## Consecuencias
- (+) Añadir un modo nuevo = implementar una interfaz
- (+) Compatible con ADIF 3.1.4 desde el día 1
- (-) Complejidad inicial mayor
