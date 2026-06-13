# DEUDA.md — Deuda técnica de RadioAficionado

> Registro vivo de deuda técnica detectada (Art. 27). Los 🔴 NO van aquí: se resuelven ya y se listan en el plan de acción de la auditoría.

## Origen
Auditoría exhaustiva del 2026-06-12 (6 pistas en paralelo: backend, seguridad, datos, arquitectura, frontend, devops/tests).

---

## 🟠 Alta — resolver pronto

| Fecha | Área | Archivo:línea | Problema | Fix propuesto |
|---|---|---|---|---|
| 2026-06-12 | Datos | `Infraestructura.Postgres` (ausencia) | No existe migración EF formal del `ContextoRadioAficionado`; las tablas se crearon/renombraron por SQL directo y Web no llama `Migrate()` | Generar baseline EF para PG con `dotnet ef migrations add` y registrarla en `__EFMigrationsHistory` (la nomenclatura snake_case ya está aplicada) |
| 2026-06-12 | Datos | `Web/Controllers/ForoController.cs:204,262`, `ServicioApiKeys.cs` | `SaveChangesAsync` fuera de UoW | `IUnidadDeTrabajoIdentidad` |

---

## 🟡 Media — mejorar

| Fecha | Área | Archivo:línea | Problema | Fix propuesto |
|---|---|---|---|---|
| 2026-06-12 | Datos | `Infraestructura/Persistencia/RepositorioQso.cs:32` | `ObtenerTodosAsync` sin paginación | No tocado: cambia el contrato (lo usa exportación ADIF). Evaluar parámetro `top` opcional |
| 2026-06-12 | Datos | `QsoConfiguracion.cs`, `ActivacionConfiguracion.cs` | Fechas como string ISO (`text`) en Postgres degradan búsquedas por rango | `timestamptz` solo en PG (Paso 3 del runbook `postgres-qsos-snakecase.md`, NO ejecutado); requiere `HasConversion` condicionado por proveedor |
| 2026-06-12 | Seguridad | `src/RadioAficionado.Web/appsettings.json:3` | `postgres/postgres` commiteado (default dev) | User Secrets en desarrollo; en prod ya va por `appsettings.Production.json` |
| 2026-06-12 | DevOps | `.github/workflows/ci.yml` | Sin escaneo CVE de dependencias | Dependabot / `dotnet list package --vulnerable` |
| 2026-06-12 | DevOps | `Infraestructura.Postgres/.Sqlite` (transitiva) | `System.Security.Cryptography.Xml 9.0.0` con CVE alta (NU1903) | Actualizar paquete a versión parcheada |
| 2026-06-12 | DevOps | `RadioAficionado.Escritorio` (transitiva) | `Tmds.DBus.Protocol 0.20.0` con CVE alta (NU1903, GHSA-xrw6-gwf8-vvr9) | Actualizar Avalonia/dependencia que la arrastra |
| 2026-06-12 | DevOps | `Dockerfile:7,38` | Base image `preview` sin digest | Fijar `@sha256:...` al pasar a .NET 10 estable |
| 2026-06-12 | DevOps | `Servicio/Program.cs:92` | Health check trivial (no valida BD/DX) | Custom health checks |
| 2026-06-12 | Frontend | `wwwroot/js/*.js` | Textos hardcoded en español (Art. 1) | Diccionario i18n JS |
| 2026-06-12 | Backend | Interfaces en Dominio/Aplicación | Métodos públicos sin `/// <summary>` | Documentar |
| 2026-06-12 | Arquitectura | `Web.csproj` | Web referencia `Nativo.ModosDigitales` | No tocado: `Web/Program.cs:56` llama `AgregarModosDigitales()` (uso activo). Evaluar mover registro a capa de servicio |
| 2026-06-12 | Tests | `IA.Tests/AnalizadorPropagacionMlNetTests.cs:32` | `SfiAlto_BandaAlta_ProbabilidadAlta` **flaky** (no determinista): ~0.2553 unas ejecuciones, >0.3 otras | Pre-existente. La varianza del entrenamiento ML.NET cruza el umbral 0.3. Fijar `seed` del entrenamiento o relajar el aserto (ver ERRORES.md) |

