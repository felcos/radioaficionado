# Changelog — RadioAficionado

## [0.1.0] — 2026-03-22 — Fase 0: Cimientos

### feat: Estructura de solución completa (14 proyectos + 5 test)
- Creada solución con Clean Architecture compartida entre escritorio y web
- Proyectos: Compartido, Dominio, Aplicacion, Infraestructura, Infraestructura.Sqlite, Infraestructura.Postgres, Nativo.Dsp, Nativo.ModosDigitales, Nativo.Audio, Nativo.Rig, Nativo.Rotador, IA, Escritorio, Web
- 5 proyectos de test: Dominio.Tests, Aplicacion.Tests, Infraestructura.Tests, Escritorio.Tests, Web.Tests

### feat: Objetos de valor del dominio
- Indicativo: validación regex, prefijo/sufijo, conversiones implícitas
- Frecuencia: almacenamiento en Hz, factorías, detección de banda
- Localizador: Maidenhead 4/6/8 chars, conversión a coordenadas
- Coordenadas: Haversine, conversión a localizador
- BandaRadio: 24 bandas (2200m → 1.2cm), categorías, exclusividad regional
- ModoOperacion: 48 modos ADIF + 43 submodos (SSB, CW, FT8, DMR, FreeDV, M17, APRS...)
- RegionItu, NivelLicencia, LicenciaOperador

### feat: Modelo de compliance regulatorio
- SegmentoBanda: restricciones de modo, potencia, nivel de licencia
- PlanDeBanda: verificación completa de compliance por región ITU
- ResultadoCompliance: fuera de banda, licencia insuficiente, modo no permitido, potencia excedida
- IServicioCompliance: interfaz del servicio

### feat: Interfaces del dominio
- IControlRig: control de radio vía rigctld (TCP)
- IControlRotador: control de rotador vía rotctld
- IAudioPipeline: pipeline de audio interno (sin cables virtuales)
- IDecodificadorDigital + IRegistroDecodificadores: sistema extensible de modos digitales
- IRepositorioQso, IUnidadDeTrabajo

### feat: Entidad Qso + handler MediatR
- Entidad Qso con factory method Crear, método Completar
- RegistrarQsoComando + RegistrarQsoHandler + RegistrarQsoValidador

### feat: EF Core configurado
- ContextoRadioAficionado con conversiones para value objects
- Proveedor SQLite (escritorio) + PostgreSQL (web)
- RepositorioQso + UnidadDeTrabajo

### feat: Shells de aplicación
- Escritorio: Avalonia UI con DI, ventana principal, tema Fluent oscuro
- Web: ASP.NET MVC estándar

### test: 89 tests unitarios del dominio
- Indicativo, Frecuencia, Localizador, Coordenadas, BandaRadio, ModoOperacion, Qso, PlanDeBanda
