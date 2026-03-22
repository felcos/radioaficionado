using System.Globalization;
using System.Net.Sockets;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.Rig;

/// <summary>
/// Cliente TCP para comunicarse con el demonio rigctld (Hamlib) y controlar equipos de radio.
/// Implementa <see cref="IControlRig"/> con polling periódico del estado del radio.
/// </summary>
public sealed class ClienteRigctld : IControlRig
{
    private readonly ConfiguracionRig _configuracion;
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private TcpClient? _clienteTcp;
    private StreamReader? _lector;
    private StreamWriter? _escritor;
    private CancellationTokenSource? _ctsPolling;
    private Task? _tareaPolling;
    private EstadoRig? _ultimoEstado;
    private bool _disposed;

    /// <inheritdoc />
    public bool EstaConectado => _clienteTcp?.Connected == true;

    /// <inheritdoc />
    public string? ModeloRadio { get; private set; }

    /// <inheritdoc />
    public event EventHandler<EstadoRig>? EstadoCambiado;

    /// <summary>
    /// Crea una nueva instancia del cliente rigctld con la configuración especificada.
    /// </summary>
    /// <param name="configuracion">Configuración de conexión y comportamiento.</param>
    /// <exception cref="ArgumentNullException">Si la configuración es null.</exception>
    public ClienteRigctld(ConfiguracionRig configuracion)
    {
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
    }

    /// <summary>
    /// Crea una nueva instancia del cliente rigctld con la configuración por defecto.
    /// </summary>
    public ClienteRigctld() : this(new ConfiguracionRig())
    {
    }

    /// <inheritdoc />
    public async Task ConectarAsync(string host = "localhost", int puerto = 4532, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("El host no puede estar vacío.", nameof(host));
        }

