# ADR-001: Arquitectura Clean compartida entre escritorio y web

## Fecha
2026-03-22

## Contexto
Necesitamos una plataforma de radioafición con dos presentaciones: escritorio (Avalonia UI + SQLite) y web (ASP.NET MVC + PostgreSQL). La lógica de negocio (QSOs, ADIF, compliance, modos digitales) es idéntica en ambas.

## Decisión
Usar Clean Architecture con capas compartidas (Dominio + Aplicación) y capas de presentación/infraestructura independientes.

## Alternativas consideradas
1. **Monolito web-only**: descartado porque el escritorio necesita acceso directo al hardware (audio, radio, SDR).
2. **Dos proyectos independientes**: descartado por duplicación masiva de lógica.
3. **MAUI Blazor Hybrid**: descartado porque Blazor no tiene soporte real para audio en tiempo real ni P/Invoke eficiente.

## Consecuencias
- (+) Código de dominio se escribe una vez
- (+) Los tests del dominio validan ambas plataformas
- (-) Más proyectos en la solución (14 fuente + 5 test)
- (-) Requiere disciplina para no meter lógica en las capas de presentación
