# ERRORES.md — Historial de errores resueltos (Art. 33)

> Consultar antes de diagnosticar. Registrar aquí cada error no trivial al resolverlo: síntoma, causa raíz, solución y cómo prevenirlo.

| Fecha | Síntoma | Causa raíz | Solución | Prevención |
|---|---|---|---|---|
| 2026-XX-XX | _(plantilla)_ | | | |

---

## Errores conocidos pendientes de documentar
- Tests de integración: se usa InMemory en lugar de SQLite para evitar problemas con `DateTimeOffset` (ver MEMORY del proyecto). Documentar causa raíz exacta cuando se aborde.
- **Test IA FLAKY (pre-existente, no determinista)**: `RadioAficionado.IA.Tests.AnalizadorPropagacionMlNetTests.SfiAlto_BandaAlta_ProbabilidadAlta`. El test exige `ProbabilidadApertura > 0.3` para SFI=200 en banda alta. **No es determinista**: en unas ejecuciones produce ~0.2553 (falla) y en otras supera 0.3 (pasa) — confirmado al pasar 86/86 en una corrida tras fallar en otra. Detectado durante la auditoría 2026-06-12. **No introducido por los fixes de la auditoría** (la IA solo referencia `Compartido` y `Dominio`, ninguno modificado). Causa raíz: la varianza del entrenamiento ML.NET (sin `seed` fijado) cruza el umbral 0.3 según la corrida. Fix propuesto: fijar el `seed` del entrenamiento para determinismo, o relajar el aserto. Registrado también en DEUDA.md.
