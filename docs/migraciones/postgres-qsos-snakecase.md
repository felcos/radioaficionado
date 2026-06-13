# Runbook — Migración Postgres a snake_case del `ContextoRadioAficionado` (Opción A de ADR-008)

> **Estado:** ✅ EJECUTADO en producción (`ham.felcos.es`) el 2026-06-13. Tablas estaban vacías (0 filas); rename aplicado en transacción tras `pg_dump`. Verificado web 200 + EF snake_case sin errores.
> **Matiz aplicado:** además del paso 1, se eliminó `ToTable("Qsos")`/`ToTable("Activaciones")` de la config compartida (la convención no reescribe nombres de tabla explícitos). Ver ADR-008.
> **Fecha:** 2026-06-12 (ejecutado 2026-06-13) · **Origen:** auditoría 2026-06-12 → decisión del usuario de eliminar PascalCase.
> **Alcance:** SOLO PostgreSQL (web). SQLite de escritorio **no se toca** (la convención se aplica únicamente en `AgregarPostgres()`).

Este runbook implementa la **Opción A** de `docs/adr/ADR-008-nomenclatura-bd-contexto-compartido.md`: añadir `EFCore.NamingConventions` y aplicar `UseSnakeCaseNamingConvention()` solo en el proveedor Postgres, de modo que SQLite mantiene PascalCase y Postgres mapea a snake_case sin duplicar las configuraciones Fluent compartidas.

---

## ⚠️ Por qué NO está aplicado todavía

1. **El cambio de código rompe el deploy si va solo.** Si se mergea `UseSnakeCaseNamingConvention()` a `main` y se despliega, EF empezará a consultar `qsos`/`activaciones` mientras el servidor sigue con `Qsos`/`Activaciones` → errores `relation "qsos" does not exist`. El **cambio de código y el rename SQL deben aplicarse juntos**, en ventana coordinada.
2. **No existe migración Postgres de baseline** para este contexto (las únicas migraciones PascalCase están en `Infraestructura.Sqlite`). EF no puede generar un `RenameTable` automático sin baseline; generaría un `CreateTable`. Por eso el rename se hace con **SQL manual idempotente** (abajo), no con `dotnet ef migrations add`.
3. **Estado del servidor sin confirmar** (paso 0). Hay que saber si las tablas existen y si tienen datos antes de elegir entre *rename* (hay datos) o *recreación limpia* (no hay datos / no existen).

---

## Paso 0 — Confirmar estado del servidor (OBLIGATORIO antes de nada)

Conectar a la BD de producción (ver `DESPLIEGUE.md`) y ejecutar:

```sql
-- ¿Existen las tablas y con qué casing?
\dt

-- ¿Cuántos datos hay? (decide rename vs recreación)
SELECT count(*) FROM "Qsos";
SELECT count(*) FROM "Activaciones";
```

Resultados posibles:

| Resultado | Acción |
|---|---|
| `Qsos`/`Activaciones` existen **con datos** | Aplicar **Vía 1 (rename, preserva datos)**. |
| Existen **vacías** | Vía 1 o simplemente borrarlas y dejar que se recreen. Vía 1 es igual de segura. |
| **No existen** | No hay rename posible. Aplicar el cambio de código + crear tablas snake_case (`EnsureCreated`/migración inicial). Confirmar primero **cómo se crean hoy** (Web no llama `Migrate()` — ver ADR-008 hallazgo 1). |

> No continuar sin haber rellenado esta tabla con el resultado real.

---

## Paso 1 — Cambio de código (Opción A)

### 1.1 Paquete NuGet

En `src/RadioAficionado.Infraestructura.Postgres/RadioAficionado.Infraestructura.Postgres.csproj`, añadir al `ItemGroup` de paquetes:

```xml
<PackageReference Include="EFCore.NamingConventions" Version="10.0.0" />
```

> Verificar la versión compatible con EF Core 10 y revisar CVEs antes de fijarla (Art. 22). `EFCore.NamingConventions` es un paquete maduro de la comunidad EF (autor: Shay Rojansky, mantenedor de Npgsql).

### 1.2 Aplicar la convención SOLO en Postgres

En `src/RadioAficionado.Infraestructura.Postgres/ConfiguracionPostgres.cs`, dentro de `AddDbContext`:

