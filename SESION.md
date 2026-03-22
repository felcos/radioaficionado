# Sesión — RadioAficionado

## Última sesión: 2026-03-23

### Lo que se hizo

#### ControlWaterfall con SkiaSharp (2026-03-23)
- Creado `Controles/ControlWaterfall.cs`: control Avalonia custom con renderizado SkiaSharp
- Usa `ICustomDrawOperation` + `ISkiaSharpApiLeaseFeature` para acceso directo al `SKCanvas`
- Bitmap interno con scroll vertical (`Buffer.MemoryCopy` unsafe para rendimiento)
- Paleta de 256 colores precalculada: negro → azul → verde → amarillo → rojo
- Thread-safe: `AgregarLinea()` desde cualquier hilo, invalidación en UI thread
- Propiedades: AnchoFft, DbMinimo, DbMaximo
- Reemplazado placeholder en `VentanaPrincipal.axaml` con el control real
- Habilitado `AllowUnsafeBlocks` en csproj para manipulación directa de píxeles
- Build limpio (0 warnings, 0 errores), 78 tests pasando

#### Fase 0 — Cimientos (completada)
- Estructura de solución: 14 proyectos fuente + 5 test
- Objetos de valor: Indicativo, Frecuencia, Localizador, Coordenadas
- BandaRadio (24 bandas), ModoOperacion (48 modos ADIF + 43 submodos)
- Modelo compliance: PlanDeBanda, SegmentoBanda, ResultadoCompliance
- Interfaces: IControlRig, IControlRotador, IAudioPipeline, IDecodificadorDigital
- Entidad Qso + handler MediatR RegistrarQso
- EF Core: ContextoRadioAficionado + proveedores SQLite/PostgreSQL
- Shells: Avalonia (escritorio) + ASP.NET MVC (web)
- 89 tests unitarios dominio

#### Fase 1 — Capa nativa (en progreso)
- ClienteRigctld: cliente TCP a rigctld, polling 500ms, mapeo modos, S-meter, PTT
- ClienteRotctld: cliente TCP a rotctld, polling 1s, AZ/EL
- PipelineAudioNAudio: captura/transmisión con NAudio, pub/sub multi-consumidor
- TransformadaCooleyTukey: FFT managed radix-2 DIT, ventanas Hann/Hamming/Blackman-Harris
- ProcesadorEspectro: audio PCM → LineaEspectro para waterfall
- ViewModels MVVM: PanelRig, PanelMensajes, PanelRegistroQso
- VentanaPrincipal: layout completo (rig bar, waterfall placeholder, mensajes, QSO form)
- 201 tests (161 Dominio + 40 Infraestructura), todos pasando

### Pendiente
- ~~Implementar WaterfallControl con SkiaSharp~~ ✅ Completado
- Implementar decodificador FT8 con ft8_lib (P/Invoke)
- Conectar ViewModels con servicios reales vía DI
- Swap FFT managed → FFTW3 nativa cuando haya binarios
- Fase 2: Logbook + ADIF parser + POTA/SOTA

### Problemas encontrados
- Ninguno crítico. Build limpio, todos los tests pasan.

### Siguiente paso sugerido
- Conectar DI: inyectar ProcesadorEspectro → PipelineAudio → ControlWaterfall para visualización en vivo
- O decodificador FT8 con ft8_lib (P/Invoke)
- O conectar DI completa entre ViewModels ↔ servicios reales
