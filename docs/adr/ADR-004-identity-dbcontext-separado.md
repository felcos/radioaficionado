# ADR-004: IdentityDbContext separado para ASP.NET Identity

**Fecha:** 2026-03-23

## Contexto

La aplicación necesita autenticación de usuarios en la web. ASP.NET Identity requiere que el DbContext herede de `IdentityDbContext<TUser>`. Sin embargo, `ContextoRadioAficionado` es compartido entre la app web (PostgreSQL) y la app de escritorio (Avalonia + SQLite). Modificarlo para heredar de `IdentityDbContext` rompería la app de escritorio, que no necesita ni puede usar Identity.

## Decisión

Crear un `ContextoIdentidadRadioAficionado` separado que:
- Hereda de `IdentityDbContext<UsuarioRadio>`
- Vive en `RadioAficionado.Web/Data/` (exclusivo de la web)
- Usa la misma cadena de conexión PostgreSQL
- Gestiona solo las tablas de Identity + la tabla de usuarios extendida

La entidad `UsuarioRadio` vive en `RadioAficionado.Dominio/Entidades/` usando el paquete ligero `Microsoft.Extensions.Identity.Stores` (no el pesado `Identity.EntityFrameworkCore`).

## Alternativas consideradas

1. **Modificar ContextoRadioAficionado para heredar de IdentityDbContext**: Rompería la app de escritorio que usa SQLite y no necesita Identity.
2. **Crear UsuarioRadio en el proyecto Web**: Violaría Clean Architecture poniendo entidades de dominio en la capa de presentación.
3. **Usar Identity sin Entity Framework (custom stores)**: Mayor complejidad sin beneficio real.

## Consecuencias

- **Positivo:** La app de escritorio no se ve afectada en absoluto.
- **Positivo:** Separación clara de responsabilidades — Identity es exclusivo de la web.
- **Negativo:** Dos DbContext apuntando a la misma BD PostgreSQL (migraciones separadas).
- **Mitigación:** Las tablas de Identity no colisionan con las de QSO/Activaciones.
