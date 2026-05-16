using System.IO.Ports;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.Rig;
using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Servicios;

/// <summary>
/// Servicio singleton que gestiona el estado global de operacion:
/// conexion al rig, polling de estado, captura de audio y waterfall.
/// Extrae la logica de PanelRigViewModel para uso en SignalR.
/// </summary>
public sealed class ServicioEstadoOperacion : IAsyncDisposable
{
    private readonly IAudioPipeline _audioPipeline;
    private readonly IServicioWaterfall _servicioWaterfall;
    private readonly ILogger<ServicioEstadoOperacion> _logger;

    private IControlRig? _controlRig;
    private PeriodicTimer? _timerPolling;
    private CancellationTokenSource? _ctsPolling;
    private bool _disposed;

    private static readonly string _rutaConfiguracion = Path.Combine(
        AppContext.BaseDirectory, "configuracion-rig.json");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly IReadOnlyDictionary<string, long> _frecuenciasPorBanda = new Dictionary<string, long>
    {
        ["160m"] = 1_840_000,
        ["80m"] = 3_573_000,
        ["40m"] = 7_074_000,
        ["30m"] = 10_136_000,
        ["20m"] = 14_074_000,
        ["17m"] = 18_100_000,
        ["15m"] = 21_074_000,
        ["12m"] = 24_915_000,
        ["10m"] = 28_074_000,
        ["6m"] = 50_313_000,
        ["2m"] = 144_174_000
    };

    // ================================================================
    // ESTADO ACTUAL
    // ================================================================

    /// <summary>Si el rig esta conectado.</summary>
    public bool Conectado { get; private set; }

    /// <summary>Frecuencia actual en Hz.</summary>
    public long FrecuenciaHz { get; private set; } = 14_074_000;

    /// <summary>Modo de operacion actual.</summary>
    public string ModoActual { get; private set; } = "FT8";

    /// <summary>Banda actual.</summary>
    public string BandaActual { get; private set; } = "20m";

    /// <summary>Nivel del S-meter.</summary>
    public int NivelSenal { get; private set; }

    /// <summary>Porcentaje del S-meter.</summary>
    public double NivelSenalPorcentaje { get; private set; }

    /// <summary>Si esta transmitiendo.</summary>
    public bool Transmitiendo { get; private set; }

    /// <summary>VFO activo.</summary>
    public char VfoActivo { get; private set; } = 'A';

    /// <summary>Potencia en vatios.</summary>
    public double PotenciaVatios { get; private set; } = 50.0;

    /// <summary>Si el split esta activo.</summary>
    public bool SplitActivo { get; private set; }

    /// <summary>Si el audio esta capturando.</summary>
    public bool AudioCapturando { get; private set; }

    /// <summary>Descripcion de la conexion.</summary>
    public string TipoConexion { get; private set; } = "Sin conexion";

    /// <summary>Detalle del estado de conexion.</summary>
    public string EstadoConexionDetalle { get; private set; } = "";

    /// <summary>Referencia al control de rig actual (para uso interno de hubs).</summary>
    public IControlRig? ControlRig => _controlRig;

    /// <summary>Referencia al pipeline de audio.</summary>
    public IAudioPipeline AudioPipeline => _audioPipeline;

    /// <summary>Referencia al servicio de waterfall.</summary>
    public IServicioWaterfall ServicioWaterfall => _servicioWaterfall;

    // ================================================================
    // EVENTOS
    // ================================================================

    /// <summary>Se dispara cada vez que cambia el estado del rig.</summary>
    public event EventHandler<EstadoRigDto>? EstadoCambiado;

    /// <summary>Se dispara cuando cambia el estado de conexion.</summary>
    public event EventHandler<(bool Conectado, string Detalle)>? ConexionCambiada;

    // ================================================================
    // CONSTRUCTOR
    // ================================================================

