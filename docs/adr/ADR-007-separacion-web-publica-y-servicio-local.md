# ADR-007: Separación entre Web pública y Servicio local

**Fecha:** 2026-06-12
**Estado:** Aceptado (documenta decisión ya implementada)

## Contexto

RadioAficionado tiene dos aplicaciones ASP.NET con responsabilidades y entornos distintos:

- **RadioAficionado.Servicio** — corre en el PC del usuario (`http://localhost:5200`). Controla el hardware (CAT serie / rigctld, audio, SDR, decodificadores), usa **SQLite** local y expone 4 hubs SignalR (Rig, Waterfall, Decodificaciones, Estado).
- **RadioAficionado.Web** — servidor público (`https://ham.felcos.es`, puerto 5700). Gestiona usuarios con ASP.NET Identity sobre **PostgreSQL**, foro, claves API y el relay/tunelado hacia el Servicio (ADR-005). No tiene acceso directo al hardware.

Surge la pregunta: ¿es duplicación de código tener dos apps web? La auditoría confirma que **no**: comparten Dominio y Aplicación, pero cada una tiene responsabilidades disjuntas.

## Decisión

Mantener dos proyectos de presentación separados sobre el mismo núcleo Clean Architecture:

| Aspecto | Servicio (local) | Web (público) |
|---|---|---|
| Entorno | PC del usuario | Servidor compartido |
| BD | SQLite | PostgreSQL + Identity |
| Responsabilidad | Control de hardware y estado del rig | Usuarios, foro, relay remoto |
| Auth | — (local) | Cookie Identity + API key |
| Despliegue | App de escritorio / lanzador | systemd en Ubuntu ARM64 |

## Alternativas descartadas

1. **Una sola app web que hable con el hardware** — imposible: el hardware está en casa del usuario, no en el servidor.
2. **Exponer el Servicio local a internet con port forwarding** — inseguro y requiere configuración del router (ya descartado en ADR-005).
3. **Fusionar ambas tras feature flags** — acoplaría dos ciclos de vida y dos modelos de datos distintos.

## Consecuencias

- (+) Cada app tiene una superficie de ataque y un modelo de datos acotados.
- (+) El relay remoto (ADR-005) conecta ambas sin exponer el hardware.
- (-) Algunos DTOs de transporte se duplican entre `Servicio/Dtos` y `Compartido/Contratos`; deben consolidarse en `Compartido` como única fuente de verdad (ver DEUDA.md / auditoría 2026-06-12).
- (-) Dos pipelines de configuración y despliegue.