```csharp
servicios.AddDbContext<ContextoRadioAficionado>(opciones =>
{
    opciones.UseNpgsql(cadenaDeConexion);
    opciones.UseSnakeCaseNamingConvention();   // <-- NUEVO: solo afecta a Postgres
});
```

> SQLite (`RadioAficionado.Servicio`) **no** añade esta línea → conserva PascalCase. Las configs Fluent compartidas no se tocan.

### 1.3 Verificación local antes de tocar el servidor

```bash
dotnet build RadioAficionado.slnx
dotnet test
```

Ensayar el mapeo contra una **copia** de la BD (nunca producción) para confirmar que EF lee/escribe en snake_case correctamente.

---

## Paso 2 — Vía 1: SQL de rename (preserva datos) — Postgres

> Ejecutar **dentro de una transacción** y **sobre una copia primero**. Idempotente con `IF EXISTS` donde aplica.
> El orden importa por la FK: renombrar columnas/índices y al final las tablas.

```sql
BEGIN;

-- ============ Tabla Activaciones → activaciones ============
ALTER TABLE "Activaciones" RENAME COLUMN "Id"                TO id;
ALTER TABLE "Activaciones" RENAME COLUMN "TipoActivacion"    TO tipo_activacion;
ALTER TABLE "Activaciones" RENAME COLUMN "Referencia"        TO referencia;
ALTER TABLE "Activaciones" RENAME COLUMN "IndicativoActivador" TO indicativo_activador;
ALTER TABLE "Activaciones" RENAME COLUMN "FechaInicio"       TO fecha_inicio;
ALTER TABLE "Activaciones" RENAME COLUMN "FechaFin"          TO fecha_fin;
ALTER TABLE "Activaciones" RENAME COLUMN "Localizador"       TO localizador;
ALTER TABLE "Activaciones" RENAME COLUMN "Notas"             TO notas;
ALTER TABLE "Activaciones" RENAME COLUMN "EstadoActivacion"  TO estado_activacion;
ALTER TABLE "Activaciones" RENAME COLUMN "FechaCreacion"     TO fecha_creacion;
ALTER TABLE "Activaciones" RENAME COLUMN "FechaModificacion" TO fecha_modificacion;

ALTER INDEX "IX_Activaciones_EstadoActivacion"   RENAME TO ix_activaciones_estado_activacion;
ALTER INDEX "IX_Activaciones_FechaInicio"        RENAME TO ix_activaciones_fecha_inicio;
ALTER INDEX "IX_Activaciones_IndicativoActivador" RENAME TO ix_activaciones_indicativo_activador;
ALTER INDEX "IX_Activaciones_Referencia"         RENAME TO ix_activaciones_referencia;
ALTER INDEX "IX_Activaciones_TipoActivacion"     RENAME TO ix_activaciones_tipo_activacion;

ALTER TABLE "Activaciones" RENAME CONSTRAINT "PK_Activaciones" TO pk_activaciones;

-- ============ Tabla Qsos → qsos ============
ALTER TABLE "Qsos" RENAME COLUMN "Id"                 TO id;
ALTER TABLE "Qsos" RENAME COLUMN "IndicativoPropio"   TO indicativo_propio;
ALTER TABLE "Qsos" RENAME COLUMN "IndicativoContacto" TO indicativo_contacto;
ALTER TABLE "Qsos" RENAME COLUMN "FechaHoraInicio"    TO fecha_hora_inicio;
ALTER TABLE "Qsos" RENAME COLUMN "FechaHoraFin"       TO fecha_hora_fin;
ALTER TABLE "Qsos" RENAME COLUMN "Frecuencia"         TO frecuencia;
ALTER TABLE "Qsos" RENAME COLUMN "Modo"               TO modo;
ALTER TABLE "Qsos" RENAME COLUMN "SenalEnviada"       TO senal_enviada;
ALTER TABLE "Qsos" RENAME COLUMN "SenalRecibida"      TO senal_recibida;
ALTER TABLE "Qsos" RENAME COLUMN "Potencia"           TO potencia;
ALTER TABLE "Qsos" RENAME COLUMN "LocalizadorContacto" TO localizador_contacto;
ALTER TABLE "Qsos" RENAME COLUMN "Notas"              TO notas;
ALTER TABLE "Qsos" RENAME COLUMN "FechaCreacion"      TO fecha_creacion;
ALTER TABLE "Qsos" RENAME COLUMN "FechaModificacion"  TO fecha_modificacion;
ALTER TABLE "Qsos" RENAME COLUMN "ActivacionId"       TO activacion_id;
ALTER TABLE "Qsos" RENAME COLUMN "Sincronizado"       TO sincronizado;

ALTER INDEX "IX_Qsos_ActivacionId"        RENAME TO ix_qsos_activacion_id;
ALTER INDEX "IX_Qsos_FechaHoraInicio"     RENAME TO ix_qsos_fecha_hora_inicio;
ALTER INDEX "IX_Qsos_Frecuencia"          RENAME TO ix_qsos_frecuencia;
ALTER INDEX "IX_Qsos_IndicativoContacto"  RENAME TO ix_qsos_indicativo_contacto;
ALTER INDEX "IX_Qsos_IndicativoPropio"    RENAME TO ix_qsos_indicativo_propio;
ALTER INDEX "IX_Qsos_Sincronizado"        RENAME TO ix_qsos_sincronizado;

ALTER TABLE "Qsos" RENAME CONSTRAINT "PK_Qsos" TO pk_qsos;
ALTER TABLE "Qsos" RENAME CONSTRAINT "FK_Qsos_Activaciones_ActivacionId" TO fk_qsos_activaciones_activacion_id;

-- ============ Renombrar las tablas al final ============
ALTER TABLE "Activaciones" RENAME TO activaciones;
ALTER TABLE "Qsos"         RENAME TO qsos;

-- Revisar con \dt y SELECTs antes de confirmar:
-- COMMIT;   -- descomentar tras verificar
-- ROLLBACK; -- si algo no cuadra
```

