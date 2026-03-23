# Estructura — RadioAficionado

## Solucion: 14 proyectos fuente + 5 proyectos de test

### Capas de la arquitectura

#### Compartido (RadioAficionado.Compartido)
- Sin dependencias externas
- Excepciones: ExcepcionDeValidacion, ExcepcionDeNegocio
- Constantes: ConstantesRadio

#### Dominio (RadioAficionado.Dominio) → Compartido
- ObjetosDeValor: Indicativo, Frecuencia, Localizador, Coordenadas, BandaRadio (24 bandas), ModoOperacion (48 modos + 43 submodos), RegionItu, ClaseLicencia, FiltroQso, ReferenciaPota, ReferenciaSota, EstadoActivacion, TipoActivacion
- Entidades: Qso
- Activaciones: Activacion (entidad para POTA/SOTA)
- Compliance: PlanDeBanda, SegmentoBanda, ResultadoCompliance, TipoSegmento, TipoViolacion, PlanDeBandaItu (planes IARU 3 regiones)
- Contests: MotorContest, ReglaContest, ConfiguracionContest, ResultadoContest, TipoContest, TipoIntercambio, MetodoMultiplicador, Intercambio
- Configuracion: ConfiguracionCompleta, ConfiguracionEstacion, ConfiguracionAudio, ConfiguracionGeneral
- Interfaces: IRepositorioQso, IRepositorioActivaciones, IUnidadDeTrabajo, IControlRig, IControlRotador, IAudioPipeline, IDecodificadorDigital, IRegistroDecodificadores, IServicioCompliance, IDxCluster, IPskReporter, IServicioActivaciones, IServicioConfiguracion

#### Aplicacion (RadioAficionado.Aplicacion) → Dominio, Compartido
- Qsos/RegistrarQso: RegistrarQsoComando, RegistrarQsoHandler, RegistrarQsoValidador
- ConfiguracionServicios (MediatR + FluentValidation)

#### Infraestructura (RadioAficionado.Infraestructura) → Dominio, Aplicacion, Compartido
- Persistencia: ContextoRadioAficionado, QsoConfiguracion, RepositorioQso (con paginacion y filtros), UnidadDeTrabajo
- Adif: RegistroAdif, ParserAdif, GeneradorAdif, ConvertidorAdifQso
- DxCluster: ClienteDxCluster (cliente TCP/Telnet)
- Compliance: ServicioCompliance (verificacion IARU 3 regiones)
- Contests: GeneradorCabrillo (formato de logs para contests)
- Activaciones: ServicioActivaciones (gestion POTA/SOTA)
- PskReporter: ClientePskReporter (envio de spots)
- Configuracion: ServicioConfiguracionJson (persistencia JSON)
- ConfiguracionServicios (DI)

#### Infraestructura.Sqlite → Infraestructura
- ConfiguracionSqlite

#### Infraestructura.Postgres → Infraestructura
- ConfiguracionPostgres

#### Nativo.Dsp → Compartido
- Interfaces/ITransformadaFourier: contrato para FFT (intercambiable managed ↔ FFTW3)
- TransformadaCooleyTukey: FFT radix-2 DIT managed con twiddle factors y ventana Hann pre-computados
- ProcesadorEspectro: convierte PCM 16-bit → LineaEspectro (waterfall data)
- VentanasDsp: funciones de ventana estaticas (Hann, Hamming, Blackman-Harris)
- LineaEspectro: modelo de datos de espectro (magnitudes dB, resolucion Hz, rango)

#### Nativo.ModosDigitales → Compartido, Dominio
- (pendiente: implementaciones de IDecodificadorDigital — FT8, CW, etc.)

#### Nativo.Audio → Compartido, Dominio
- PipelineAudioNAudio: captura/transmision con NAudio WaveInEvent/WaveOutEvent
- Pipeline pub/sub para multiples consumidores simultaneos
- Enumeracion de dispositivos de entrada/salida

#### Nativo.Rig → Compartido, Dominio
- ClienteRigctld: cliente TCP a rigctld (Hamlib), polling 500ms
- MapeadorModos: conversion bidireccional rigctld ↔ ModoOperacion/SubModoOperacion
- ConfiguracionRig: host, puerto, intervalo, potencia maxima

