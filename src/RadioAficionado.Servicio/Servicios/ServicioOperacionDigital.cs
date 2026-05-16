using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Nativo.ModosDigitales.Ft8;

namespace RadioAficionado.Servicio.Servicios;

/// <summary>
/// Motor de auto-sequencing FT8.
/// Maquina de estados que orquesta QSO automatico:
/// CQ -> respuesta -> reporte -> RRR -> 73.
/// Sincroniza con ventanas de 15 segundos y genera audio TX.
/// </summary>
public sealed class ServicioOperacionDigital : IServicioOperacionDigital
{
    private readonly ServicioEstadoOperacion _estadoOperacion;
    private readonly ILogger<ServicioOperacionDigital> _logger;

    private ConfiguracionSecuencia? _configuracion;
    private FaseQsoFt8 _fase = FaseQsoFt8.Inactivo;
    private string? _indicativoDx;
    private string? _gridDx;
    private int? _reporteEnviado;
    private int? _reporteRecibido;
    private string? _mensajeTxActual;
    private bool _txHabilitado;
    private bool _autoSecuenciaActiva;
    private bool _transmitiendo;
    private bool _ventanaPar = true;
    private PeriodicTimer? _timerVentana;
    private CancellationTokenSource? _ctsVentana;
    private bool _disposed;

    /// <summary>Estado actual de la secuencia.</summary>
    public EstadoSecuencia EstadoActual => new(
        _fase,
        _indicativoDx,
        _gridDx,
        _reporteEnviado,
        _reporteRecibido,
        _mensajeTxActual,
        _txHabilitado,
        _autoSecuenciaActiva,
        _transmitiendo,
        _ventanaPar);

    /// <summary>Evento disparado cuando cambia el estado.</summary>
    public event EventHandler<EstadoSecuencia>? EstadoCambiado;