        if (puerto is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(puerto), puerto, "El puerto debe estar entre 1 y 65535.");
        }

        await DesconectarAsync(ct).ConfigureAwait(false);

        _clienteTcp = new TcpClient();
        _clienteTcp.ReceiveTimeout = _configuracion.TimeoutMs;
        _clienteTcp.SendTimeout = _configuracion.TimeoutMs;

        using CancellationTokenSource ctsConexion = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ctsConexion.CancelAfter(_configuracion.TimeoutMs);

        await _clienteTcp.ConnectAsync(host, puerto, ctsConexion.Token).ConfigureAwait(false);

        NetworkStream flujoRed = _clienteTcp.GetStream();
        _lector = new StreamReader(flujoRed, leaveOpen: true);
        _escritor = new StreamWriter(flujoRed, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };

        _ctsPolling = new CancellationTokenSource();
        _tareaPolling = EjecutarPollingAsync(_ctsPolling.Token);
    }

    /// <inheritdoc />
    public async Task DesconectarAsync(CancellationToken ct = default)
    {
        if (_ctsPolling is not null)
        {
            await _ctsPolling.CancelAsync().ConfigureAwait(false);

            if (_tareaPolling is not null)
            {
                try
                {
                    await _tareaPolling.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Esperado al cancelar el polling.
                }
            }

            _ctsPolling.Dispose();
            _ctsPolling = null;
            _tareaPolling = null;
        }

        if (_escritor is not null)
        {
            await _escritor.DisposeAsync().ConfigureAwait(false);
            _escritor = null;
        }

        if (_lector is not null)
        {
            _lector.Dispose();
            _lector = null;
        }

        if (_clienteTcp is not null)
        {
            _clienteTcp.Close();
            _clienteTcp.Dispose();
            _clienteTcp = null;
        }

        _ultimoEstado = null;
        ModeloRadio = null;
    }

    /// <inheritdoc />
    public async Task<EstadoRig> ObtenerEstadoAsync(CancellationToken ct = default)
    {
        VerificarConexion();

        Frecuencia frecuencia = await ObtenerFrecuenciaInternaAsync(ct).ConfigureAwait(false);
        (ModoOperacion modo, SubModoOperacion? subModo, int anchoBanda) = await ObtenerModoInternoAsync(ct).ConfigureAwait(false);
        bool transmitiendo = await ObtenerPttInternoAsync(ct).ConfigureAwait(false);
        char vfoActivo = await ObtenerVfoInternoAsync(ct).ConfigureAwait(false);
        int nivelSenal = await ObtenerNivelSenalInternoAsync(ct).ConfigureAwait(false);
        double potenciaVatios = await ObtenerPotenciaInternaAsync(ct).ConfigureAwait(false);

        EstadoRig estado = new()
        {
            Frecuencia = frecuencia,
            Modo = modo,
            SubModo = subModo,
            NivelSenal = nivelSenal,
            PotenciaVatios = potenciaVatios,
            Transmitiendo = transmitiendo,
            AnchoDeBandaHz = anchoBanda,
            VfoActivo = vfoActivo
        };

        return estado;
    }

    /// <inheritdoc />
    public async Task CambiarFrecuenciaAsync(Frecuencia frecuencia, CancellationToken ct = default)
    {
        VerificarConexion();

        string respuesta = await EnviarComandoAsync($"F {frecuencia.Hz}", ct).ConfigureAwait(false);
        VerificarRespuesta(respuesta, "CambiarFrecuencia");
    }

    /// <inheritdoc />
    public async Task CambiarModoAsync(ModoOperacion modo, SubModoOperacion? subModo = null, CancellationToken ct = default)
    {
        VerificarConexion();

        string modoRigctld = MapeadorModos.HaciaRigctld(modo, subModo);
        string respuesta = await EnviarComandoAsync($"M {modoRigctld} 0", ct).ConfigureAwait(false);
        VerificarRespuesta(respuesta, "CambiarModo");
    }

    /// <inheritdoc />
    public async Task CambiarPotenciaAsync(double vatios, CancellationToken ct = default)
    {
        VerificarConexion();

        if (vatios < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vatios), vatios, "La potencia no puede ser negativa.");
        }

        double nivelRfPower = _configuracion.PotenciaMaximaVatios > 0
            ? Math.Min(vatios / _configuracion.PotenciaMaximaVatios, 1.0)
            : 0.0;

        string nivelFormateado = nivelRfPower.ToString("F3", CultureInfo.InvariantCulture);
        string respuesta = await EnviarComandoAsync($"L RFPOWER {nivelFormateado}", ct).ConfigureAwait(false);
        VerificarRespuesta(respuesta, "CambiarPotencia");
    }

    /// <inheritdoc />
    public async Task CambiarPttAsync(bool activar, CancellationToken ct = default)
    {
        VerificarConexion();

        string valor = activar ? "1" : "0";
        string respuesta = await EnviarComandoAsync($"T {valor}", ct).ConfigureAwait(false);
        VerificarRespuesta(respuesta, "CambiarPtt");
    }

    /// <inheritdoc />
    public async Task CambiarVfoAsync(char vfo, CancellationToken ct = default)
    {
        VerificarConexion();

        string vfoRigctld = MapeadorModos.VfoHaciaRigctld(vfo);
        string respuesta = await EnviarComandoAsync($"V {vfoRigctld}", ct).ConfigureAwait(false);
        VerificarRespuesta(respuesta, "CambiarVfo");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DesconectarAsync().ConfigureAwait(false);
        _semaforo.Dispose();
    }

    /// <summary>
    /// Envía un comando de texto a rigctld y lee la respuesta, protegido por semáforo.
    /// </summary>
    /// <param name="comando">Comando a enviar (sin salto de línea).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Primera línea de la respuesta.</returns>
    private async Task<string> EnviarComandoAsync(string comando, CancellationToken ct)
    {
        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_escritor is null || _lector is null)
            {
                throw new InvalidOperationException("No hay conexión activa con rigctld.");
            }

            await _escritor.WriteLineAsync(comando.AsMemory(), ct).ConfigureAwait(false);

            string? respuesta = await _lector.ReadLineAsync(ct).ConfigureAwait(false);

            if (respuesta is null)
            {
                throw new IOException("La conexión con rigctld se cerró inesperadamente.");
            }

            return respuesta;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>
    /// Envía un comando y lee múltiples líneas de respuesta.
    /// </summary>
    /// <param name="comando">Comando a enviar.</param>
    /// <param name="cantidadLineas">Número de líneas esperadas en la respuesta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de líneas leídas.</returns>
    private async Task<List<string>> EnviarComandoMultilineaAsync(string comando, int cantidadLineas, CancellationToken ct)
    {
        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_escritor is null || _lector is null)
            {
                throw new InvalidOperationException("No hay conexión activa con rigctld.");
            }

            await _escritor.WriteLineAsync(comando.AsMemory(), ct).ConfigureAwait(false);

            List<string> lineas = new(cantidadLineas);
            for (int i = 0; i < cantidadLineas; i++)
            {
                string? linea = await _lector.ReadLineAsync(ct).ConfigureAwait(false);

                if (linea is null)
                {
                    throw new IOException("La conexión con rigctld se cerró inesperadamente.");
                }

                lineas.Add(linea);
            }

            return lineas;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>
    /// Obtiene la frecuencia actual del VFO activo.
    /// </summary>
    private async Task<Frecuencia> ObtenerFrecuenciaInternaAsync(CancellationToken ct)
    {
        string respuesta = await EnviarComandoAsync("f", ct).ConfigureAwait(false);

        if (!long.TryParse(respuesta.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long hz) || hz <= 0)
        {
            throw new InvalidOperationException($"Respuesta de frecuencia inválida de rigctld: '{respuesta}'");
        }

        return Frecuencia.DesdeHz(hz);
    }

    /// <summary>
    /// Obtiene el modo, submodo y ancho de banda actuales.
    /// </summary>
    private async Task<(ModoOperacion Modo, SubModoOperacion? SubModo, int AnchoBanda)> ObtenerModoInternoAsync(CancellationToken ct)
    {
        List<string> lineas = await EnviarComandoMultilineaAsync("m", 2, ct).ConfigureAwait(false);

        string modoTexto = lineas[0].Trim();
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld(modoTexto);

        int anchoBanda = 0;
        if (int.TryParse(lineas[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bw))
        {
            anchoBanda = bw;
        }

        return (resultado.Modo, resultado.SubModo, anchoBanda);
    }

    /// <summary>
    /// Obtiene el estado del PTT.
    /// </summary>
    private async Task<bool> ObtenerPttInternoAsync(CancellationToken ct)
    {
        string respuesta = await EnviarComandoAsync("t", ct).ConfigureAwait(false);

        return respuesta.Trim() != "0";
    }

    /// <summary>
    /// Obtiene el VFO activo.
    /// </summary>
    private async Task<char> ObtenerVfoInternoAsync(CancellationToken ct)
    {
        string respuesta = await EnviarComandoAsync("v", ct).ConfigureAwait(false);

        return MapeadorModos.VfoDesdeRigctld(respuesta);
    }

    /// <summary>
    /// Obtiene el nivel de señal del S-meter en unidades S.
    /// </summary>
    private async Task<int> ObtenerNivelSenalInternoAsync(CancellationToken ct)
    {
        string respuesta = await EnviarComandoAsync("l STRENGTH", ct).ConfigureAwait(false);

        if (double.TryParse(respuesta.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double dBm))
        {
            return MapeadorModos.ConvertirDbmAUnidadesS(dBm);
        }

        return 0;
    }

    /// <summary>
    /// Obtiene la potencia de transmisión actual en vatios.
    /// </summary>
    private async Task<double> ObtenerPotenciaInternaAsync(CancellationToken ct)
    {
        string respuesta = await EnviarComandoAsync("l RFPOWER", ct).ConfigureAwait(false);

        if (double.TryParse(respuesta.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double nivelRfPower))
        {
            return nivelRfPower * _configuracion.PotenciaMaximaVatios;
        }

        return 0.0;
    }

    /// <summary>
    /// Ejecuta el bucle de polling que lee el estado del radio periódicamente
    /// y dispara <see cref="EstadoCambiado"/> cuando hay cambios.
    /// </summary>
    private async Task EjecutarPollingAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_configuracion.IntervaloPollingMs, ct).ConfigureAwait(false);

                if (!EstaConectado)
                {
                    break;
                }

                EstadoRig estadoActual = await ObtenerEstadoAsync(ct).ConfigureAwait(false);

                if (HayCambioDeEstado(_ultimoEstado, estadoActual))
                {
                    _ultimoEstado = estadoActual;
                    EstadoCambiado?.Invoke(this, estadoActual);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada al detener el polling.
                break;
            }
            catch (IOException)
            {
                // Conexión perdida. Salir del bucle de polling.
                break;
            }
            catch (InvalidOperationException)
            {
                // Conexión no disponible. Salir del bucle de polling.
                break;
            }
        }
    }

    /// <summary>
    /// Determina si el estado actual difiere del estado anterior.
    /// </summary>
    private static bool HayCambioDeEstado(EstadoRig? anterior, EstadoRig actual)
    {
        if (anterior is null)
        {
            return true;
        }

        return anterior.Frecuencia != actual.Frecuencia
            || anterior.Modo != actual.Modo
            || anterior.SubModo != actual.SubModo
            || anterior.NivelSenal != actual.NivelSenal
            || Math.Abs(anterior.PotenciaVatios - actual.PotenciaVatios) > 0.01
            || anterior.Transmitiendo != actual.Transmitiendo
            || anterior.AnchoDeBandaHz != actual.AnchoDeBandaHz
            || anterior.VfoActivo != actual.VfoActivo;
    }

    /// <summary>
    /// Verifica que la respuesta de rigctld sea exitosa (RPRT 0).
    /// </summary>
    /// <param name="respuesta">Línea de respuesta de rigctld.</param>
    /// <param name="operacion">Nombre de la operación para el mensaje de error.</param>
    /// <exception cref="InvalidOperationException">Si la respuesta indica un error.</exception>
    private static void VerificarRespuesta(string respuesta, string operacion)
    {
        string respuestaTrimmed = respuesta.Trim();

        if (respuestaTrimmed != "RPRT 0")
        {
            throw new InvalidOperationException(
                $"Error en la operación '{operacion}'. Respuesta de rigctld: '{respuestaTrimmed}'");
        }
    }

    /// <summary>
    /// Verifica que haya una conexión activa con rigctld.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si no hay conexión activa.</exception>
    private void VerificarConexion()
    {
        if (!EstaConectado)
        {
            throw new InvalidOperationException("No hay conexión activa con rigctld. Llame a ConectarAsync primero.");
        }
    }
}
