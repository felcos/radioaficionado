using System.Text;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Cw;

namespace RadioAficionado.Nativo.ModosDigitales.Rtty;

/// <summary>
/// Decodificador de RTTY (Radio TeleType) que implementa <see cref="IDecodificadorDigital"/>.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Aplica filtros Goertzel duales para detectar tonos mark (2125 Hz) y space (2295 Hz).
/// 3. Compara magnitudes para determinar el bit actual (mark=1, space=0).
/// 4. Muestrea al centro de cada bit segun la tasa de baudios.
/// 5. Detecta bits de start/stop y extrae los 5 bits de datos Baudot.
/// 6. Decodifica usando <see cref="TablaBaudot"/>.
/// </summary>
public sealed class DecodificadorRtty : IDecodificadorDigital
{
    private readonly ConfiguracionRtty _configuracion;
    private readonly StringBuilder _textoDecodificado = new();
    private readonly int _muestrasPorBit;

    // Estado de decodificacion
    private bool _estaActivo;
    private bool _dispuesto;
    private bool _modoFiguras;

    // Buffer para acumular muestras de un bit completo
    private short[] _bufferBit;
    private int _posicionEnBufferBit;

    // Estado de la maquina de estados del protocolo Baudot
    private EstadoRtty _estado;
    private int _bitsRecibidos;
    private int _codigoActual;
    private int _contadorBitsParada;

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.RTTY;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>
    {
        SubModoOperacion.ASCI
    }.AsReadOnly();

    /// <inheritdoc />
    public bool EstaActivo => _estaActivo;

    /// <inheritdoc />
    public int TasaDeMuestreoRequeridaHz => _configuracion.TasaDeMuestreo;

    /// <inheritdoc />
    public event EventHandler<MensajeDecodificado>? MensajeDecodificadoRecibido;

    /// <summary>
    /// Crea una nueva instancia del decodificador RTTY.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa la configuracion por defecto.</param>
    public DecodificadorRtty(ConfiguracionRtty? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionRtty();
        _muestrasPorBit = (int)(_configuracion.TasaDeMuestreo / _configuracion.Baudios);
        _bufferBit = new short[_muestrasPorBit];
        _posicionEnBufferBit = 0;
        _estado = EstadoRtty.EsperandoStart;
    }

    /// <inheritdoc />
    public Task IniciarAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        _estaActivo = true;
        _textoDecodificado.Clear();
        _modoFiguras = false;
        _estado = EstadoRtty.EsperandoStart;
        _bitsRecibidos = 0;
        _codigoActual = 0;
        _contadorBitsParada = 0;
        _posicionEnBufferBit = 0;
        Array.Clear(_bufferBit, 0, _bufferBit.Length);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DetenerAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        _estaActivo = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MensajeDecodificado>> ProcesarAudioAsync(MuestraAudio muestra, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        if (!_estaActivo)
        {
            return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(Array.Empty<MensajeDecodificado>());
        }

        List<MensajeDecodificado> mensajes = new();
        ReadOnlySpan<short> datos = muestra.Datos.Span;

        for (int i = 0; i < datos.Length; i++)
        {
            _bufferBit[_posicionEnBufferBit] = datos[i];
            _posicionEnBufferBit++;

            if (_posicionEnBufferBit >= _muestrasPorBit)
            {
                bool esMark = DetectarMark(_bufferBit.AsSpan(0, _muestrasPorBit));
                ProcesarBit(esMark, muestra.MarcaDeTiempo, mensajes);
                _posicionEnBufferBit = 0;
            }
        }

        return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(mensajes.AsReadOnly());
    }

    /// <summary>
    /// Detecta si el contenido del buffer corresponde a un tono mark o space
    /// usando filtros Goertzel duales.
    /// </summary>
    private bool DetectarMark(ReadOnlySpan<short> muestras)
    {
        double magnitudMark = FiltroGoertzel.CalcularMagnitud(
            muestras, _configuracion.FrecuenciaMark, _configuracion.TasaDeMuestreo);

        double magnitudSpace = FiltroGoertzel.CalcularMagnitud(
            muestras, _configuracion.FrecuenciaSpace, _configuracion.TasaDeMuestreo);

        return magnitudMark > magnitudSpace;
    }

