using System.IO.Ports;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.Rig.Cat;

namespace RadioAficionado.Nativo.Rig;

/// <summary>
/// Cliente CAT directo por puerto serie para controlar radios sin necesidad de rigctld.
/// Soporta protocolos Yaesu, Icom CI-V y Kenwood/Elecraft.
/// Implementa <see cref="IControlRig"/> con polling periódico del estado del radio.
/// </summary>
public sealed class ClienteCatSerial : IControlRig
{
    private readonly ConfiguracionPuertoSerie _configuracion;
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private SerialPort? _puertoSerie;
    private IProtocoloCat? _protocolo;
    private CancellationTokenSource? _ctsPolling;
    private Task? _tareaPolling;
    private EstadoRig? _ultimoEstado;
    private bool _disposed;

    /// <inheritdoc />
    public bool EstaConectado => _puertoSerie?.IsOpen == true;

    /// <inheritdoc />
    public string? ModeloRadio { get; private set; }

    /// <inheritdoc />
    public event EventHandler<EstadoRig>? EstadoCambiado;

    /// <summary>
    /// Crea una nueva instancia del cliente CAT serial con la configuración especificada.
    /// </summary>
    /// <param name="configuracion">Configuración del puerto serie y protocolo.</param>
    /// <exception cref="ArgumentNullException">Si la configuración es null.</exception>
    public ClienteCatSerial(ConfiguracionPuertoSerie configuracion)
    {
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
    }

    /// <summary>
    /// Crea una nueva instancia del cliente CAT serial con la configuración por defecto.
    /// </summary>
    public ClienteCatSerial() : this(new ConfiguracionPuertoSerie())
    {
    }

    /// <summary>
    /// Obtiene la lista de puertos serie disponibles en el sistema.
    /// </summary>
    /// <returns>Lista de nombres de puertos serie disponibles.</returns>
    public static IReadOnlyList<string> ObtenerPuertosDisponibles()
    {
        return SerialPort.GetPortNames();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Los parámetros host y puerto se ignoran en esta implementación.
    /// La conexión se realiza al puerto serie configurado en <see cref="ConfiguracionPuertoSerie"/>.
    /// </remarks>
    public async Task ConectarAsync(string host = "localhost", int puerto = 4532, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_configuracion.PuertoSerie))
        {
            throw new ArgumentException("El nombre del puerto serie no puede estar vacío.");
        }

        await DesconectarAsync(ct).ConfigureAwait(false);

        _puertoSerie = new SerialPort
        {
            PortName = _configuracion.PuertoSerie,
            BaudRate = _configuracion.VelocidadBaudios,
            DataBits = _configuracion.BitsDeDatos,
            Parity = _configuracion.Paridad,
            StopBits = _configuracion.BitsDeParada,
            RtsEnable = _configuracion.RtsEnable,
            DtrEnable = _configuracion.DtrEnable,
            ReadTimeout = _configuracion.TimeoutLecturaMs,
            WriteTimeout = _configuracion.TimeoutEscrituraMs
        };

        await Task.Run(() => _puertoSerie.Open(), ct).ConfigureAwait(false);

        _protocolo = FabricaProtocoloCat.Crear(_configuracion.Modelo);

        if (_protocolo is null)
        {
            _protocolo = await AutoDetectarProtocoloAsync(ct).ConfigureAwait(false);
        }

        if (_protocolo is not null)
        {
            ModeloRadio = $"{_protocolo.NombreFabricante} (CAT Serial)";
        }

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

        if (_puertoSerie is not null)
        {
            if (_puertoSerie.IsOpen)
            {
                await Task.Run(() => _puertoSerie.Close()).ConfigureAwait(false);
            }

            _puertoSerie.Dispose();
            _puertoSerie = null;
        }

