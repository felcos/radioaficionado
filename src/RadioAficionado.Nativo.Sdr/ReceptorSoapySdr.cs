using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Sdr;

namespace RadioAficionado.Nativo.Sdr;

/// <summary>
/// Implementación de <see cref="IReceptorSdr"/> usando la librería nativa SoapySDR.
/// Gestiona la conexión, configuración y lectura continua de muestras IQ
/// desde dispositivos SDR compatibles con SoapySDR.
/// </summary>
public sealed class ReceptorSoapySdr : IReceptorSdr
{
    private readonly ILogger<ReceptorSoapySdr> _logger;
    private readonly ConfiguracionSdr _configuracion;
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private IntPtr _dispositivo;
    private IntPtr _stream;
    private Thread? _hiloLectura;
    private CancellationTokenSource? _ctsLectura;
    private bool _disposed;

    /// <inheritdoc />
    public bool EstaConectado { get; private set; }

    /// <inheritdoc />
    public double FrecuenciaCentralHz { get; private set; }

    /// <inheritdoc />
    public double AnchoDeBandaHz { get; private set; }

    /// <inheritdoc />
    public double GananciaDb { get; private set; }

    /// <inheritdoc />
    public double TasaDeMuestreoHz { get; private set; }

    /// <inheritdoc />
    public string? DispositivoActual { get; private set; }

    /// <inheritdoc />
    public event EventHandler<MuestrasSdrEventArgs>? MuestrasRecibidas;

    /// <summary>
    /// Crea una nueva instancia del receptor SoapySDR.
    /// </summary>
    /// <param name="logger">Logger para diagnóstico.</param>
    /// <param name="configuracion">Configuración inicial del receptor SDR.</param>
    public ReceptorSoapySdr(ILogger<ReceptorSoapySdr> logger, ConfiguracionSdr configuracion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
    }