    /// <summary>
    /// Crea el servicio de estado de operacion.
    /// </summary>
    public ServicioEstadoOperacion(
        IAudioPipeline audioPipeline,
        IServicioWaterfall servicioWaterfall,
        ILogger<ServicioEstadoOperacion> logger)
    {
        _audioPipeline = audioPipeline ?? throw new ArgumentNullException(nameof(audioPipeline));
        _servicioWaterfall = servicioWaterfall ?? throw new ArgumentNullException(nameof(servicioWaterfall));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ================================================================
    // CONEXION
    // ================================================================

    /// <summary>
    /// Conecta al radio con la configuracion proporcionada.
    /// Inicia polling y captura de audio automaticamente.
    /// </summary>
    public async Task ConectarAsync(ConfiguracionConexionDto config)
    {
        if (Conectado)
        {
            return;
        }

        try
        {
            EstadoConexionDetalle = "Conectando...";
            ConexionCambiada?.Invoke(this, (false, EstadoConexionDetalle));

            GuardarConfiguracion(config);

            if (config.UsarCatSerial)
            {
                await ConectarCatSerialAsync(config);
            }
            else
            {
                await ConectarRigctldAsync(config);
            }

            if (Conectado)
            {
                EstadoConexionDetalle = "Conectado";
                IniciarPolling(config.IntervaloPollingMs);
                await IniciarCapturaAudioAsync(config.DispositivoAudioEntrada, config.TasaDeMuestreoHz);
            }
            else
            {
                EstadoConexionDetalle = "No se pudo conectar";
            }

            ConexionCambiada?.Invoke(this, (Conectado, EstadoConexionDetalle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar al radio.");
            Conectado = false;
            TipoConexion = "Error";
            EstadoConexionDetalle = $"Error: {ex.Message}";
            ConexionCambiada?.Invoke(this, (false, EstadoConexionDetalle));
        }
    }

    /// <summary>
    /// Desconecta del radio y detiene polling y audio.
    /// </summary>
    public async Task DesconectarAsync()
    {
        try
        {
            DetenerPolling();
            await DetenerCapturaAudioAsync();

            if (_controlRig is not null)
            {
                await _controlRig.DesconectarAsync();
                await _controlRig.DisposeAsync();
                _controlRig = null;
            }

            Conectado = false;
            TipoConexion = "Sin conexion";
            EstadoConexionDetalle = "Desconectado";
            ConexionCambiada?.Invoke(this, (false, EstadoConexionDetalle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desconectar del radio.");
        }
    }

    private async Task ConectarCatSerialAsync(ConfiguracionConexionDto config)
    {
        if (string.IsNullOrWhiteSpace(config.Puerto))
        {
            EstadoConexionDetalle = "Seleccione un puerto COM";
            return;
        }

        ConfiguracionPuertoSerie configuracion = new()
        {
            PuertoSerie = config.Puerto,
            VelocidadBaudios = config.Baudios,
            Modelo = Enum.TryParse<ModeloRadio>(config.Modelo, out ModeloRadio modelo) ? modelo : ModeloRadio.Automatico,
            BitsDeDatos = config.BitsDeDatos,
            BitsDeParada = (StopBits)config.BitsDeParada,
            Paridad = Enum.TryParse<Parity>(config.Paridad, out Parity paridad) ? paridad : Parity.None,
            RtsEnable = config.RtsEnable,
            DtrEnable = config.DtrEnable,
            IntervaloPollingMs = config.IntervaloPollingMs
        };

        ClienteCatSerial cliente = new(configuracion);
        await cliente.ConectarAsync();
        _controlRig = cliente;
        Conectado = cliente.EstaConectado;

        if (Conectado)
        {
            string modeloDetectado = cliente.ModeloRadio ?? "Auto";
            TipoConexion = $"CAT {config.Puerto} @ {config.Baudios} | {modeloDetectado}";
        }
    }

    private async Task ConectarRigctldAsync(ConfiguracionConexionDto config)
    {
        ClienteRigctld cliente = new();
        await cliente.ConectarAsync(config.HostRigctld, config.PuertoRigctld);
        _controlRig = cliente;
        Conectado = cliente.EstaConectado;

        if (Conectado)
        {
            string modeloDetectado = cliente.ModeloRadio ?? "Desconocido";
            TipoConexion = $"rigctld @ {config.HostRigctld}:{config.PuertoRigctld} | {modeloDetectado}";
        }
    }

    // ================================================================
    // CAMBIOS DE FRECUENCIA/MODO/PTT
    // ================================================================

    /// <summary>Cambia la frecuencia del radio.</summary>
    public async Task CambiarFrecuenciaAsync(long frecuenciaHz)
    {
        if (frecuenciaHz <= 0) { return; }

        FrecuenciaHz = frecuenciaHz;
        BandaActual = DeterminarBandaDesdeFrecuencia(frecuenciaHz);

        if (Conectado && _controlRig is not null)
        {
            Frecuencia frecuencia = Frecuencia.DesdeHz(frecuenciaHz);
            await _controlRig.CambiarFrecuenciaAsync(frecuencia);
        }

        EmitirEstado();
    }

    /// <summary>Cambia el modo del radio.</summary>
    public async Task CambiarModoAsync(string modo)
    {
        if (string.IsNullOrWhiteSpace(modo)) { return; }

        ModoActual = modo;

        if (Conectado && _controlRig is not null && Enum.TryParse<ModoOperacion>(modo, out ModoOperacion modoOp))
        {
            await _controlRig.CambiarModoAsync(modoOp);
        }

        EmitirEstado();
    }

    /// <summary>Cambia la banda del radio.</summary>
    public async Task CambiarBandaAsync(string banda)
    {
        if (string.IsNullOrWhiteSpace(banda)) { return; }

        BandaActual = banda;

        if (_frecuenciasPorBanda.TryGetValue(banda, out long frecuenciaHz))
        {
            FrecuenciaHz = frecuenciaHz;

            if (Conectado && _controlRig is not null)
            {
                Frecuencia frecuencia = Frecuencia.DesdeHz(frecuenciaHz);
                await _controlRig.CambiarFrecuenciaAsync(frecuencia);
            }
        }

        EmitirEstado();
    }

    /// <summary>Activa o desactiva el PTT.</summary>
    public async Task CambiarPttAsync(bool activar)
    {
        if (!Conectado || _controlRig is null) { return; }

        await _controlRig.CambiarPttAsync(activar);
        Transmitiendo = activar;
        EmitirEstado();
    }

    /// <summary>Cambia el VFO activo.</summary>
    public async Task CambiarVfoAsync()
    {
        char nuevoVfo = VfoActivo == 'A' ? 'B' : 'A';

        if (Conectado && _controlRig is not null)
        {
            await _controlRig.CambiarVfoAsync(nuevoVfo);
        }

        VfoActivo = nuevoVfo;
        EmitirEstado();
    }

    // ================================================================
    // CONSULTAS
    // ================================================================

    /// <summary>Obtiene los puertos serie disponibles.</summary>
    public IReadOnlyList<string> ObtenerPuertosDisponibles()
    {
        try
        {
            return ClienteCatSerial.ObtenerPuertosDisponibles();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al enumerar puertos serie.");
            return Array.Empty<string>();
        }
    }

    /// <summary>Obtiene los dispositivos de audio disponibles.</summary>
    public async Task<IReadOnlyList<DispositivoAudio>> ObtenerDispositivosAudioAsync()
    {
        try
        {
            return await _audioPipeline.ObtenerDispositivosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al enumerar dispositivos de audio.");
            return Array.Empty<DispositivoAudio>();
        }
    }

    /// <summary>Obtiene un snapshot del estado actual como DTO.</summary>
    public EstadoRigDto ObtenerEstadoActual()
    {
        return new EstadoRigDto(
            FrecuenciaHz,
            FormatearFrecuencia(FrecuenciaHz),
            ModoActual,
            BandaActual,
            NivelSenal,
            NivelSenalPorcentaje,
            Transmitiendo,
            VfoActivo,
            PotenciaVatios,
            SplitActivo);
    }

    // ================================================================
    // AUDIO
    // ================================================================

    private async Task IniciarCapturaAudioAsync(string dispositivoId, int tasaDeMuestreoHz)
    {
        if (string.IsNullOrWhiteSpace(dispositivoId))
        {
            _logger.LogWarning("No hay dispositivo de audio seleccionado.");
            return;
        }

        try
        {
            if (_audioPipeline.EstaActivo)
            {
                await _audioPipeline.DetenerCapturaAsync();
            }

            await _audioPipeline.IniciarCapturaAsync(dispositivoId, tasaDeMuestreoHz);
            AudioCapturando = true;
            _logger.LogInformation("Captura de audio iniciada: {Dispositivo} @ {TasaMuestreo} Hz.",
                dispositivoId, tasaDeMuestreoHz);

            if (!_servicioWaterfall.EstaActivo)
            {
                await _servicioWaterfall.IniciarAsync(2048);
                _logger.LogInformation("Waterfall iniciado automaticamente con FFT 2048.");
            }
        }
        catch (Exception ex)
        {
            AudioCapturando = false;
            _logger.LogError(ex, "Error al iniciar captura de audio.");
        }
    }

    private async Task DetenerCapturaAudioAsync()
    {
        try
        {
            if (_servicioWaterfall.EstaActivo)
            {
                await _servicioWaterfall.DetenerAsync();
            }

            if (_audioPipeline.EstaActivo)
            {
                await _audioPipeline.DetenerCapturaAsync();
            }

            AudioCapturando = false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al detener captura de audio.");
        }
    }

    // ================================================================
    // POLLING
    // ================================================================

    private void IniciarPolling(int intervaloMs)
    {
        DetenerPolling();
        _ctsPolling = new CancellationTokenSource();
        int intervalo = Math.Max(intervaloMs, 100);
        _timerPolling = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalo));
        CancellationToken token = _ctsPolling.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timerPolling.WaitForNextTickAsync(token))
                {
                    await ActualizarEstadoDesdeRigAsync(token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el bucle de polling del rig.");
                Conectado = false;
                TipoConexion = "Conexion perdida";
                EstadoConexionDetalle = $"Polling fallido: {ex.Message}";
                ConexionCambiada?.Invoke(this, (false, EstadoConexionDetalle));
            }
        }, token);
    }

    private void DetenerPolling()
    {
        _ctsPolling?.Cancel();
        _ctsPolling?.Dispose();
        _ctsPolling = null;
        _timerPolling?.Dispose();
        _timerPolling = null;
    }

    private async Task ActualizarEstadoDesdeRigAsync(CancellationToken ct)
    {
        try
        {
            if (_controlRig is null) { return; }

            if (!_controlRig.EstaConectado)
            {
                Conectado = false;
                TipoConexion = "Conexion perdida";
                EstadoConexionDetalle = "El radio se desconecto";
                ConexionCambiada?.Invoke(this, (false, EstadoConexionDetalle));
                DetenerPolling();
                return;
            }

            EstadoRig estado = await _controlRig.ObtenerEstadoAsync(ct);

            FrecuenciaHz = estado.Frecuencia.Hz;
            NivelSenal = estado.NivelSenal;
            NivelSenalPorcentaje = CalcularPorcentajeSenal(estado.NivelSenal);
            PotenciaVatios = estado.PotenciaVatios;
            Transmitiendo = estado.Transmitiendo;
            VfoActivo = estado.VfoActivo;

            if (estado.SubModo.HasValue)
            {
                ModoActual = estado.SubModo.Value.ToString();
            }
            else
            {
                ModoActual = estado.Modo.ToString();
            }

            BandaActual = DeterminarBandaDesdeFrecuencia(estado.Frecuencia.Hz);

            EmitirEstado();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al obtener estado del rig durante polling.");
        }
    }

    // ================================================================
    // AUXILIARES
    // ================================================================

    private void EmitirEstado()
    {
        EstadoCambiado?.Invoke(this, ObtenerEstadoActual());
    }

    /// <summary>Formatea frecuencia en Hz con separadores de punto.</summary>
    public static string FormatearFrecuencia(long hz)
    {
        string hzStr = hz.ToString();
        if (hzStr.Length > 6) { return $"{hzStr[..^6]}.{hzStr[^6..^3]}.{hzStr[^3..]}"; }
        if (hzStr.Length > 3) { return $"{hzStr[..^3]}.{hzStr[^3..]}"; }
        return hzStr;
    }

    /// <summary>Convierte magnitudes double[] a byte[] para transmision eficiente.</summary>
    public static byte[] ConvertirMagnitudesABytes(double[] magnitudesDb)
    {
        byte[] resultado = new byte[magnitudesDb.Length];
        for (int i = 0; i < magnitudesDb.Length; i++)
        {
            // Mapear rango tipico -120dB a 0dB -> 0 a 255
            double normalizado = (magnitudesDb[i] + 120.0) / 120.0;
            normalizado = Math.Clamp(normalizado, 0.0, 1.0);
            resultado[i] = (byte)(normalizado * 255.0);
        }
        return resultado;
    }

    private static double CalcularPorcentajeSenal(int nivelSenal)
    {
        if (nivelSenal <= 0) { return 0.0; }
        if (nivelSenal <= 9) { return nivelSenal * (60.0 / 9.0); }
        double exceso = Math.Min(nivelSenal - 9, 6);
        return 60.0 + (exceso * (40.0 / 6.0));
    }

    private static string DeterminarBandaDesdeFrecuencia(long frecuenciaHz)
    {
        foreach (KeyValuePair<string, long> kvp in _frecuenciasPorBanda)
        {
            long frecBanda = kvp.Value;
            long margen = frecBanda switch
            {
                < 5_000_000 => 500_000,
                < 15_000_000 => 350_000,
                < 30_000_000 => 500_000,
                _ => 2_000_000
            };

            if (Math.Abs(frecuenciaHz - frecBanda) < margen) { return kvp.Key; }
        }

        return "---";
    }

    private void GuardarConfiguracion(ConfiguracionConexionDto config)
    {
        try
        {
            string json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_rutaConfiguracion, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al guardar configuracion de conexion.");
        }
    }

    /// <summary>Libera recursos.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) { return; }
        _disposed = true;

        DetenerPolling();

        try
        {
            await DetenerCapturaAudioAsync();
        }
        catch { }

        if (_controlRig is not null)
        {
            try
            {
                await _controlRig.DesconectarAsync();
                await _controlRig.DisposeAsync();
            }
            catch { }
        }
    }
}