> Los nombres exactos de índices/constraints que `EFCore.NamingConventions` espera deben **confirmarse** generando el snapshot con la convención activada (paso 1 + `dotnet ef migrations add VerificacionSnakeCase` en una rama de prueba y leer el `.Designer`/snapshot). Ajustar el SQL si difiere algún nombre. **No** mergear esa migración de prueba.

---

## Paso 3 (opcional, recomendado) — Fechas a `timestamptz` en Postgres

Hoy las fechas se guardan como **string ISO 8601** (`v.ToString("O")`) por compatibilidad con `ORDER BY` en SQLite. En Postgres degrada las búsquedas por rango. Tras el rename, convertir las columnas a `timestamptz` **solo en Postgres** (requiere config por proveedor para la conversión EF — ver Opción B parcial en ADR-008, o `HasConversion` condicionado a `Database.IsNpgsql()`):

```sql
-- Ejemplo para una columna; repetir por cada fecha. Ensayar en copia.
ALTER TABLE qsos
  ALTER COLUMN fecha_hora_inicio TYPE timestamptz
  USING fecha_hora_inicio::timestamptz;
```

> Esto exige también dejar de aplicar el `HasConversion` a string en el proveedor Postgres. Es un cambio mayor: tratarlo como fase aparte y con sus propios tests. Registrado en DEUDA.md.

---

## Rollback

- **Antes de `COMMIT`:** `ROLLBACK;` deshace todo (Vía 1 va en transacción).
- **Tras `COMMIT`:** el rename inverso (snake_case → PascalCase) es simétrico; conservar este runbook para reconstruirlo. Mejor: **backup completo** (`pg_dump`) antes de la ventana.
- **Código:** revertir el commit con la línea `UseSnakeCaseNamingConvention()` y el paquete.

---

## Checklist de aplicación (cuando se apruebe)

- [ ] Paso 0 ejecutado y tabla de estado rellenada con datos reales del servidor.
- [ ] `pg_dump` de respaldo realizado.
- [ ] Cambio de código (1.1 + 1.2) en rama, build + tests verdes.
- [ ] Nombres de índices/constraints confirmados contra el snapshot con la convención activada.
- [ ] SQL de Vía 1 ensayado en **copia** de la BD; lectura/escritura OK desde la app apuntando a la copia.
- [ ] Ventana coordinada: desplegar código y aplicar SQL **juntos**.
- [ ] Verificación post-deploy (`\dt`, alta/consulta de un QSO de prueba).
- [ ] SCHEMA.md actualizado (tablas en snake_case) y entrada en CHANGELOG.md.
- [ ] ADR-008 pasado a estado "Aceptado".
