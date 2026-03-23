using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using Serilog;

namespace RadioAficionado.Nativo.ModosDigitales.Ft8;

/// <summary>
/// Decodificador de modos digitales FT8 y FT4.
/// Implementa <see cref="IDecodificadorDigital"/> acumulando audio en un buffer circular
/// y decodificando cada ventana temporal (15s para FT8, 7.5s para FT4).
/// </summary>
public sealed class DecodificadorFt8 : IDecodificadorDigital
{
    private readonly ILogger _logger;
    private readonly ConfiguracionFt8 _configuracion;
    private readonly object _lockBuffer = new();
    private readonly object _lockEstado = new();

    private float[] _bufferAudio;
    private int _posicionBuffer;
    private int _muestrasEnBuffer;
    private bool _estaActivo;
    private bool _disposed;

    /// <summary>
    /// Modo de operación principal: FT8.
    /// </summary>
    public ModoOperacion Modo => ModoOperacion.FT8;

    /// <summary>
    /// Submodos soportados: FT4.
    /// </summary>
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } =
        new List<SubModoOperacion> { SubModoOperacion.Ninguno }.AsReadOnly();

    /// <summary>
    /// Indica si el decodificador está activo procesando audio.
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
    /// Evento que se dispara al decodificar un nuevo mensaje FT8.
    /// </summary>
    public event EventHandler<MensajeDecodificado>? MensajeDecodificadoRecibido;

    /// <summary>
    /// Crea una nueva instancia del decodificador FT8.
    /// </summary>
    /// <param name="logger">Logger de Serilog para registro de eventos.</param>
    /// <param name="configuracion">Configuración del decodificador. Si es null, usa valores por defecto.</param>
    public DecodificadorFt8(ILogger logger, ConfiguracionFt8? configuracion = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuracion = configuracion ?? new ConfiguracionFt8();

        int tamanoBuffer = (int)(_configuracion.TasaDeMuestreo * _configuracion.AnchoDeVentana);
        _bufferAudio = new float[tamanoBuffer];
        _posicionBuffer = 0;
        _muestrasEnBuffer = 0;

        _logger.Information(
            "DecodificadorFt8 creado. TasaMuestreo={TasaMuestreo}Hz, Ventana={Ventana}s, Buffer={Buffer} muestras",
            _configuracion.TasaDeMuestreo,
            _configuracion.AnchoDeVentana,
            tamanoBuffer);
    }

    /// <summary>
    /// Inicia el decodificador FT8.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    public Task IniciarAsync(CancellationToken ct = default)
    {
        lock (_lockEstado)
        {
            if (_estaActivo)
            {
                _logger.Warning("DecodificadorFt8 ya está activo, ignorando llamada a IniciarAsync");
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

        _logger.Information("DecodificadorFt8 iniciado");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Detiene el decodificador FT8.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    public Task DetenerAsync(CancellationToken ct = default)
    {
        lock (_lockEstado)
        {
            if (!_estaActivo)
            {
                _logger.Warning("DecodificadorFt8 ya está detenido, ignorando llamada a DetenerAsync");
                return Task.CompletedTask;
            }

            _estaActivo = false;
        }

        _logger.Information("DecodificadorFt8 detenido");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Procesa un bloque de muestras de audio PCM de 16 bits.
    /// Las muestras se acumulan en el buffer circular y se decodifican cuando
    /// se completa una ventana temporal de 15 segundos.
    /// </summary>
    /// <param name="muestra">Muestra de audio a procesar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de mensajes decodificados (puede estar vacía).</returns>
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
                // Convertir PCM 16-bit a float normalizado [-1.0, 1.0]
                _bufferAudio[_posicionBuffer] = datos[i] / 32768.0f;
                _posicionBuffer = (_posicionBuffer + 1) % _bufferAudio.Length;
                _muestrasEnBuffer = Math.Min(_muestrasEnBuffer + 1, _bufferAudio.Length);
            }

            if (_muestrasEnBuffer >= _bufferAudio.Length)
            {
                bufferCompleto = true;
            }

            // Crear copia del buffer para decodificación fuera del lock
            bufferParaDecodificar = new float[_bufferAudio.Length];
            Array.Copy(_bufferAudio, bufferParaDecodificar, _bufferAudio.Length);
        }

        if (!bufferCompleto)
        {
            return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(Array.Empty<MensajeDecodificado>());
        }

        // Intentar decodificar con la librería nativa
        List<MensajeDecodificado> resultados = DecodificarBuffer(bufferParaDecodificar, muestra.MarcaDeTiempo);

        // Reiniciar buffer después de decodificación
        lock (_lockBuffer)
        {
            _posicionBuffer = 0;
            _muestrasEnBuffer = 0;
            Array.Clear(_bufferAudio, 0, _bufferAudio.Length);
        }

        // Disparar eventos para cada mensaje decodificado
        foreach (MensajeDecodificado mensaje in resultados)
        {
            MensajeDecodificadoRecibido?.Invoke(this, mensaje);
        }

        return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(resultados.AsReadOnly());
    }

    /// <summary>
    /// Filtra los mensajes donde aparece el indicativo especificado,
    /// ya sea como emisor o como receptor.
    /// </summary>
    /// <param name="miIndicativo">Indicativo propio a buscar.</param>
    /// <param name="mensajes">Lista de mensajes a filtrar.</param>
    /// <returns>Mensajes donde aparece el indicativo especificado.</returns>
    public IReadOnlyList<MensajeFt8> DetectarMisQsos(string miIndicativo, IReadOnlyList<MensajeFt8> mensajes)
    {
        if (string.IsNullOrWhiteSpace(miIndicativo))
        {
            return Array.Empty<MensajeFt8>();
        }

        if (mensajes is null || mensajes.Count == 0)
        {
            return Array.Empty<MensajeFt8>();
        }

        string indicativoNormalizado = miIndicativo.ToUpperInvariant();

        List<MensajeFt8> misQsos = new();

        foreach (MensajeFt8 mensaje in mensajes)
        {
            bool coincideEmisor = string.Equals(
                mensaje.IndicativoEmisor, indicativoNormalizado, StringComparison.OrdinalIgnoreCase);

            bool coincideReceptor = string.Equals(
                mensaje.IndicativoReceptor, indicativoNormalizado, StringComparison.OrdinalIgnoreCase);

            if (coincideEmisor || coincideReceptor)
            {
                misQsos.Add(mensaje);
            }
        }

        return misQsos.AsReadOnly();
    }

    private List<MensajeDecodificado> DecodificarBuffer(float[] buffer, DateTimeOffset marcaDeTiempo)
    {
        List<MensajeDecodificado> resultados = new();

        if (!Ft8Nativo.EstaDisponible())
        {
            _logger.Warning(
                "Librería nativa ft8_lib no disponible. Decodificación FT8 no es posible.");
            return resultados;
        }

        try
        {
            const int maximoMensajes = 50;
            Ft8MensajeNativo[] mensajesNativos = new Ft8MensajeNativo[maximoMensajes];

            int cantidad = Ft8Nativo.Decodificar(buffer, buffer.Length, mensajesNativos, maximoMensajes);

            _logger.Debug("ft8_decode retornó {Cantidad} mensajes", cantidad);

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

                MensajeFt8 mensajeFt8 = MensajeFt8.ParsearMensaje(
                    nativo.Texto,
                    nativo.FrecuenciaHz,
                    nativo.DeltaTiempoMs / 1000.0,
                    nativo.Snr,
                    marcaDeTiempo);

                MensajeDecodificado mensajeDecodificado = new(
                    marcaDeTiempo: marcaDeTiempo,
                    frecuenciaAudioHz: nativo.FrecuenciaHz,
                    snr: nativo.Snr,
                    deltaTiempo: nativo.DeltaTiempoMs / 1000.0,
                    modo: ModoOperacion.FT8,
                    texto: nativo.Texto,
                    indicativoEmisor: mensajeFt8.IndicativoEmisor,
                    indicativoDestinatario: mensajeFt8.IndicativoReceptor,
                    localizador: mensajeFt8.Localizador,
                    reporteSenal: mensajeFt8.ReporteSenal?.ToString());

                resultados.Add(mensajeDecodificado);
            }
        }
        catch (DllNotFoundException ex)
        {
            _logger.Error(ex, "No se encontró la librería nativa ft8_lib");
        }
        catch (EntryPointNotFoundException ex)
        {
            _logger.Error(ex, "Punto de entrada no encontrado en ft8_lib");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error inesperado durante la decodificación FT8");
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
        _logger.Information("DecodificadorFt8 disposed");
    }
}
