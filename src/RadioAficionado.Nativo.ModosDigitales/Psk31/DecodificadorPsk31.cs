using System.Text;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Psk31;

/// <summary>
/// Decodificador de PSK31 (Phase Shift Keying a 31.25 baudios) que implementa <see cref="IDecodificadorDigital"/>.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Multiplica la senal por portadoras de referencia I (coseno) y Q (seno).
/// 3. Integra sobre un periodo de bit (384 muestras a 12000 Hz / 31.25 baudios).
/// 4. Detecta cambios de fase: cambio de fase = bit 0, sin cambio = bit 1.
/// 5. Acumula bits y decodifica caracteres usando <see cref="TablaVaricode"/>.
/// </summary>
public sealed class DecodificadorPsk31 : IDecodificadorDigital
{
    private readonly ConfiguracionPsk31 _configuracion;
    private readonly StringBuilder _textoDecodificado = new();
    private readonly List<bool> _bitsVaricode = new();
    private readonly int _muestrasPorBit;

    // Estado de decodificacion
    private bool _estaActivo;
    private bool _dispuesto;

    // Acumuladores para deteccion de fase I/Q
    private double _acumuladorI;
    private double _acumuladorQ;
    private double _faseAnterior;
    private bool _faseAnteriorInicializada;
    private int _contadorMuestras;
    private int _indiceMuestra;

    // Contador de ceros consecutivos para separador Varicode
    private int _cerosConsecutivos;

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.PSK;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>
    {
        SubModoOperacion.PSK31
    }.AsReadOnly();

    /// <inheritdoc />
    public bool EstaActivo => _estaActivo;

    /// <inheritdoc />
    public int TasaDeMuestreoRequeridaHz => _configuracion.TasaDeMuestreo;

    /// <inheritdoc />
    public event EventHandler<MensajeDecodificado>? MensajeDecodificadoRecibido;

    /// <summary>
    /// Crea una nueva instancia del decodificador PSK31.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa la configuracion por defecto.</param>
    public DecodificadorPsk31(ConfiguracionPsk31? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionPsk31();
        _muestrasPorBit = (int)(_configuracion.TasaDeMuestreo / _configuracion.BaudRate);
    }

    /// <inheritdoc />
    public Task IniciarAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        _estaActivo = true;
        _textoDecodificado.Clear();
        _bitsVaricode.Clear();
        _acumuladorI = 0.0;
        _acumuladorQ = 0.0;
        _faseAnterior = 0.0;
        _faseAnteriorInicializada = false;
        _contadorMuestras = 0;
        _indiceMuestra = 0;
        _cerosConsecutivos = 0;

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
            double muestra_normalizada = datos[i] / 32768.0;

            // Mezcla con portadora de referencia (correlador I/Q)
            double angulo = 2.0 * Math.PI * _configuracion.FrecuenciaPortadora * _indiceMuestra / _configuracion.TasaDeMuestreo;
            _acumuladorI += muestra_normalizada * Math.Cos(angulo);
            _acumuladorQ += muestra_normalizada * Math.Sin(angulo);

            _indiceMuestra++;
            _contadorMuestras++;

            if (_contadorMuestras >= _muestrasPorBit)
            {
                // Calcular fase del periodo de bit actual
                double faseActual = Math.Atan2(_acumuladorQ, _acumuladorI);

                if (_faseAnteriorInicializada)
                {
                    // Calcular diferencia de fase
                    double diferenciaFase = Math.Abs(faseActual - _faseAnterior);

                    // Normalizar a [0, PI]
                    if (diferenciaFase > Math.PI)
                    {
                        diferenciaFase = 2.0 * Math.PI - diferenciaFase;
                    }

                    // Cambio de fase > PI/2 indica bit 0, sin cambio indica bit 1
                    bool esBitUno = diferenciaFase < Math.PI / 2.0;

                    ProcesarBitVaricode(esBitUno, muestra.MarcaDeTiempo, mensajes);
                }

                _faseAnterior = faseActual;
                _faseAnteriorInicializada = true;
                _acumuladorI = 0.0;
                _acumuladorQ = 0.0;
                _contadorMuestras = 0;
            }
        }

        return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(mensajes.AsReadOnly());
    }

    /// <summary>
    /// Procesa un bit decodificado y lo acumula para decodificacion Varicode.
    /// En Varicode, dos ceros consecutivos separan caracteres.
    /// </summary>
    private void ProcesarBitVaricode(bool esBitUno, DateTimeOffset marcaDeTiempo, List<MensajeDecodificado> mensajes)
    {
        if (esBitUno)
        {
            _cerosConsecutivos = 0;
            _bitsVaricode.Add(true);
        }
        else
        {
            _cerosConsecutivos++;

            if (_cerosConsecutivos >= 2 && _bitsVaricode.Count > 0)
            {
                // Separador detectado — decodificar caracter
                char? caracter = TablaVaricode.DecodificarVaricode(_bitsVaricode);
                if (caracter.HasValue)
                {
                    _textoDecodificado.Append(caracter.Value);

                    // Emitir mensaje cuando se acumula suficiente texto o se recibe salto de linea
                    if (caracter.Value == '\n' || _textoDecodificado.Length >= 80)
                    {
                        string texto = _textoDecodificado.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(texto))
                        {
                            MensajeDecodificado mensaje = new(
                                marcaDeTiempo: marcaDeTiempo,
                                frecuenciaAudioHz: (int)_configuracion.FrecuenciaPortadora,
                                snr: 0,
                                deltaTiempo: 0.0,
                                modo: ModoOperacion.PSK,
                                texto: texto,
                                subModo: SubModoOperacion.PSK31);

                            mensajes.Add(mensaje);
                            MensajeDecodificadoRecibido?.Invoke(this, mensaje);
                        }
                        _textoDecodificado.Clear();
                    }
                }

                _bitsVaricode.Clear();
                _cerosConsecutivos = 0;
            }
            else
            {
                _bitsVaricode.Add(false);
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
        _bitsVaricode.Clear();
    }
}
