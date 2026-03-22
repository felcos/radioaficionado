# Estructura — RadioAficionado

## Solución: 14 proyectos fuente + 5 proyectos de test

### Capas de la arquitectura

#### Compartido (RadioAficionado.Compartido)
- Sin dependencias externas
- Excepciones: ExcepcionDeValidacion, ExcepcionDeNegocio
- Constantes: ConstantesRadio

#### Dominio (RadioAficionado.Dominio) → Compartido
- ObjetosDeValor: Indicativo, Frecuencia, Localizador, Coordenadas, BandaRadio (24 bandas), ModoOperacion (48 modos + 43 submodos), RegionItu, NivelLicencia, LicenciaOperador
- Entidades: Qso
- Compliance: PlanDeBanda, SegmentoBanda, ResultadoCompliance, TipoSegmento, TipoViolacion
- Interfaces: IRepositorioQso, IUnidadDeTrabajo, IControlRig, IControlRotador, IAudioPipeline, IDecodificadorDigital, IRegistroDecodificadores, IServicioCompliance

#### Aplicacion (RadioAficionado.Aplicacion) → Dominio, Compartido
- Qsos/RegistrarQso: RegistrarQsoComando, RegistrarQsoHandler, RegistrarQsoValidador
- ConfiguracionServicios (MediatR + FluentValidation)

#### Infraestructura (RadioAficionado.Infraestructura) → Dominio, Aplicacion, Compartido
- Persistencia: ContextoRadioAficionado, QsoConfiguracion, RepositorioQso, UnidadDeTrabajo
- ConfiguracionServicios (DI)

#### Infraestructura.Sqlite → Infraestructura
- ConfiguracionSqlite

#### Infraestructura.Postgres → Infraestructura
- ConfiguracionPostgres

#### Nativo.Dsp → Compartido
- (pendiente: P/Invoke a FFTW3)

#### Nativo.ModosDigitales → Compartido, Dominio
- (pendiente: implementaciones de IDecodificadorDigital)

#### Nativo.Audio → Compartido
- (pendiente: implementación de IAudioPipeline con NAudio)

#### Nativo.Rig → Compartido, Dominio
- (pendiente: implementación de IControlRig)

#### Nativo.Rotador → Compartido, Dominio
- (pendiente: implementación de IControlRotador)

#### IA → Compartido, Dominio
- (pendiente: ML.NET + ONNX)

#### Escritorio (RadioAficionado.Escritorio) → todos los proyectos
- Avalonia UI, MVVM, DI
- Vistas: VentanaPrincipal

#### Web (RadioAficionado.Web) → Dominio, Aplicacion, Infraestructura, Infraestructura.Postgres, Compartido
- ASP.NET MVC con Razor Views

### Features

- ✅ Estructura de solución completa
- ✅ Objetos de valor del dominio
- ✅ Modelo de compliance regulatorio
- ✅ Interfaces de dominio completas
- ✅ Entidad Qso + handler MediatR
- ✅ EF Core con SQLite + PostgreSQL
- ✅ Shell Avalonia + Shell Web
- ✅ 89 tests unitarios
- 🔨 Fase 1: Control de rig (rigctld)
- 📋 Fase 1: Waterfall (FFTW3 + SkiaSharp)
- 📋 Fase 1: Decodificador FT8 (ft8_lib)
- 📋 Fase 2: Logbook + ADIF parser
- 📋 Fase 2: POTA/SOTA
- 📋 Fase 3: Web con cuentas + logbook online
- 📋 Fase 4: DX Cluster, LoTW, eQSL, ClubLog, APRS, Satélites
- 📋 Fase 5: SDR (SoapySDR) + más modos digitales
- 📋 Fase 6: IA + Contests + Mobile