        _protocolo = null;
        _ultimoEstado = null;
        ModeloRadio = null;
    }

    /// <inheritdoc />
    public async Task<EstadoRig> ObtenerEstadoAsync(CancellationToken ct = default)
    {
        VerificarConexion();

        long frecuenciaHz = await LeerFrecuenciaInternaAsync(ct).ConfigureAwait(false);
        (ModoOperacion modo, SubModoOperacion? subModo) = await LeerModoInternoAsync(ct).ConfigureAwait(false);
        bool transmitiendo = await LeerPttInternoAsync(ct).ConfigureAwait(false);
        int nivelSenal = await LeerNivelSenalInternoAsync(ct).ConfigureAwait(false);

        EstadoRig estado = new()
        {
            Frecuencia = Frecuencia.DesdeHz(frecuenciaHz),
            Modo = modo,
            SubModo = subModo,
            NivelSenal = MapeadorModos.ConvertirDbmAUnidadesS(ConvertirNivelADbm(nivelSenal)),
            Transmitiendo = transmitiendo,
            VfoActivo = 'A'
        };

        return estado;
    }

    /// <inheritdoc />
    public async Task CambiarFrecuenciaAsync(Frecuencia frecuencia, CancellationToken ct = default)
    {
        VerificarConexion();

        byte[] comando = _protocolo!.ComandoCambiarFrecuencia(frecuencia.Hz);
        await EnviarComandoAsync(comando, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CambiarModoAsync(ModoOperacion modo, SubModoOperacion? subModo = null, CancellationToken ct = default)
    {
        VerificarConexion();

        byte[] comando = _protocolo!.ComandoCambiarModo(modo);
        await EnviarComandoAsync(comando, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CambiarPotenciaAsync(double vatios, CancellationToken ct = default)
    {
        VerificarConexion();

        // Control de potencia no implementado por CAT directo en la mayoría de radios.
        // Se lanza excepción para mantener la interfaz.
        await Task.CompletedTask;
        throw new NotSupportedException("El control de potencia no está soportado por el protocolo CAT directo.");
    }

    /// <inheritdoc />
    public async Task CambiarPttAsync(bool activar, CancellationToken ct = default)
    {
        VerificarConexion();

        byte[] comando = _protocolo!.ComandoCambiarPtt(activar);
        await EnviarComandoAsync(comando, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CambiarVfoAsync(char vfo, CancellationToken ct = default)
    {
        VerificarConexion();

        // Cambio de VFO no implementado de forma genérica por CAT directo.
        await Task.CompletedTask;
        throw new NotSupportedException("El cambio de VFO no está soportado por el protocolo CAT directo genérico.");
    }

    /// <inheritdoc />
    public async Task ActivarSplitAsync(bool activar, CancellationToken ct = default)
    {
        VerificarConexion();

        byte[] comando = _protocolo!.ComandoActivarSplit(activar);
        await EnviarComandoAsync(comando, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CambiarFrecuenciaVfoBAsync(Frecuencia frecuencia, CancellationToken ct = default)
    {
        VerificarConexion();

        byte[] comando = _protocolo!.ComandoCambiarFrecuenciaVfoB(frecuencia.Hz);
        await EnviarComandoAsync(comando, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CambiarPttDtrAsync(bool activar, CancellationToken ct = default)
    {
        if (_puertoSerie is null || !_puertoSerie.IsOpen)
        {
            throw new InvalidOperationException("No hay conexión activa con el puerto serie.");
        }

        await Task.Run(() => _puertoSerie.DtrEnable = activar).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CambiarPttRtsAsync(bool activar, CancellationToken ct = default)
    {
        if (_puertoSerie is null || !_puertoSerie.IsOpen)
        {
            throw new InvalidOperationException("No hay conexión activa con el puerto serie.");
        }

        await Task.Run(() => _puertoSerie.RtsEnable = activar).ConfigureAwait(false);
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
    /// Envía un comando binario por el puerto serie, protegido por semáforo.
    /// </summary>
    /// <param name="comando">Bytes del comando a enviar.</param>
    /// <param name="ct">Token de cancelación.</param>
    private async Task EnviarComandoAsync(byte[] comando, CancellationToken ct)
    {
        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_puertoSerie is null || !_puertoSerie.IsOpen)
            {
                throw new InvalidOperationException("No hay conexión activa con el puerto serie.");
            }

            await Task.Run(() => _puertoSerie.Write(comando, 0, comando.Length), ct).ConfigureAwait(false);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>
    /// Envía un comando y lee la respuesta del tamaño esperado, protegido por semáforo.
    /// </summary>
    /// <param name="comando">Bytes del comando a enviar.</param>
    /// <param name="tamanoRespuesta">Número de bytes esperados en la respuesta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Bytes de la respuesta del radio.</returns>
    private async Task<byte[]> EnviarYLeerAsync(byte[] comando, int tamanoRespuesta, CancellationToken ct)
    {
        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_puertoSerie is null || !_puertoSerie.IsOpen)
            {
                throw new InvalidOperationException("No hay conexión activa con el puerto serie.");
            }

            _puertoSerie.DiscardInBuffer();
            await Task.Run(() => _puertoSerie.Write(comando, 0, comando.Length), ct).ConfigureAwait(false);

            byte[] buffer = new byte[tamanoRespuesta];
            int bytesLeidos = 0;

            while (bytesLeidos < tamanoRespuesta)
            {
                int leidos = await Task.Run(() =>
                {
                    try
                    {
                        return _puertoSerie.Read(buffer, bytesLeidos, tamanoRespuesta - bytesLeidos);
                    }
                    catch (TimeoutException)
                    {
                        return 0;
                    }
                }, ct).ConfigureAwait(false);

                if (leidos == 0)
                {
                    throw new TimeoutException(
                        $"Timeout leyendo respuesta del radio: se esperaban {tamanoRespuesta} bytes, se recibieron {bytesLeidos}.");
                }

                bytesLeidos += leidos;
            }

            return buffer;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>
    /// Lee la frecuencia actual del radio.
    /// </summary>
    private async Task<long> LeerFrecuenciaInternaAsync(CancellationToken ct)
    {
        byte[] comando = _protocolo!.ComandoLeerFrecuencia();
        byte[] respuesta = await EnviarYLeerAsync(comando, _protocolo.TamanoRespuestaFrecuencia, ct).ConfigureAwait(false);
        return _protocolo.ParsearFrecuencia(respuesta);
    }

    /// <summary>
    /// Lee el modo de operación actual del radio.
    /// </summary>
    private async Task<(ModoOperacion modo, SubModoOperacion? subModo)> LeerModoInternoAsync(CancellationToken ct)
    {
        byte[] comando = _protocolo!.ComandoLeerModo();
        byte[] respuesta = await EnviarYLeerAsync(comando, _protocolo.TamanoRespuestaModo, ct).ConfigureAwait(false);
        return _protocolo.ParsearModo(respuesta);
    }

    /// <summary>
    /// Lee el estado del PTT del radio.
    /// </summary>
    private async Task<bool> LeerPttInternoAsync(CancellationToken ct)
    {
        byte[] comando = _protocolo!.ComandoLeerPtt();
        byte[] respuesta = await EnviarYLeerAsync(comando, _protocolo.TamanoRespuestaPtt, ct).ConfigureAwait(false);
        return _protocolo.ParsearPtt(respuesta);
    }

    /// <summary>
    /// Lee el nivel de señal del radio.
    /// </summary>
    private async Task<int> LeerNivelSenalInternoAsync(CancellationToken ct)
    {
        byte[] comando = _protocolo!.ComandoLeerNivelSenal();
        byte[] respuesta = await EnviarYLeerAsync(comando, _protocolo.TamanoRespuestaNivelSenal, ct).ConfigureAwait(false);
        return _protocolo.ParsearNivelSenal(respuesta);
    }

    /// <summary>
    /// Intenta detectar automáticamente el protocolo CAT del radio conectado.
    /// Prueba Yaesu, Kenwood e Icom en orden.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El protocolo detectado, o null si no se pudo detectar.</returns>
    private async Task<IProtocoloCat?> AutoDetectarProtocoloAsync(CancellationToken ct)
    {
        IProtocoloCat[] protocolosAProbrar = new IProtocoloCat[]
        {
            new ProtocoloYaesu(),
            new ProtocoloKenwood(),
            new ProtocoloIcom(0x94) // IC-7300 como default
        };

        foreach (IProtocoloCat protocolo in protocolosAProbrar)
        {
            try
            {
                byte[] comando = protocolo.ComandoLeerFrecuencia();
                byte[] respuesta = await EnviarYLeerAsync(comando, protocolo.TamanoRespuestaFrecuencia, ct).ConfigureAwait(false);
                long frecuencia = protocolo.ParsearFrecuencia(respuesta);

                if (frecuencia > 0)
                {
                    return protocolo;
                }
            }
            catch (TimeoutException)
            {
                // Este protocolo no respondió, probar el siguiente.
            }
            catch (InvalidOperationException)
            {
                // Respuesta inválida para este protocolo, probar el siguiente.
            }
        }

        return null;
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

                if (!EstaConectado || _protocolo is null)
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
                break;
            }
            catch (IOException)
            {
                break;
            }
            catch (TimeoutException)
            {
                // Timeout puntual de lectura, continuar el polling.
            }
            catch (InvalidOperationException)
            {
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
            || anterior.Transmitiendo != actual.Transmitiendo
            || anterior.SplitActivo != actual.SplitActivo
            || anterior.FrecuenciaVfoB != actual.FrecuenciaVfoB;
    }

    /// <summary>
    /// Verifica que haya una conexión activa con el radio.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si no hay conexión activa.</exception>
    private void VerificarConexion()
    {
        if (!EstaConectado || _protocolo is null)
        {
            throw new InvalidOperationException("No hay conexión activa con el radio. Llame a ConectarAsync primero.");
        }
    }

    /// <summary>
    /// Convierte un nivel de señal crudo (0-255) a dBm aproximado.
    /// </summary>
    /// <param name="nivelCrudo">Nivel de señal crudo del radio (0-255).</param>
    /// <returns>Nivel de señal en dBm.</returns>
    private static double ConvertirNivelADbm(int nivelCrudo)
    {
        // Mapeo lineal aproximado: 0 = -127 dBm (S0), 255 = -13 dBm (S9+60)
        const double DbmMinimo = -127.0;
        const double DbmMaximo = -13.0;
        const double RangoNivel = 255.0;

        return DbmMinimo + (nivelCrudo / RangoNivel) * (DbmMaximo - DbmMinimo);
    }
}