---

## Falsos positivos descartados (de los subagentes de auditoría)

- **`RateLimitingMiddleware.cs:60` — `lock` sin timeout**: el `lock` es correcto. Es breve, sobre un objeto por usuario y sin `await` dentro. Cambiar a `Interlocked` no aporta y complica la lógica de ventana. **No se cambia.**
- **`HubTunelServicio.cs:22` — "campo de instancia"**: la sugerencia del subagente era errónea. Los hubs SignalR se instancian por invocación; un campo de instancia rompería el throttle. Fix correcto aplicado: `ConcurrentDictionary` estático por usuario con limpieza en `OnDisconnectedAsync`.
- **`controlRemoto.js:301` — listeners SignalR sin `.off()` (memory leak)**: falso positivo. `iniciarConexion()` se llama una sola vez (`DOMContentLoaded`); los `.on()` se registran sobre una única conexión y `onclose` solo reinicia la *misma* conexión vía `iniciarConexionHub()` (no re-registra handlers). No hay fuga. **No se cambia.**
- **`ContextoIdentidadRadioAficionado.cs` (HiloForo) — FK `autor_id` sin índice**: falso positivo. EF Core crea el índice del FK por convención; el snapshot lo confirma (`HasIndex("AutorId")` en `hilos_foro` y `respuestas_foro`). El índice **ya existe** en la BD. **No se cambia.**

---

## Resuelto

| Fecha | Área | Qué | Cómo |
|---|---|---|---|
| 2026-06-12 | Seguridad | Password BD commiteado (`docker-compose.yml`) | Movido a `${DB_PASSWORD}` + `.env.example` + `.env` en `.gitignore` |
| 2026-06-12 | Seguridad | CORS `AllowAnyOrigin` en web pública | `WithOrigins(localhost:5200, ham.felcos.es)` |
| 2026-06-12 | Seguridad | `AllowedHosts: "*"` | `ham.felcos.es;localhost;127.0.0.1` |
| 2026-06-12 | Backend | Service Locator en `ControladorTimeoutPtt` | Inyección directa de `IHubContext<>` y `RegistroServiciosConectados` (singletons) |
| 2026-06-12 | Backend | Throttle espectro global entre usuarios (`HubTunelServicio`) | `ConcurrentDictionary<string,long>` por usuario + limpieza en `OnDisconnectedAsync` |
| 2026-06-12 | Seguridad | Rate limit solo `/hubs/` | Extendido también a `/api/` |
| 2026-06-12 | Backend | Excepciones tragadas en `Satelites`/`Propagacion` ApiControllers | Inyectado `ILogger<T>` + `LogWarning/LogDebug(ex, ...)` |
| 2026-06-12 | DevOps | Sin Correlation ID en Serilog (Art. 17) | `Enrich.FromLogContext()` + plantilla con `{CorrelationId}` + middleware en ambos `Program.cs` |
| 2026-06-12 | DevOps | Contenedor Docker como root | `USER $APP_UID` antes del `ENTRYPOINT` |
| 2026-06-12 | Frontend | XSS por `innerHTML` (NOAA/popup mapa) | `textContent`/nodos DOM en `solarDashboard.js` (Web+Servicio) y `mapa-operador.js` |
| 2026-06-12 | Datos | Lecturas sin `AsNoTracking()` | Añadido a `ObtenerTodosAsync`, `BuscarPorIndicativoAsync`, `ObtenerPaginadoAsync` en `RepositorioQso` |
| 2026-06-13 | Datos | `Qsos`/`Activaciones` PascalCase en Postgres (viola Art. 23) | **ADR-008 Opción A aplicada en prod**: `EFCore.NamingConventions` + `UseSnakeCaseNamingConvention()` solo en `AgregarPostgres()` + eliminado `ToTable` explícito (config compartida intacta para SQLite). Rename con datos preservados (runbook ejecutado). Tablas/columnas/índices ahora `qsos`/`activaciones` snake_case |
