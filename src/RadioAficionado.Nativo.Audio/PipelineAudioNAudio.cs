using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using NAudio.Wave;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Nativo.Audio;

/// <summary>
/// Métodos P/Invoke para enumerar dispositivos de salida de audio WinMM.
/// NAudio no expone WaveOut.DeviceCount en el paquete principal,
/// por lo que accedemos directamente a la API de Windows.
/// </summary>
internal static class InteropWinMM
{
    /// <summary>
    /// Obtiene la cantidad de dispositivos de salida de audio disponibles.
    /// </summary>
    [DllImport("winmm.dll")]
    internal static extern int waveOutGetNumDevs();

    /// <summary>
    /// Obtiene las capacidades de un dispositivo de salida de audio.
    /// </summary>
    [DllImport("winmm.dll", EntryPoint = "waveOutGetDevCapsW", CharSet = CharSet.Unicode)]
    internal static extern int waveOutGetDevCapsW(
        nint uDeviceID,
        ref CapacidadesDispositivoSalida pwoc,
        int cbwoc);
}

/// <summary>
/// Estructura que representa las capacidades de un dispositivo de salida WinMM.
/// Equivalente a WAVEOUTCAPS de la API de Windows.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct CapacidadesDispositivoSalida
{
    /// <summary>Fabricante del dispositivo.</summary>
    public ushort wMid;

    /// <summary>Identificador del producto.</summary>
    public ushort wPid;

    /// <summary>Versión del driver.</summary>
    public uint vDriverVersion;

    /// <summary>Nombre del producto (máximo 32 caracteres).</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szPname;

    /// <summary>Formatos soportados.</summary>
    public uint dwFormats;

    /// <summary>Número de canales.</summary>
    public ushort wChannels;

    /// <summary>Reservado.</summary>
    public ushort wReserved1;

    /// <summary>Funcionalidades opcionales soportadas.</summary>
    public uint dwSupport;
}

/// <summary>
/// Implementación del pipeline de audio usando NAudio.
/// Captura audio del hardware y lo distribuye a múltiples consumidores.
/// En plataformas que no sean Windows, <see cref="ObtenerDispositivosAsync"/> devuelve una lista vacía
/// y los métodos de captura/transmisión lanzan <see cref="PlatformNotSupportedException"/>.
/// </summary>
public sealed class PipelineAudioNAudio : IAudioPipeline
{
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _proveedorBuffer;
    private readonly ConcurrentDictionary<Guid, Action<MuestraAudio>> _suscriptores = new();
    private int _tasaDeMuestreoHz;
    private bool _estaActivo;
    private bool _dispuesto;

    /// <inheritdoc />
    public bool EstaActivo => _estaActivo;

    /// <inheritdoc />
    public int TasaDeMuestreoHz => _tasaDeMuestreoHz;

