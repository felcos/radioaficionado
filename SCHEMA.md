# SCHEMA.md — Estado de la base de datos (Art. 21)

> **Generado por inspección de migraciones y configuraciones Fluent API el 2026-06-12.**
> Debe verificarse contra la BD real antes de escribir SQL. No es output de `dotnet ef dbcontext info`.

## Arquitectura de persistencia

Dos `DbContext` separados (decisión ADR-004):

| Contexto | Proyecto | BD producción | BD local/escritorio | Contenido |
|---|---|---|---|---|
| `ContextoRadioAficionado` | `Infraestructura` (+ `.Postgres` / `.Sqlite`) | PostgreSQL 17 | SQLite | QSOs, Activaciones |
| `ContextoIdentidadRadioAficionado` | `Web/Data` | PostgreSQL 17 | — | Identity (usuarios), Foro, Claves API |

> ✅ **Resuelto (2026-06-13, ADR-008 Opción A aplicada):** en **Postgres** las tablas son `qsos`/`activaciones` con columnas/índices/constraints snake_case (`UseSnakeCaseNamingConvention()` solo en `AgregarPostgres()` + eliminado `ToTable` explícito). En **SQLite** (escritorio) siguen como `Qsos`/`Activaciones` porque no aplica la convención y el nombre deriva del `DbSet` — config Fluent compartida, intacta.
> ⚠️ **Pendiente:** sigue sin existir migración Postgres formal del `ContextoRadioAficionado` (`Web/Program.cs` no llama `Migrate()`); las tablas se crearon manualmente y el rename a snake_case se aplicó por SQL directo (runbook). Las tablas Identity base (`AspNet*`) permanecen en PascalCase (default de Identity, fuera del alcance de este ADR).

---

## Contexto principal — `ContextoRadioAficionado`

### qsos (Postgres) / `Qsos` (SQLite escritorio)
PK + indicativo, frecuencia, modo, fecha/hora inicio (DateTimeOffset, almacenado como string ISO "O" / `text` en ambos proveedores), `activacion_id` (FK nullable → activaciones, `ON DELETE SET NULL`).
- Columnas en snake_case en PG: `indicativo_propio`, `indicativo_contacto`, `fecha_hora_inicio`, `senal_enviada`, etc.
- Índices PG: `ix_qsos_indicativo_propio`, `ix_qsos_indicativo_contacto`, `ix_qsos_fecha_hora_inicio`, `ix_qsos_frecuencia`, `ix_qsos_sincronizado`, `ix_qsos_activacion_id`.

### activaciones (Postgres) / `Activaciones` (SQLite escritorio)
PK + referencia (SOTA/POTA), relación 1:N con qsos (`HasMany(a => a.Qsos)`, `Include` usado para evitar N+1). Columnas snake_case en PG.

---

## Contexto de identidad — `ContextoIdentidadRadioAficionado` (snake_case, PostgreSQL)

### usuarios (ASP.NET Identity extendido)
- `indicativo` — **UNIQUE** (`ix_usuarios_indicativo`)
- `region_itu` — con **CHECK constraint**
- `nivel_licencia`, `nombre`, campos estándar de Identity.

### hilos_foro
- `autor_id` FK → usuarios (`ON DELETE RESTRICT`).
- Índice en `autor_id`: **existe** (creado por convención EF Core; confirmado en el snapshot `HasIndex("AutorId")`). Índices adicionales: `categoria`, `(fijado, fecha_ultima_respuesta)`.

### respuestas_foro
- FK → hilos_foro, `autor_id` FK → usuarios.

### claves_api
- PK UUID, `usuario_id` FK → usuarios (índice `ix_claves_api_usuario_id`).
- `hash_clave` (SHA-256), `salt` (32 bytes), `prefijo` (8 chars para búsqueda), `nombre`, fechas, `activa` (**CHECK**).

---

## Migraciones existentes
- `Infraestructura.Sqlite/Migraciones/20260323100132_Inicial.cs` — tablas `Qsos`, `Activaciones` (PascalCase).
- `Web/Migrations/20260517112250_Inicial.cs` — `usuarios`, `hilos_foro`, `respuestas_foro`, `claves_api` (snake_case).

## Pendiente (ver ADR-008 y DEUDA.md)
1. Decidir el enfoque de naming por proveedor (ADR-008, Opción A recomendada) ANTES de tocar la BD.
2. Confirmar estado real del servidor: ¿existen `Qsos`/`Activaciones` en Postgres y con datos?
3. Crear migración Postgres del `ContextoRadioAficionado` preservando datos (rename, no drop+create).
4. Migrar fechas a `timestamptz` real en Postgres (mantener string ISO solo en SQLite).
5. Considerar transacciones cruzadas entre los dos contextos para operaciones atómicas.

> Nota: el índice en `hilos_foro.autor_id` **ya existe** (no era deuda — falso positivo de la auditoría).