    /// <summary>
    /// Procesa un bit decodificado a traves de la maquina de estados Baudot.
    /// </summary>
    private void ProcesarBit(bool esMark, DateTimeOffset marcaDeTiempo, List<MensajeDecodificado> mensajes)
    {
        switch (_estado)
        {
            case EstadoRtty.EsperandoStart:
                if (!esMark) // Start bit es space (0)
                {
                    _estado = EstadoRtty.RecibiendoDatos;
                    _bitsRecibidos = 0;
                    _codigoActual = 0;
                }
                break;

            case EstadoRtty.RecibiendoDatos:
                if (esMark)
                {
                    _codigoActual |= (1 << _bitsRecibidos);
                }
                _bitsRecibidos++;

                if (_bitsRecibidos >= 5)
                {
                    _estado = EstadoRtty.EsperandoStop;
                    _contadorBitsParada = 0;
                }
                break;

            case EstadoRtty.EsperandoStop:
                _contadorBitsParada++;
                // Esperamos al menos 1 bit de parada (mark)
                if (_contadorBitsParada >= 1)
                {
                    ProcesarCodigoBaudot(_codigoActual, marcaDeTiempo, mensajes);
                    _estado = EstadoRtty.EsperandoStart;
                }
                break;
        }
    }

    /// <summary>
    /// Procesa un codigo Baudot completo, manejando cambios de modo y generando caracteres.
    /// </summary>
    private void ProcesarCodigoBaudot(int codigo, DateTimeOffset marcaDeTiempo, List<MensajeDecodificado> mensajes)
    {
        if (TablaBaudot.EsCambioAFiguras(codigo))
        {
            _modoFiguras = true;
            return;
        }

        if (TablaBaudot.EsCambioALetras(codigo))
        {
            _modoFiguras = false;
            return;
        }

        char? caracter = TablaBaudot.DecodificarCaracter(codigo, _modoFiguras);

        if (caracter.HasValue)
        {
            _textoDecodificado.Append(caracter.Value);

            // Emitir mensaje cuando se acumula un bloque de texto o se recibe retorno de carro
            if (caracter.Value == '\r' || _textoDecodificado.Length >= 80)
            {
                string texto = _textoDecodificado.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    MensajeDecodificado mensaje = new(
                        marcaDeTiempo: marcaDeTiempo,
                        frecuenciaAudioHz: (int)_configuracion.FrecuenciaMark,
                        snr: 0,
                        deltaTiempo: 0.0,
                        modo: ModoOperacion.RTTY,
                        texto: texto,
                        subModo: SubModoOperacion.ASCI);

                    mensajes.Add(mensaje);
                    MensajeDecodificadoRecibido?.Invoke(this, mensaje);
                }
                _textoDecodificado.Clear();
            }
        }
    }

    /// <summary>
    /// Obtiene el texto actualmente acumulado en el buffer de decodificacion (para diagnostico).
    /// </summary>
    /// <returns>Texto decodificado hasta el momento.</returns>
    public string ObtenerTextoEnBuffer()
    {
        return _textoDecodificado.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_dispuesto)
        {
            return;
        }

        _dispuesto = true;
        _estaActivo = false;
        _textoDecodificado.Clear();
    }
}

/// <summary>
/// Estados de la maquina de estados del decodificador RTTY.
/// </summary>
internal enum EstadoRtty
{
    /// <summary>Esperando bit de start (space).</summary>
    EsperandoStart,

    /// <summary>Recibiendo los 5 bits de datos.</summary>
    RecibiendoDatos,

    /// <summary>Esperando bits de parada (mark).</summary>
    EsperandoStop
}