#### Nativo.Rotador → Compartido, Dominio
- ClienteRotctld: cliente TCP a rotctld, polling 1s, AZ/EL
- ConfiguracionRotador: host, puerto, intervalo, umbral de cambio

#### IA → Compartido, Dominio
- (pendiente: ML.NET + ONNX)

#### Escritorio (RadioAficionado.Escritorio) → todos los proyectos
- Avalonia UI, MVVM con CommunityToolkit.Mvvm, DI
- ViewModels: ViewModelBase, VentanaPrincipalViewModel, PanelRigViewModel (polling real), PanelMensajesViewModel, PanelRegistroQsoViewModel (MediatR), PanelLogbookViewModel (DataGrid paginado, filtros, import/export ADIF), PanelDxClusterViewModel (spots en tiempo real, filtros)
- Controles: ControlWaterfall (SkiaSharp, ICustomDrawOperation, SKBitmap con scroll vertical)
- Vistas: VentanaPrincipal (layout completo: rig bar, waterfall SkiaSharp, mensajes, QSO form), PanelLogbook (DataGrid paginado), PanelDxCluster (DataGrid de spots)

#### Web (RadioAficionado.Web) → Dominio, Aplicacion, Infraestructura, Infraestructura.Postgres, Compartido
- ASP.NET MVC con Razor Views

### Tests (321 tests, todos pasando)
- Dominio.Tests (161): Indicativo, Frecuencia, Localizador, Coordenadas, BandaRadio, ModoOperacion, Qso, PlanDeBanda, MapeadorModos, ReferenciaPota, MotorContest
- Infraestructura.Tests (131): TransformadaCooleyTukey, ProcesadorEspectro, VentanasDsp, ParserAdif, GeneradorAdif, ConvertidorAdifQso, ServicioCompliance, ClienteDxCluster, ClientePskReporter, GeneradorCabrillo
- Aplicacion.Tests (29): RegistrarQsoHandler, RegistrarQsoValidador

### Features

- ✅ Estructura de solucion completa (14+5 proyectos)
- ✅ Objetos de valor del dominio (24 bandas, 48+43 modos ADIF)
- ✅ Modelo de compliance regulatorio (PlanDeBandaItu, IARU 3 regiones)
- ✅ ServicioCompliance con verificacion de frecuencia/modo
- ✅ Interfaces de dominio completas (12 interfaces)
- ✅ Entidad Qso + handler MediatR
- ✅ EF Core con SQLite + PostgreSQL
- ✅ ClienteRigctld (control de radio via TCP)
- ✅ ClienteRotctld (control de rotador via TCP)
- ✅ PipelineAudioNAudio (captura/transmision)
- ✅ FFT managed Cooley-Tukey + ProcesadorEspectro
- ✅ UI escritorio MVVM (rig bar, waterfall, mensajes, QSO form)
- ✅ WaterfallControl con SkiaSharp
- ✅ ViewModels conectados a DI real (PanelRig polling, PanelRegistroQso con MediatR)
- ✅ ADIF parser/generador completo (RegistroAdif, ParserAdif, GeneradorAdif, ConvertidorAdifQso)
- ✅ Logbook UI (PanelLogbook con DataGrid paginado, filtros, import/export ADIF)
- ✅ DX Cluster (IDxCluster, ClienteDxCluster, PanelDxCluster)
- ✅ IRepositorioQso ampliado con paginacion y filtros (FiltroQso)
- ✅ Motor de Contests (MotorContest, ReglaContest, ConfiguracionContest)
- ✅ GeneradorCabrillo (formato de logs para contests)
- ✅ Activaciones POTA/SOTA (Activacion, ReferenciaPota, ReferenciaSota, ServicioActivaciones)
- ✅ PSK Reporter (IPskReporter, ClientePskReporter)
- ✅ Configuracion persistente JSON (ConfiguracionCompleta, ServicioConfiguracionJson)
- ✅ 321 tests unitarios (161 + 131 + 29)
- 🔨 Decodificador FT8 (ft8_lib P/Invoke)
- 🔨 Swap FFT managed → FFTW3 nativa
- 📋 Fase 3: Web con cuentas + logbook online
- 📋 Fase 4: LoTW, eQSL, ClubLog, APRS, Satelites
- 📋 Fase 5: SDR (SoapySDR) + mas modos digitales
- 📋 Fase 6: IA + Mobile