    /// <summary>
    /// Inicia la captura de audio desde el dispositivo especificado.
    /// </summary>
    /// <param name="dispositivoId">
    /// Identificador del dispositivo en formato "in:N" donde N es el número de dispositivo NAudio.
    /// </param>
    /// <param name="tasaDeMuestreoHz">Tasa de muestreo deseada (default: 12000 Hz para modos digitales).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <exception cref="PlatformNotSupportedException">Se lanza en plataformas que no sean Windows.</exception>
    /// <exception cref="InvalidOperationException">Se lanza si el pipeline ya está activo.</exception>
    /// <exception cref="ArgumentException">Se lanza si el formato de dispositivoId no es válido.</exception>
    public Task IniciarCapturaAsync(string dispositivoId, int tasaDeMuestreoHz = 12000, CancellationToken ct = default)
    {
        ValidarPlataformaWindows();
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        if (_estaActivo)
        {
            throw new InvalidOperationException("El pipeline de audio ya está activo. Detenga la captura antes de iniciar una nueva.");
        }

        if (string.IsNullOrWhiteSpace(dispositivoId))
        {
            throw new ArgumentException("El identificador de dispositivo no puede estar vacío.", nameof(dispositivoId));
        }

        int numeroDispositivo = ParsearDispositivoEntrada(dispositivoId);
        _tasaDeMuestreoHz = tasaDeMuestreoHz;

        WaveFormat formato = new WaveFormat(tasaDeMuestreoHz, 16, 1);

        _waveIn = new WaveInEvent
        {
            DeviceNumber = numeroDispositivo,
            WaveFormat = formato,
            BufferMilliseconds = 100
        };

        _waveIn.DataAvailable += AlRecibirDatos;
        _waveIn.RecordingStopped += AlDetenerGrabacion;
        _waveIn.StartRecording();
        _estaActivo = true;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Detiene la captura de audio activa.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    public Task DetenerCapturaAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        if (_waveIn is not null)
        {
            _waveIn.StopRecording();
            _waveIn.DataAvailable -= AlRecibirDatos;
            _waveIn.RecordingStopped -= AlDetenerGrabacion;
            _waveIn.Dispose();
            _waveIn = null;
        }

        _estaActivo = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Guid Suscribir(Action<MuestraAudio> consumidor)
    {
        ArgumentNullException.ThrowIfNull(consumidor);
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        Guid id = Guid.NewGuid();
        _suscriptores.TryAdd(id, consumidor);
        return id;
    }

    /// <inheritdoc />
    public void Desuscribir(Guid suscripcionId)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);
        _suscriptores.TryRemove(suscripcionId, out _);
    }

    /// <summary>
    /// Obtiene la lista de dispositivos de audio disponibles en el sistema.
    /// En plataformas que no sean Windows, devuelve una lista vacía.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de dispositivos de audio disponibles.</returns>
    public Task<IReadOnlyList<DispositivoAudio>> ObtenerDispositivosAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        List<DispositivoAudio> dispositivos = new();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Task.FromResult<IReadOnlyList<DispositivoAudio>>(dispositivos.AsReadOnly());
        }

        int cantidadEntradas = WaveInEvent.DeviceCount;
        for (int i = 0; i < cantidadEntradas; i++)
        {
            WaveInCapabilities capacidades = WaveInEvent.GetCapabilities(i);
            DispositivoAudio dispositivo = new DispositivoAudio(
                id: $"in:{i}",
                nombre: capacidades.ProductName,
                esEntrada: true,
                esSalida: false
            );
            dispositivos.Add(dispositivo);
        }

        int cantidadSalidas = InteropWinMM.waveOutGetNumDevs();
        for (int i = 0; i < cantidadSalidas; i++)
        {
            CapacidadesDispositivoSalida capacidades = new CapacidadesDispositivoSalida();
            int tamanioEstructura = Marshal.SizeOf<CapacidadesDispositivoSalida>();
            int resultado = InteropWinMM.waveOutGetDevCapsW((nint)i, ref capacidades, tamanioEstructura);

            string nombreDispositivo = resultado == 0
                ? capacidades.szPname
                : $"Dispositivo de salida {i}";

            DispositivoAudio dispositivo = new DispositivoAudio(
                id: $"out:{i}",
                nombre: nombreDispositivo,
                esEntrada: false,
                esSalida: true
            );
            dispositivos.Add(dispositivo);
        }

        return Task.FromResult<IReadOnlyList<DispositivoAudio>>(dispositivos.AsReadOnly());
    }

    /// <summary>
    /// Envía audio al dispositivo de salida para transmisión.
    /// </summary>
    /// <param name="datos">Datos de audio PCM de 16 bits.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <exception cref="PlatformNotSupportedException">Se lanza en plataformas que no sean Windows.</exception>
    public Task TransmitirAudioAsync(ReadOnlyMemory<short> datos, CancellationToken ct = default)
    {
        ValidarPlataformaWindows();
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        if (datos.IsEmpty)
        {
            return Task.CompletedTask;
        }

        if (_waveOut is null)
        {
            WaveFormat formato = new WaveFormat(_tasaDeMuestreoHz > 0 ? _tasaDeMuestreoHz : 12000, 16, 1);
            _proveedorBuffer = new BufferedWaveProvider(formato)
            {
                DiscardOnBufferOverflow = true
            };

            _waveOut = new WaveOutEvent();
            _waveOut.Init(_proveedorBuffer);
            _waveOut.Play();
        }

        byte[] bytesAudio = ConvertirShortsABytes(datos.Span);
        _proveedorBuffer!.AddSamples(bytesAudio, 0, bytesAudio.Length);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Libera todos los recursos de audio de forma asíncrona.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_dispuesto)
        {
            return;
        }

        _dispuesto = true;

        if (_estaActivo)
        {
            await DetenerCapturaAsync();
        }

        if (_waveOut is not null)
        {
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }

        _proveedorBuffer = null;
        _suscriptores.Clear();
    }

    /// <summary>
    /// Manejador del evento DataAvailable de NAudio.
    /// Convierte los bytes PCM a shorts y notifica a todos los suscriptores.
    /// </summary>
    private void AlRecibirDatos(object? remitente, WaveInEventArgs argumentos)
    {
        if (argumentos.BytesRecorded == 0 || _suscriptores.IsEmpty)
        {
            return;
        }

        int cantidadMuestras = argumentos.BytesRecorded / 2;
        short[] muestras = new short[cantidadMuestras];
        Buffer.BlockCopy(argumentos.Buffer, 0, muestras, 0, argumentos.BytesRecorded);

        MuestraAudio muestra = new MuestraAudio(
            datos: new ReadOnlyMemory<short>(muestras),
            tasaDeMuestreoHz: _tasaDeMuestreoHz,
            marcaDeTiempo: DateTimeOffset.UtcNow
        );

        foreach (KeyValuePair<Guid, Action<MuestraAudio>> suscriptor in _suscriptores)
        {
            try
            {
                suscriptor.Value.Invoke(muestra);
            }
            catch (Exception)
            {
                // No propagamos excepciones de suscriptores para no interrumpir la captura.
                // TODO: Considerar agregar logging con Serilog cuando se integre DI.
            }
        }
    }

    /// <summary>
    /// Manejador del evento RecordingStopped de NAudio.
    /// </summary>
    private void AlDetenerGrabacion(object? remitente, StoppedEventArgs argumentos)
    {
        _estaActivo = false;
    }

    /// <summary>
    /// Parsea el identificador de dispositivo de entrada en formato "in:N".
    /// </summary>
    /// <param name="dispositivoId">Identificador en formato "in:N".</param>
    /// <returns>Número de dispositivo NAudio.</returns>
    /// <exception cref="ArgumentException">Si el formato no es válido.</exception>
    private static int ParsearDispositivoEntrada(string dispositivoId)
    {
        if (!dispositivoId.StartsWith("in:", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"El identificador de dispositivo de entrada debe tener formato 'in:N'. Recibido: '{dispositivoId}'.",
                nameof(dispositivoId));
        }

        string numeroParte = dispositivoId[3..];

        if (!int.TryParse(numeroParte, out int numero) || numero < 0)
        {
            throw new ArgumentException(
                $"El número de dispositivo debe ser un entero no negativo. Recibido: '{numeroParte}'.",
                nameof(dispositivoId));
        }

        return numero;
    }

    /// <summary>
    /// Convierte un span de shorts (PCM 16-bit) a un arreglo de bytes.
    /// </summary>
    private static byte[] ConvertirShortsABytes(ReadOnlySpan<short> muestras)
    {
        byte[] bytes = new byte[muestras.Length * 2];
        Buffer.BlockCopy(muestras.ToArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Valida que la plataforma actual sea Windows.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Si no se ejecuta en Windows.</exception>
    private static void ValidarPlataformaWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "La captura de audio con NAudio solo está disponible en Windows. " +
                "Para soporte multiplataforma se requiere una implementación con PortAudio.");
        }
    }
}
