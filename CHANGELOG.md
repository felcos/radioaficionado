# Changelog — RadioAficionado

## [0.4.0] — 2026-03-22 — Tests capa nativa + UI escritorio

### test: 201 tests (161 Dominio + 40 Infraestructura)
- TransformadaCooleyTukeyTests: 10 tests (seno puro, silencio, tamaños, dispose)
- ProcesadorEspectroTests: 10 tests (PCM, bloques, solapamiento, validación)
- VentanasDspTests: 10 tests (Hann, Hamming, Blackman-Harris extremos/centro/simetría)
- MapeadorModosTests: 30 tests (29 modos rigctld, S-meter dBm→S, VFO)

### feat: UI escritorio completa
- ViewModels MVVM: VentanaPrincipal, PanelRig, PanelMensajes, PanelRegistroQso
- Layout: barra rig (frecuencia, modo, S-meter, PTT), waterfall placeholder, mensajes digitales, formulario QSO
- Tema oscuro personalizado, compiled bindings con x:DataType

## [0.3.0] — 2026-03-22 — Capa nativa: Rig, DSP, Audio, Rotador

### feat: ClienteRigctld (Nativo.Rig)
- Cliente TCP a rigctld con polling 500ms, SemaphoreSlim thread-safe
- MapeadorModos: conversión bidireccional rigctld ↔ ModoOperacion/SubModoOperacion
- S-meter: conversión dBm → unidades S
- ConfiguracionRig: host, puerto, intervalo, potencia máxima

### feat: TransformadaCooleyTukey (Nativo.Dsp)
- FFT managed radix-2 DIT con twiddle factors pre-computados
- ITransformadaFourier: interfaz para swap futuro a FFTW3 nativa
- ProcesadorEspectro: PCM 16-bit → LineaEspectro (magnitudes dB, resolución)
- VentanasDsp: Hann, Hamming, Blackman-Harris

### feat: PipelineAudioNAudio (Nativo.Audio)
- Captura/transmisión con NAudio WaveInEvent/WaveOutEvent
- Pipeline pub/sub para múltiples consumidores simultáneos
- Enumeración de dispositivos de entrada/salida

### feat: ClienteRotctld (Nativo.Rotador)
- Cliente TCP a rotctld con polling 1s
- Soporte AZ/EL, detección de cambio por umbral

## [0.1.0] — 2026-03-22 — Fase 0: Cimientos

### feat: Estructura de solución completa (14 proyectos + 5 test)
- Clean Architecture compartida entre escritorio y web
- Proyectos: Compartido, Dominio, Aplicacion, Infraestructura (+Sqlite, +Postgres), Nativo.Dsp, Nativo.ModosDigitales, Nativo.Audio, Nativo.Rig, Nativo.Rotador, IA, Escritorio, Web

### feat: Objetos de valor del dominio
- Indicativo, Frecuencia, Localizador, Coordenadas
- BandaRadio (24 bandas 2200m→1.2cm), ModoOperacion (48 modos ADIF + 43 submodos)
- RegionItu, NivelLicencia, LicenciaOperador

### feat: Modelo de compliance regulatorio
- PlanDeBanda, SegmentoBanda, ResultadoCompliance, IServicioCompliance

### feat: Interfaces del dominio
- IControlRig, IControlRotador, IAudioPipeline, IDecodificadorDigital, IRegistroDecodificadores
- IRepositorioQso, IUnidadDeTrabajo

### feat: Entidad Qso + MediatR + EF Core
- Qso.Crear/Completar + RegistrarQsoComando/Handler/Validador
- ContextoRadioAficionado + RepositorioQso + UnidadDeTrabajo
- Proveedores SQLite (escritorio) + PostgreSQL (web)

### test: 89 tests unitarios del dominio
