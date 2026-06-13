# ADR-008: Nomenclatura de BD en el contexto compartido (snake_case vs config compartida SQLite/Postgres)

**Fecha:** 2026-06-12 (aplicado 2026-06-13)
**Estado:** ACEPTADO Y APLICADO — Opción A en producción (`ham.felcos.es`) el 2026-06-13.

> **Resolución (2026-06-13):** decisión del usuario de eliminar PascalCase. Aplicada la **Opción A** con un matiz necesario: además de `UseSnakeCaseNamingConvention()` en `AgregarPostgres()`, se **eliminó el `ToTable("Qsos")`/`ToTable("Activaciones")`** de la config compartida, porque la convención NO reescribe nombres de tabla puestos explícitamente (sí columnas/índices/constraints). Sin `ToTable`, el nombre se deriva del `DbSet`: SQLite lo deja en `Qsos`/`Activaciones` (escritorio intacto) y Postgres lo mapea a `qsos`/`activaciones`. Migración aplicada con rename preservando datos (tablas vacías, 0 filas). Runbook ejecutado: `docs/migraciones/postgres-qsos-snakecase.md`.

## Contexto

La auditoría detectó que `ContextoRadioAficionado` (tablas `Qsos` y `Activaciones`) usa **PascalCase**, lo que viola la convención del proyecto para PostgreSQL (Art. 23: snake_case en PG). El primer impulso fue renombrar `ToTable("Qsos")` → `ToTable("qsos")` y columnas a snake_case.

Al investigar la raíz (Art. 11) aparece un **blocker arquitectónico**:

- `QsoConfiguracion` y `ActivacionConfiguracion` (en `RadioAficionado.Infraestructura/Persistencia/Configuraciones/`) son **compartidas** por dos proveedores:
  - **SQLite** en `RadioAficionado.Servicio` (BD local en el PC del usuario, ya desplegada en cada máquina).
  - **PostgreSQL** en `RadioAficionado.Web` vía `AgregarPostgres()` → `AddDbContext<ContextoRadioAficionado>(UseNpgsql(...))`.
- Renombrar las tablas/columnas a snake_case en la config compartida **también renombra las tablas de las BD SQLite de escritorio** ya existentes, rompiéndolas (Art. 5).
- Las fechas se guardan como **string ISO 8601** (`v.ToString("O")`) "para compatibilidad con SQLite ORDER BY". En Postgres esto degrada las búsquedas por rango de fecha (no usa `timestamptz`).

Hallazgos adicionales relacionados:

1. **No existe migración Postgres** para `ContextoRadioAficionado`. Las únicas migraciones (`20260517112250_Inicial`) son del `ContextoIdentidadRadioAficionado` y viven en `src/RadioAficionado.Web/Migrations/`. `RadioAficionado.Web/Program.cs` **no llama** `Migrate()` ni `EnsureCreated()` para el contexto de QSOs. Hay que confirmar cómo se crean (o si se crean) las tablas `Qsos`/`Activaciones` en el servidor.
2. El `MigrationsAssembly` configurado para Identity es `"RadioAficionado.Infraestructura.Postgres"`, pero las migraciones de Identity están físicamente en el proyecto `Web`. Mismatch a verificar.
3. **Falso positivo de la auditoría:** "FK `autor_id` sin índice". EF Core crea el índice del FK por convención; el snapshot lo confirma (`HasIndex("AutorId")` en `hilos_foro` y `respuestas_foro`). El índice **ya existe** en la BD. No requiere cambio.

## Decisión propuesta

**No** aplicar el renombrado a snake_case modificando la config compartida. En su lugar, **separar la nomenclatura por proveedor**:

### Opción A (recomendada) — Convención de nombres solo en Postgres

Añadir el paquete `EFCore.NamingConventions` y aplicar `optionsBuilder.UseSnakeCaseNamingConvention()` **únicamente** en `AgregarPostgres()`. SQLite mantiene PascalCase; Postgres mapea a snake_case automáticamente sin tocar las configs compartidas.

- (+) No rompe las BD SQLite de escritorio.
- (+) Cumple Art. 23 en Postgres sin duplicar configuraciones.
- (-) Requiere una migración de datos en el servidor (renombrar `Qsos`→`qsos`, columnas) si ya hay datos productivos.
- (-) Nueva dependencia NuGet (verificar CVE, Art. 22).

### Opción B — Configuración específica por proveedor

`ToTable`/`HasColumnName` condicionados al proveedor (`Database.IsNpgsql()`), o una clase de configuración separada para Postgres.

- (+) Sin dependencias nuevas.
- (-) Duplica/complica las configuraciones; más propenso a divergencia.

### Opción C — No cambiar

Aceptar PascalCase en Postgres como excepción documentada porque la config es compartida.

- (+) Cero riesgo.
- (-) Incumple Art. 23; las consultas SQL manuales en PG deben recordar el casing con comillas.

> **Runbook preparado:** los pasos concretos de la Opción A (cambio de código + SQL de rename preservando datos + checklist) están en `docs/migraciones/postgres-qsos-snakecase.md`. Estado: PREPARADO, nada aplicado.

## Acciones requeridas ANTES de aplicar (revisión del usuario)

1. **Confirmar el estado real del servidor**: ¿existen ya las tablas `Qsos`/`Activaciones` en la BD Postgres de producción y tienen datos? (`\dt` en psql sobre `radioaficionado_db`).
2. Decidir entre Opción A / B / C.
3. Si A o B: generar la migración Postgres con renombrado **preservando datos** (`RenameTable`/`RenameColumn`, no `Drop`+`Create`) y ensayarla en una copia de la BD antes del servidor.
4. Migrar las fechas a `timestamptz` real en Postgres (deja el string ISO solo para SQLite) — mejora rango/orden.

## Consecuencias

- Hasta decidir, las tablas de QSOs/Activaciones en Postgres siguen en PascalCase (deuda registrada en DEUDA.md).
- El índice `autor_id` NO necesita acción (ya existe).
- Ninguna BD (escritorio ni servidor) se toca sin la aprobación explícita del usuario (Art. 5 + "preparar y revisar").