    /// <summary>
    /// Crea el servicio de operacion digital.
    /// </summary>
    public ServicioOperacionDigital(
        ServicioEstadoOperacion estadoOperacion,
        ILogger<ServicioOperacionDigital> logger)
    {
        _estadoOperacion = estadoOperacion ?? throw new ArgumentNullException(nameof(estadoOperacion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Configura la secuencia con indicativo y localizador propios.</summary>
    public Task ConfigurarAsync(ConfiguracionSecuencia configuracion, CancellationToken ct = default)
    {
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
        _ventanaPar = configuracion.VentanaPar;
        EmitirEstado();
        return Task.CompletedTask;
    }

    /// <summary>Habilita o deshabilita la transmision.</summary>
    public Task HabilitarTxAsync(bool habilitar, CancellationToken ct = default)
    {
        _txHabilitado = habilitar;

        if (habilitar && _ctsVentana is null)
        {
            IniciarTimerVentana();
        }
        else if (!habilitar)
        {
            _transmitiendo = false;
        }

        EmitirEstado();
        return Task.CompletedTask;
    }

    /// <summary>Activa o desactiva el auto-sequencing.</summary>
    public Task HabilitarAutoSecuenciaAsync(bool habilitar, CancellationToken ct = default)
    {
        _autoSecuenciaActiva = habilitar;
        EmitirEstado();
        return Task.CompletedTask;
    }

    /// <summary>Inicia un CQ.</summary>
    public Task LlamarCqAsync(CancellationToken ct = default)
    {
        if (_configuracion is null)
        {
            _logger.LogWarning("No se puede llamar CQ sin configuracion.");
            return Task.CompletedTask;
        }

        _fase = FaseQsoFt8.CQEnviado;
        _indicativoDx = null;
        _gridDx = null;
        _reporteEnviado = null;
        _reporteRecibido = null;
        _mensajeTxActual = GeneradorMensajeFt8.GenerarCQ(
            _configuracion.MiIndicativo,
            _configuracion.MiLocalizador);

        _logger.LogInformation("CQ iniciado: {Mensaje}", _mensajeTxActual);
        EmitirEstado();
        return Task.CompletedTask;
    }

    /// <summary>Selecciona una estacion DX para iniciar QSO.</summary>
    public Task SeleccionarEstacionAsync(
        string indicativoDx,
        string? grid,
        int? reporte,
        CancellationToken ct = default)
    {
        if (_configuracion is null) { return Task.CompletedTask; }

        _indicativoDx = indicativoDx;
        _gridDx = grid;
        _reporteRecibido = reporte;
        _fase = FaseQsoFt8.EsperandoRespuesta;

        // Generar mensaje de respuesta con reporte o localizador
        if (reporte.HasValue)
        {
            _reporteEnviado = reporte.Value;
            _mensajeTxActual = GeneradorMensajeFt8.GenerarRespuesta(
                indicativoDx,
                _configuracion.MiIndicativo,
                reporte.Value);
        }
        else
        {
            _mensajeTxActual = $"{indicativoDx} {_configuracion.MiIndicativo} {_configuracion.MiLocalizador}";
        }

        _logger.LogInformation("Estacion seleccionada: {DX}, Mensaje: {Mensaje}", indicativoDx, _mensajeTxActual);
        EmitirEstado();
        return Task.CompletedTask;
    }

    /// <summary>Procesa una decodificacion recibida para avanzar la secuencia.</summary>
    public Task ProcesarDecodificacionAsync(
        string textoMensaje,
        string? indicativoEmisor,
        int snr,
        CancellationToken ct = default)
    {
        if (_configuracion is null || !_autoSecuenciaActiva) { return Task.CompletedTask; }

        // Parsear el mensaje
        MensajeFt8 mensaje = MensajeFt8.ParsearMensaje(textoMensaje, snr: snr);

        // Solo procesar mensajes que mencionan mi indicativo
        bool esDirigidoAMi = !string.IsNullOrWhiteSpace(mensaje.IndicativoReceptor) &&
            mensaje.IndicativoReceptor.Equals(_configuracion.MiIndicativo, StringComparison.OrdinalIgnoreCase);

        if (!esDirigidoAMi) { return Task.CompletedTask; }

        string emisor = mensaje.IndicativoEmisor ?? indicativoEmisor ?? "";

        switch (_fase)
        {
            case FaseQsoFt8.CQEnviado:
                // Alguien respondio a mi CQ
                _indicativoDx = emisor;
                _gridDx = mensaje.Localizador;
                _reporteRecibido = mensaje.ReporteSenal ?? snr;
                _reporteEnviado = snr;
                _fase = FaseQsoFt8.ReporteEnviado;
                _mensajeTxActual = GeneradorMensajeFt8.GenerarReporte(
                    emisor,
                    _configuracion.MiIndicativo,
                    _reporteEnviado.Value);
                _logger.LogInformation("Respuesta recibida de {DX}, enviando reporte.", emisor);
                break;

            case FaseQsoFt8.EsperandoRespuesta:
            case FaseQsoFt8.ReporteEnviado:
                if (mensaje.TipoMensaje == TipoMensajeFt8.Reporte)
                {
                    // Recibi R+reporte -> enviar RRR
                    _reporteRecibido = mensaje.ReporteSenal;
                    _fase = FaseQsoFt8.RRREnviado;
                    _mensajeTxActual = GeneradorMensajeFt8.GenerarRRR(
                        emisor,
                        _configuracion.MiIndicativo);
                    _logger.LogInformation("Reporte recibido de {DX}, enviando RRR.", emisor);
                }
                else if (mensaje.TipoMensaje == TipoMensajeFt8.Respuesta)
                {
                    // Recibi respuesta con reporte -> enviar R+reporte
                    _reporteRecibido = mensaje.ReporteSenal ?? snr;
                    _reporteEnviado = snr;
                    _fase = FaseQsoFt8.ReporteEnviado;
                    _mensajeTxActual = GeneradorMensajeFt8.GenerarReporte(
                        emisor,
                        _configuracion.MiIndicativo,
                        _reporteEnviado.Value);
                }
                break;

            case FaseQsoFt8.RRREnviado:
                if (mensaje.TipoMensaje == TipoMensajeFt8.RRR ||
                    mensaje.TipoMensaje == TipoMensajeFt8.Setenta73)
                {
                    // Recibi RRR o 73 -> enviar 73
                    _fase = FaseQsoFt8.SetentatresEnviado;
                    _mensajeTxActual = GeneradorMensajeFt8.Generar73(
                        emisor,
                        _configuracion.MiIndicativo);
                    _logger.LogInformation("RRR recibido de {DX}, enviando 73.", emisor);
                }
                break;

            case FaseQsoFt8.SetentatresEnviado:
                // QSO completado
                _fase = FaseQsoFt8.QsoCompletado;
                _txHabilitado = false;
                _mensajeTxActual = null;
                _logger.LogInformation("QSO completado con {DX}.", emisor);
                break;
        }

        EmitirEstado();
        return Task.CompletedTask;
    }

    /// <summary>Detiene la transmision inmediatamente.</summary>
    public async Task DetenerTxAsync(CancellationToken ct = default)
    {
        _txHabilitado = false;
        _transmitiendo = false;

        if (_estadoOperacion.Conectado && _estadoOperacion.ControlRig is not null)
        {
            try
            {
                await _estadoOperacion.ControlRig.CambiarPttAsync(false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al desactivar PTT.");
            }
        }

        EmitirEstado();
    }

    /// <summary>Selecciona un mensaje TX especifico (Tx1-Tx6).</summary>
    public Task SeleccionarMensajeTxAsync(int numeroTx, string? textoLibre = null, CancellationToken ct = default)
    {
        if (_configuracion is null) { return Task.CompletedTask; }

        string dx = _indicativoDx ?? "???";
        int reporte = _reporteEnviado ?? 0;

        _mensajeTxActual = numeroTx switch
        {
            1 => GeneradorMensajeFt8.GenerarCQ(_configuracion.MiIndicativo, _configuracion.MiLocalizador),
            2 => GeneradorMensajeFt8.GenerarRespuesta(dx, _configuracion.MiIndicativo, reporte),
            3 => GeneradorMensajeFt8.GenerarReporte(dx, _configuracion.MiIndicativo, reporte),
            4 => GeneradorMensajeFt8.GenerarRRR(dx, _configuracion.MiIndicativo),
            5 => GeneradorMensajeFt8.Generar73(dx, _configuracion.MiIndicativo),
            6 => textoLibre ?? "",
            _ => _mensajeTxActual
        };

        EmitirEstado();
        return Task.CompletedTask;
    }

    // ================================================================
    // TIMER DE VENTANA FT8 (15 segundos)
    // ================================================================

    private void IniciarTimerVentana()
    {
        DetenerTimerVentana();
        _ctsVentana = new CancellationTokenSource();
        _timerVentana = new PeriodicTimer(TimeSpan.FromSeconds(1));
        CancellationToken token = _ctsVentana.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timerVentana.WaitForNextTickAsync(token))
                {
                    await VerificarVentanaAsync(token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en timer de ventana FT8.");
            }
        }, token);
    }

    private void DetenerTimerVentana()
    {
        _ctsVentana?.Cancel();
        _ctsVentana?.Dispose();
        _ctsVentana = null;
        _timerVentana?.Dispose();
        _timerVentana = null;
    }

    private async Task VerificarVentanaAsync(CancellationToken ct)
    {
        if (!_txHabilitado || string.IsNullOrWhiteSpace(_mensajeTxActual))
        {
            return;
        }

        DateTime utcAhora = DateTime.UtcNow;
        int segundo = utcAhora.Second;

        // Detectar inicio de ventana TX
        bool esInicioVentana = false;
        if (_ventanaPar)
        {
            esInicioVentana = segundo == 0 || segundo == 30;
        }
        else
        {
            esInicioVentana = segundo == 15 || segundo == 45;
        }

        if (esInicioVentana && !_transmitiendo)
        {
            await TransmitirAsync(ct);
        }
    }

    private async Task TransmitirAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_mensajeTxActual) || _configuracion is null)
        {
            return;
        }

