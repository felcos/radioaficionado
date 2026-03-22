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
- Interfaces/ITransformadaFourier: contrato para FFT (intercambiable managed ↔ FFTW3)
- TransformadaCooleyTukey: FFT radix-2 DIT managed con twiddle factors y ventana Hann pre-computados
- ProcesadorEspectro: convierte PCM 16-bit → LineaEspectro (waterfall data)
- VentanasDsp: funciones de ventana estáticas (Hann, Hamming, Blackman-Harris)
- LineaEspectro: modelo de datos de espectro (magnitudes dB, resolución Hz, rango)

#### Nativo.ModosDigitales → Compartido, Dominio
- (pendiente: implementaciones de IDecodificadorDigital — FT8, CW, etc.)

#### Nativo.Audio → Compartido, Dominio
- PipelineAudioNAudio: captura/transmisión con NAudio WaveInEvent/WaveOutEvent
- Pipeline pub/sub para múltiples consumidores simultáneos
- Enumeración de dispositivos de entrada/salida

#### Nativo.Rig → Compartido, Dominio
- ClienteRigctld: cliente TCP a rigctld (Hamlib), polling 500ms
- MapeadorModos: conversión bidireccional rigctld ↔ ModoOperacion/SubModoOperacion
- ConfiguracionRig: host, puerto, intervalo, potencia máxima

#### Nativo.Rotador → Compartido, Dominio
- ClienteRotctld: cliente TCP a rotctld, polling 1s, AZ/EL
- ConfiguracionRotador: host, puerto, intervalo, umbral de cambio

#### IA → Compartido, Dominio
- (pendiente: ML.NET + ONNX)

#### Escritorio (RadioAficionado.Escritorio) → todos los proyectos
- Avalonia UI, MVVM con CommunityToolkit.Mvvm, DI
- ViewModels: ViewModelBase, VentanaPrincipalViewModel, PanelRigViewModel, PanelMensajesViewModel, PanelRegistroQsoViewModel, MensajeDigitalVm, QsoRecienteVm
- Vistas: VentanaPrincipal (layout completo: rig bar, waterfall, mensajes, QSO form)

#### Web (RadioAficionado.Web) → Dominio, Aplicacion, Infraestructura, Infraestructura.Postgres, Compartido
- ASP.NET MVC con Razor Views

### Tests (201 tests, todos pasando)
- Dominio.Tests (161): Indicativo, Frecuencia, Localizador, Coordenadas, BandaRadio, ModoOperacion, Qso, PlanDeBanda, MapeadorModos
- Infraestructura.Tests (40): TransformadaCooleyTukey, ProcesadorEspectro, VentanasDsp

### Features

- ✅ Estructura de solución completa (14+5 proyectos)
- ✅ Objetos de valor del dominio (24 bandas, 48+43 modos ADIF)
- ✅ Modelo de compliance regulatorio
- ✅ Interfaces de dominio completas
- ✅ Entidad Qso + handler MediatR
- ✅ EF Core con SQLite + PostgreSQL
- ✅ ClienteRigctld (control de radio vía TCP)
- ✅ ClienteRotctld (control de rotador vía TCP)
- ✅ PipelineAudioNAudio (captura/transmisión)
- ✅ FFT managed Cooley-Tukey + ProcesadorEspectro
- ✅ UI escritorio MVVM (rig bar, waterfall placeholder, mensajes, QSO form)
- ✅ 201 tests unitarios
- 🔨 WaterfallControl con SkiaSharp
- 🔨 Decodificador FT8 (ft8_lib P/Invoke)
- 🔨 Conectar DI: ViewModels ↔ servicios reales
- 📋 Fase 2: Logbook completo + ADIF parser/generador
- 📋 Fase 2: POTA/SOTA integrado
- 📋 Fase 3: Web con cuentas + logbook online
- 📋 Fase 4: DX Cluster, LoTW, eQSL, ClubLog, APRS, Satélites
- 📋 Fase 5: SDR (SoapySDR) + más modos digitales
- 📋 Fase 6: IA + Contests + Mobile
