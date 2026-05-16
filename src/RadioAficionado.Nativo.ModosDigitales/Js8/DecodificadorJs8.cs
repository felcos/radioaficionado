using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Ft8;
using Serilog;

namespace RadioAficionado.Nativo.ModosDigitales.Js8;

/// <summary>
/// Decodificador de JS8Call que implementa <see cref="IDecodificadorDigital"/>.
/// JS8Call es un modo de mensajeria keyboard-to-keyboard basado en la modulacion MFSK de FT8.
/// La ventana temporal es configurable segun la velocidad: Normal(15s), Rapido(10s), Lento(30s), Turbo(6s).
/// Reutiliza la libreria nativa ft8_lib para la decodificacion de la capa fisica.
/// </summary>
public sealed class DecodificadorJs8 : IDecodificadorDigital
{
    private readonly ILogger _logger;
    private readonly ConfiguracionJs8 _configuracion;
    private readonly object _lockBuffer = new();
    private readonly object _lockEstado = new();

    private float[] _bufferAudio;
    private int _posicionBuffer;
    private int _muestrasEnBuffer;
    private bool _estaActivo;
    private bool _disposed;

    /// <summary>
    /// Modo de operacion: JS8 (usa modulacion MFSK).
    /// </summary>
    public ModoOperacion Modo => ModoOperacion.JS8;

