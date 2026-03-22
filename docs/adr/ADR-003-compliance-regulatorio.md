# ADR-003: Modelo de compliance regulatorio integrado

## Fecha
2026-03-22

## Contexto
Los operadores de radioafición deben respetar regulaciones de su país y región ITU: bandas permitidas, modos por segmento, potencia máxima, privilegios por clase de licencia. La mayoría del software actual no ofrece alertas de compliance.

## Decisión
Integrar un modelo de compliance en el dominio desde Fase 0:
- RegionItu (3 regiones)
- NivelLicencia (Basico, Intermedio, Avanzado)
- PlanDeBanda con SegmentoBanda (tipo, potencia, nivel mínimo)
- ResultadoCompliance con tipos de violación

## Alternativas consideradas
1. **Sin compliance**: riesgo legal para operadores nuevos.
2. **Compliance solo en UI**: frágil, difícil de testear.

## Consecuencias
- (+) Alertas de borde de banda en tiempo real
- (+) Restricciones por licencia automáticas
- (+) Testeable con tests unitarios (9 tests ya)
- (-) Requiere mantener datos regulatorios actualizados por país