    /// <inheritdoc />
    public async Task ConectarAsync(string dispositivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dispositivo))
        {
            throw new ArgumentException(
                "El identificador del dispositivo no puede ser nulo ni vacío.",
                nameof(dispositivo));
        }

        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (EstaConectado)
            {
                _logger.LogWarning("Ya hay un dispositivo SDR conectado: {Dispositivo}. Desconectando primero.", DispositivoActual);
                await DesconectarInternoAsync().ConfigureAwait(false);
            }

            if (!SoapySdrNativo.EstaDisponible())
            {
                throw new InvalidOperationException(
                    "La librería SoapySDR no está disponible en el sistema. " +
                    "Instale SoapySDR y los módulos correspondientes a su dispositivo.");
            }

            _logger.LogInformation("Conectando al dispositivo SDR: {Dispositivo}...", dispositivo);

            _dispositivo = SoapySdrNativo.SoapySDRDevice_makeStrArgs(dispositivo);
            if (_dispositivo == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"No se pudo crear el dispositivo SDR con argumentos: '{dispositivo}'.");
            }

            DispositivoActual = dispositivo;

            // Configurar parámetros iniciales
            ConfigurarParametrosIniciales();

            // Configurar stream RX
            _stream = SoapySdrNativo.SoapySDRDevice_setupStream(
                _dispositivo,
                SoapySdrNativo.DireccionRx,
                SoapySdrNativo.FormatoComplejo32,
                IntPtr.Zero,
                0,
                IntPtr.Zero);

            if (_stream == IntPtr.Zero)
            {
                SoapySdrNativo.SoapySDRDevice_unmake(_dispositivo);
                _dispositivo = IntPtr.Zero;
                throw new InvalidOperationException("No se pudo configurar el stream de recepción SDR.");
            }

            int resultadoActivar = SoapySdrNativo.SoapySDRDevice_activateStream(
                _dispositivo, _stream, 0, 0, 0);

            if (resultadoActivar != 0)
            {
                SoapySdrNativo.SoapySDRDevice_closeStream(_dispositivo, _stream);
                SoapySdrNativo.SoapySDRDevice_unmake(_dispositivo);
                _stream = IntPtr.Zero;
                _dispositivo = IntPtr.Zero;
                throw new InvalidOperationException(
                    $"No se pudo activar el stream de recepción SDR. Código de error: {resultadoActivar}.");
            }

            EstaConectado = true;
            IniciarHiloLectura();

            _logger.LogInformation(
                "Dispositivo SDR conectado: {Dispositivo}. Frecuencia: {Frecuencia} Hz, Tasa: {Tasa} Hz.",
                dispositivo, FrecuenciaCentralHz, TasaDeMuestreoHz);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <inheritdoc />
    public async Task DesconectarAsync(CancellationToken ct = default)
    {
        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await DesconectarInternoAsync().ConfigureAwait(false);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <inheritdoc />
    public async Task ConfigurarFrecuenciaAsync(double frecuenciaHz, CancellationToken ct = default)
    {
        VerificarConectado();

        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            int resultado = SoapySdrNativo.SoapySDRDevice_setFrequency(
                _dispositivo, SoapySdrNativo.DireccionRx, 0, frecuenciaHz, IntPtr.Zero);

            if (resultado != 0)
            {
                throw new InvalidOperationException(
                    $"Error al configurar frecuencia a {frecuenciaHz} Hz. Código: {resultado}.");
            }

            FrecuenciaCentralHz = frecuenciaHz;
            _logger.LogDebug("Frecuencia SDR configurada a {Frecuencia} Hz.", frecuenciaHz);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <inheritdoc />
    public async Task ConfigurarGananciaAsync(double gananciaDb, CancellationToken ct = default)
    {
        VerificarConectado();

        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            int resultado = SoapySdrNativo.SoapySDRDevice_setGain(
                _dispositivo, SoapySdrNativo.DireccionRx, 0, gananciaDb);

            if (resultado != 0)
            {
                throw new InvalidOperationException(
                    $"Error al configurar ganancia a {gananciaDb} dB. Código: {resultado}.");
            }

            GananciaDb = gananciaDb;
            _logger.LogDebug("Ganancia SDR configurada a {Ganancia} dB.", gananciaDb);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <inheritdoc />
    public async Task ConfigurarAnchoDeBandaAsync(double anchoBandaHz, CancellationToken ct = default)
    {
        VerificarConectado();

        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            int resultado = SoapySdrNativo.SoapySDRDevice_setBandwidth(
                _dispositivo, SoapySdrNativo.DireccionRx, 0, anchoBandaHz);

            if (resultado != 0)
            {
                throw new InvalidOperationException(
                    $"Error al configurar ancho de banda a {anchoBandaHz} Hz. Código: {resultado}.");
            }

            AnchoDeBandaHz = anchoBandaHz;
            _logger.LogDebug("Ancho de banda SDR configurado a {AnchoBanda} Hz.", anchoBandaHz);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<DispositivoSdr> ObtenerDispositivosDisponibles()
    {
        List<DispositivoSdr> dispositivos = new();

        if (!SoapySdrNativo.EstaDisponible())
        {
            _logger.LogWarning("SoapySDR no está disponible. No se pueden enumerar dispositivos.");
            return dispositivos.AsReadOnly();
        }

        IntPtr punteroLista = IntPtr.Zero;
        nint cantidad = 0;

        try
        {
            punteroLista = SoapySdrNativo.SoapySDRDevice_enumerate(IntPtr.Zero, out cantidad);

            if (punteroLista == IntPtr.Zero || cantidad <= 0)
            {
                _logger.LogInformation("No se encontraron dispositivos SDR.");
                return dispositivos.AsReadOnly();
            }

            _logger.LogInformation("Se encontraron {Cantidad} dispositivo(s) SDR.", cantidad);

            // Nota: La estructura SoapySDRKwargs es específica de cada plataforma.
            // En un escenario real se parseaían las kwargs para extraer la info.
            // Por ahora retornamos información básica basada en la enumeración.
            for (nint i = 0; i < cantidad; i++)
            {
                DispositivoSdr dispositivoSdr = new(
                    Nombre: $"Dispositivo SDR #{i}",
                    Controlador: "soapysdr",
                    NumeroSerie: null,
                    Argumentos: new Dictionary<string, string>());

                dispositivos.Add(dispositivoSdr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enumerar dispositivos SDR.");
        }
        finally
        {
            if (punteroLista != IntPtr.Zero && cantidad > 0)
            {
                SoapySdrNativo.SoapySDRKwargsList_clear(punteroLista, cantidad);
            }
        }

        return dispositivos.AsReadOnly();
    }

    /// <summary>
    /// Configura los parámetros iniciales del dispositivo (frecuencia, tasa, ganancia, ancho de banda).
    /// </summary>
    private void ConfigurarParametrosIniciales()
    {
        SoapySdrNativo.SoapySDRDevice_setSampleRate(
            _dispositivo, SoapySdrNativo.DireccionRx, 0, _configuracion.TasaDeMuestreoHz);
        TasaDeMuestreoHz = _configuracion.TasaDeMuestreoHz;

        SoapySdrNativo.SoapySDRDevice_setFrequency(
            _dispositivo, SoapySdrNativo.DireccionRx, 0, _configuracion.FrecuenciaCentralHz, IntPtr.Zero);
        FrecuenciaCentralHz = _configuracion.FrecuenciaCentralHz;

        SoapySdrNativo.SoapySDRDevice_setGain(
            _dispositivo, SoapySdrNativo.DireccionRx, 0, _configuracion.GananciaDb);
        GananciaDb = _configuracion.GananciaDb;

        SoapySdrNativo.SoapySDRDevice_setBandwidth(
            _dispositivo, SoapySdrNativo.DireccionRx, 0, _configuracion.AnchoDeBandaHz);
        AnchoDeBandaHz = _configuracion.AnchoDeBandaHz;
    }

    /// <summary>
    /// Inicia el hilo dedicado de lectura continua de muestras IQ.
    /// </summary>
    private void IniciarHiloLectura()
    {
        _ctsLectura = new CancellationTokenSource();
        CancellationToken token = _ctsLectura.Token;

        _hiloLectura = new Thread(() => BucleLectura(token))
        {
            Name = "SoapySDR-Lectura-IQ",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };

        _hiloLectura.Start();
        _logger.LogDebug("Hilo de lectura SDR iniciado.");
    }

    /// <summary>
    /// Bucle principal de lectura de muestras IQ desde el dispositivo SDR.
    /// Se ejecuta en un hilo dedicado hasta que se solicita la cancelación.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    private void BucleLectura(CancellationToken ct)
    {
        int tamanoBuffer = _configuracion.TamanoBufferMuestras;
        // CF32 = pares de float (I, Q), cada par son 2 floats = 8 bytes
        int tamanoBytes = tamanoBuffer * 2 * sizeof(float);
        IntPtr bufferNativo = Marshal.AllocHGlobal(tamanoBytes);

        try
        {
            IntPtr[] buffers = new IntPtr[] { bufferNativo };

            while (!ct.IsCancellationRequested)
            {
                int muestrasLeidas = SoapySdrNativo.SoapySDRDevice_readStream(
                    _dispositivo, _stream, buffers,
                    tamanoBuffer, out int flags, out long tiempoNs,
                    SoapySdrNativo.TimeoutLecturaUs);

                if (muestrasLeidas <= 0)
                {
                    if (muestrasLeidas < 0)
                    {
                        _logger.LogWarning("Error en lectura SDR. Código: {Codigo}.", muestrasLeidas);
                    }
                    continue;
                }

                // Convertir buffer nativo CF32 a arrays double[] de I y Q
                double[] muestrasI = new double[muestrasLeidas];
                double[] muestrasQ = new double[muestrasLeidas];

                unsafe
                {
                    float* pBuffer = (float*)bufferNativo;
                    for (int i = 0; i < muestrasLeidas; i++)
                    {
                        muestrasI[i] = pBuffer[i * 2];
                        muestrasQ[i] = pBuffer[i * 2 + 1];
                    }
                }

                MuestrasSdrEventArgs args = new()
                {
                    MuestrasI = muestrasI,
                    MuestrasQ = muestrasQ,
                    FrecuenciaCentralHz = FrecuenciaCentralHz,
                    Marca = DateTimeOffset.UtcNow
                };

                MuestrasRecibidas?.Invoke(this, args);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelación normal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fatal en el bucle de lectura SDR.");
        }
        finally
        {
            Marshal.FreeHGlobal(bufferNativo);
            _logger.LogDebug("Hilo de lectura SDR finalizado.");
        }
    }

    /// <summary>
    /// Verifica que el receptor esté conectado antes de ejecutar una operación.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si el receptor no está conectado.</exception>
    private void VerificarConectado()
    {
        if (!EstaConectado)
        {
            throw new InvalidOperationException(
                "El receptor SDR no está conectado. Llame a ConectarAsync primero.");
        }
    }

    /// <summary>
    /// Desconecta del dispositivo SDR sin adquirir el semáforo (uso interno).
    /// </summary>
    private Task DesconectarInternoAsync()
    {
        DesconectarInterno();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Desconecta del dispositivo SDR sin adquirir el semáforo (uso interno, síncrono).
    /// Las operaciones son llamadas nativas síncronas; no hay E/S async real.
    /// </summary>
    private void DesconectarInterno()
    {
        if (!EstaConectado)
        {
            return;
        }

        _logger.LogInformation("Desconectando dispositivo SDR: {Dispositivo}...", DispositivoActual);

        // Detener hilo de lectura
        _ctsLectura?.Cancel();
        _hiloLectura?.Join(TimeSpan.FromSeconds(3));
        _ctsLectura?.Dispose();
        _ctsLectura = null;
        _hiloLectura = null;

        // Liberar recursos nativos
        if (_stream != IntPtr.Zero)
        {
            SoapySdrNativo.SoapySDRDevice_deactivateStream(_dispositivo, _stream, 0, 0);
            SoapySdrNativo.SoapySDRDevice_closeStream(_dispositivo, _stream);
            _stream = IntPtr.Zero;
        }

        if (_dispositivo != IntPtr.Zero)
        {
            SoapySdrNativo.SoapySDRDevice_unmake(_dispositivo);
            _dispositivo = IntPtr.Zero;
        }

        EstaConectado = false;
        string? dispositivoAnterior = DispositivoActual;
        DispositivoActual = null;
        FrecuenciaCentralHz = 0;
        AnchoDeBandaHz = 0;
        GananciaDb = 0;
        TasaDeMuestreoHz = 0;

        _logger.LogInformation("Dispositivo SDR desconectado: {Dispositivo}.", dispositivoAnterior);
    }

    /// <summary>
    /// Libera todos los recursos nativos y managed del receptor SDR.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaforo.Wait();
        try
        {
            DesconectarInterno();
        }
        finally
        {
            _semaforo.Release();
            _semaforo.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