    /// <summary>
    /// Submodos soportados: ninguno.
    /// </summary>
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } =
        new List<SubModoOperacion> { SubModoOperacion.Ninguno }.AsReadOnly();

    /// <summary>
    /// Indica si el decodificador esta activo procesando audio.
    /// </summary>
    public bool EstaActivo
    {
        get
        {
            lock (_lockEstado)
            {
                return _estaActivo;
            }
        }
    }

    /// <summary>
    /// Tasa de muestreo requerida: 12000 Hz.
    /// </summary>
    public int TasaDeMuestreoRequeridaHz => _configuracion.TasaDeMuestreo;

    /// <summary>
    /// Evento que se dispara al decodificar un nuevo mensaje JS8.
    /// </summary>
    public event EventHandler<MensajeDecodificado>? MensajeDecodificadoRecibido;

    /// <summary>
    /// Crea una nueva instancia del decodificador JS8Call.
    /// </summary>
    /// <param name="logger">Logger de Serilog para registro de eventos.</param>
    /// <param name="configuracion">Configuracion del decodificador. Si es null, usa valores por defecto.</param>
    public DecodificadorJs8(ILogger logger, ConfiguracionJs8? configuracion = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuracion = configuracion ?? new ConfiguracionJs8();

        double anchoVentana = _configuracion.ObtenerAnchoDeVentana();
        int tamanoBuffer = (int)(_configuracion.TasaDeMuestreo * anchoVentana);
        _bufferAudio = new float[tamanoBuffer];
        _posicionBuffer = 0;
        _muestrasEnBuffer = 0;

        _logger.Information(
            "DecodificadorJs8 creado. Velocidad={Velocidad}, Ventana={Ventana}s, Buffer={Buffer} muestras",
            _configuracion.Velocidad,
            anchoVentana,
            tamanoBuffer);
    }

    /// <summary>
    /// Inicia el decodificador JS8.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    public Task IniciarAsync(CancellationToken ct = default)
    {
        lock (_lockEstado)
        {
            if (_estaActivo)
            {
                _logger.Warning("DecodificadorJs8 ya esta activo, ignorando llamada a IniciarAsync");
                return Task.CompletedTask;
            }

            _estaActivo = true;
        }

        lock (_lockBuffer)
        {
            _posicionBuffer = 0;
            _muestrasEnBuffer = 0;
            Array.Clear(_bufferAudio, 0, _bufferAudio.Length);
        }

        _logger.Information("DecodificadorJs8 iniciado");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Detiene el decodificador JS8.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    public Task DetenerAsync(CancellationToken ct = default)
    {
        lock (_lockEstado)
        {
            if (!_estaActivo)
            {
                _logger.Warning("DecodificadorJs8 ya esta detenido, ignorando llamada a DetenerAsync");
                return Task.CompletedTask;
            }

            _estaActivo = false;
        }

        _logger.Information("DecodificadorJs8 detenido");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Procesa un bloque de muestras de audio PCM de 16 bits.
    /// Las muestras se acumulan en el buffer circular y se decodifican cuando
    /// se completa la ventana temporal segun la velocidad configurada.
    /// </summary>
    /// <param name="muestra">Muestra de audio a procesar.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Lista de mensajes decodificados (puede estar vacia).</returns>
    public Task<IReadOnlyList<MensajeDecodificado>> ProcesarAudioAsync(MuestraAudio muestra, CancellationToken ct = default)
    {
        if (!EstaActivo)
        {
            return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(Array.Empty<MensajeDecodificado>());
        }

        if (muestra.Datos.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(Array.Empty<MensajeDecodificado>());
        }

        bool bufferCompleto = false;
        float[] bufferParaDecodificar;

        lock (_lockBuffer)
        {
            ReadOnlySpan<short> datos = muestra.Datos.Span;

            for (int i = 0; i < datos.Length; i++)
            {
                _bufferAudio[_posicionBuffer] = datos[i] / 32768.0f;
                _posicionBuffer = (_posicionBuffer + 1) % _bufferAudio.Length;
                _muestrasEnBuffer = Math.Min(_muestrasEnBuffer + 1, _bufferAudio.Length);
            }

            if (_muestrasEnBuffer >= _bufferAudio.Length)
            {
                bufferCompleto = true;
            }

            bufferParaDecodificar = new float[_bufferAudio.Length];
            Array.Copy(_bufferAudio, bufferParaDecodificar, _bufferAudio.Length);
        }

        if (!bufferCompleto)
        {
            return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(Array.Empty<MensajeDecodificado>());
        }

        List<MensajeDecodificado> resultados = DecodificarBuffer(bufferParaDecodificar, muestra.MarcaDeTiempo);

        lock (_lockBuffer)
        {
            _posicionBuffer = 0;
            _muestrasEnBuffer = 0;
            Array.Clear(_bufferAudio, 0, _bufferAudio.Length);
        }

        foreach (MensajeDecodificado mensaje in resultados)
        {
            MensajeDecodificadoRecibido?.Invoke(this, mensaje);
        }

        return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(resultados.AsReadOnly());
    }

    private List<MensajeDecodificado> DecodificarBuffer(float[] buffer, DateTimeOffset marcaDeTiempo)
    {
        List<MensajeDecodificado> resultados = new();

        if (!Ft8Nativo.EstaDisponible())
        {
            _logger.Warning(
                "Libreria nativa ft8_lib no disponible. Decodificacion JS8 no es posible.");
            return resultados;
        }

        try
        {
            const int maximoMensajes = 50;
            Ft8MensajeNativo[] mensajesNativos = new Ft8MensajeNativo[maximoMensajes];

            int cantidad = Ft8Nativo.Decodificar(buffer, buffer.Length, mensajesNativos, maximoMensajes);

            _logger.Debug("ft8_decode (JS8) retorno {Cantidad} mensajes", cantidad);

            for (int i = 0; i < cantidad; i++)
            {
                Ft8MensajeNativo nativo = mensajesNativos[i];

                if (string.IsNullOrWhiteSpace(nativo.Texto))
                {
                    continue;
                }

                if (nativo.Snr < _configuracion.UmbralSnr)
                {
                    continue;
                }

                if (nativo.FrecuenciaHz < _configuracion.FrecuenciaAudioMinima ||
                    nativo.FrecuenciaHz > _configuracion.FrecuenciaAudioMaxima)
                {
                    continue;
                }

                MensajeDecodificado mensajeDecodificado = new(
                    marcaDeTiempo: marcaDeTiempo,
                    frecuenciaAudioHz: nativo.FrecuenciaHz,
                    snr: nativo.Snr,
                    deltaTiempo: nativo.DeltaTiempoMs / 1000.0,
                    modo: ModoOperacion.JS8,
                    texto: nativo.Texto);

                resultados.Add(mensajeDecodificado);
            }
        }
        catch (DllNotFoundException ex)
        {
            _logger.Error(ex, "No se encontro la libreria nativa ft8_lib");
        }
        catch (EntryPointNotFoundException ex)
        {
            _logger.Error(ex, "Punto de entrada no encontrado en ft8_lib");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error inesperado durante la decodificacion JS8");
        }

        return resultados;
    }

    /// <summary>
    /// Libera los recursos del decodificador.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockEstado)
        {
            _estaActivo = false;
        }

        _disposed = true;
        _logger.Information("DecodificadorJs8 disposed");
    }
}