        try
        {
            _transmitiendo = true;
            EmitirEstado();

            // Generar audio FT8
            short[]? audioTx = GeneradorAudioFt8.GenerarAudioDesdeMensaje(
                _mensajeTxActual,
                _configuracion.FrecuenciaTxHz);

            if (audioTx is null)
            {
                _logger.LogWarning("No se pudo generar audio para: {Mensaje}", _mensajeTxActual);
                _transmitiendo = false;
                EmitirEstado();
                return;
            }

            // Activar PTT
            if (_estadoOperacion.Conectado && _estadoOperacion.ControlRig is not null)
            {
                await _estadoOperacion.ControlRig.CambiarPttAsync(true, ct);
            }

            // Transmitir audio
            ReadOnlyMemory<short> memoria = new(audioTx);
            await _estadoOperacion.AudioPipeline.TransmitirAudioAsync(memoria, ct);

            // Desactivar PTT
            if (_estadoOperacion.Conectado && _estadoOperacion.ControlRig is not null)
            {
                await _estadoOperacion.ControlRig.CambiarPttAsync(false, ct);
            }

            _logger.LogInformation("Transmision completada: {Mensaje}", _mensajeTxActual);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante transmision FT8.");
        }
        finally
        {
            _transmitiendo = false;
            EmitirEstado();
        }
    }

    // ================================================================
    // AUXILIARES
    // ================================================================

    private void EmitirEstado()
    {
        EstadoCambiado?.Invoke(this, EstadoActual);
    }

    /// <summary>Libera recursos.</summary>
    public ValueTask DisposeAsync()
    {
        if (_disposed) { return ValueTask.CompletedTask; }
        _disposed = true;
        DetenerTimerVentana();
        return ValueTask.CompletedTask;
    }
}
