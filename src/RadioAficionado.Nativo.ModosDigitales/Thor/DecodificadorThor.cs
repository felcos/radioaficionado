using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Thor;

/// <summary>
/// THOR (FEC sobre IFK) — modo robusto basado en DominoEX con correccion de errores.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Acumula muestras para cada simbolo (duracion = 1 / BaudRate).
/// 3. Detecta tonos IFK usando filtros Goertzel multi-tono.
/// 4. Calcula la diferencia entre tono actual y anterior (Incremental Frequency Keying).
/// 5. Mapea la diferencia de tonos a caracteres ASCII.
/// </summary>
public sealed class DecodificadorThor : IDecodificadorDigital
{
    private readonly ConfiguracionThor _configuracion;
    private readonly object _lockBuffer = new();
    private readonly object _lockEstado = new();

    private float[] _bufferSimbolo;
    private int _posicionBuffer;
    private int _muestrasEnBuffer;
    private readonly List<int> _simbolosAcumulados = new();
    private int _tonoAnterior;
    private bool _estaActivo;
    private bool _dispuesto;

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.THOR;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>
    {
        SubModoOperacion.THOR4,
        SubModoOperacion.THOR8,
        SubModoOperacion.THOR16
    }.AsReadOnly();

    /// <inheritdoc />
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

    /// <inheritdoc />
    public int TasaDeMuestreoRequeridaHz => _configuracion.TasaDeMuestreo;

    /// <inheritdoc />
    public event EventHandler<MensajeDecodificado>? MensajeDecodificadoRecibido;

    /// <summary>
    /// Crea una nueva instancia del decodificador THOR.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa THOR16.</param>
    public DecodificadorThor(ConfiguracionThor? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionThor();

        int muestrasPorSimbolo = (int)(_configuracion.TasaDeMuestreo * _configuracion.TiempoSimboloSegundos);
        _bufferSimbolo = new float[muestrasPorSimbolo];
        _posicionBuffer = 0;
        _muestrasEnBuffer = 0;
        _tonoAnterior = -1;
    }

    /// <inheritdoc />
    public Task IniciarAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        lock (_lockEstado)
        {
            _estaActivo = true;
        }

        lock (_lockBuffer)
        {
            _posicionBuffer = 0;
            _muestrasEnBuffer = 0;
            _tonoAnterior = -1;
            _simbolosAcumulados.Clear();
            Array.Clear(_bufferSimbolo, 0, _bufferSimbolo.Length);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DetenerAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        lock (_lockEstado)
        {
            _estaActivo = false;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MensajeDecodificado>> ProcesarAudioAsync(MuestraAudio muestra, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_dispuesto, this);

        if (!EstaActivo)
        {
            return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(Array.Empty<MensajeDecodificado>());
        }

        if (muestra.Datos.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(Array.Empty<MensajeDecodificado>());
        }

        ct.ThrowIfCancellationRequested();

        List<MensajeDecodificado> resultados = new();
        ReadOnlySpan<short> datos = muestra.Datos.Span;

        lock (_lockBuffer)
        {
            for (int i = 0; i < datos.Length; i++)
            {
                _bufferSimbolo[_posicionBuffer] = datos[i] / 32768.0f;
                _posicionBuffer++;
                _muestrasEnBuffer++;

                if (_posicionBuffer >= _bufferSimbolo.Length)
                {
                    // Simbolo completo — detectar tono con Goertzel
                    int tonoDetectado = DetectarTonoIfk(_bufferSimbolo, _bufferSimbolo.Length);

                    // IFK: el caracter se codifica por la diferencia entre tono actual y anterior
                    if (tonoDetectado >= 0 && _tonoAnterior >= 0)
                    {
                        int diferencia = (tonoDetectado - _tonoAnterior + _configuracion.NumeroTonos) % _configuracion.NumeroTonos;
                        _simbolosAcumulados.Add(diferencia);
                    }

                    _tonoAnterior = tonoDetectado;

                    _posicionBuffer = 0;
                    _muestrasEnBuffer = 0;
                    Array.Clear(_bufferSimbolo, 0, _bufferSimbolo.Length);

                    // Intentar decodificar cuando se acumulan suficientes simbolos
                    if (_simbolosAcumulados.Count >= _configuracion.SimbolosPorBloque)
                    {
                        string textoDecodificado = DecodificarSimbolosIfk(_simbolosAcumulados);

                        if (!string.IsNullOrWhiteSpace(textoDecodificado))
                        {
                            SubModoOperacion subModo = DeterminarSubModo();
                            MensajeDecodificado mensaje = new(
                                marcaDeTiempo: muestra.MarcaDeTiempo,
                                frecuenciaAudioHz: (int)_configuracion.FrecuenciaCentralHz,
                                snr: 0,
                                deltaTiempo: 0.0,
                                modo: ModoOperacion.THOR,
                                texto: textoDecodificado,
                                subModo: subModo);

                            resultados.Add(mensaje);
                            MensajeDecodificadoRecibido?.Invoke(this, mensaje);
                        }

                        _simbolosAcumulados.Clear();
                    }
                }
            }
        }

        return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(resultados.AsReadOnly());
    }

    /// <summary>
    /// Detecta el tono IFK dominante en un segmento de audio usando filtros Goertzel.
    /// </summary>
    /// <param name="buffer">Buffer de audio del simbolo.</param>
    /// <param name="longitud">Longitud del buffer.</param>
    /// <returns>Indice del tono detectado (0 a NumeroTonos-1), o -1 si no se detecta.</returns>
    private int DetectarTonoIfk(float[] buffer, int longitud)
    {
        double espaciadoTonos = _configuracion.AnchoDeBandaHz / _configuracion.NumeroTonos;
        double frecuenciaInicio = _configuracion.FrecuenciaCentralHz -
                                  (_configuracion.AnchoDeBandaHz / 2.0);

        double magnitudMaxima = 0.0;
        int tonoMaximo = -1;

        for (int indiceTono = 0; indiceTono < _configuracion.NumeroTonos; indiceTono++)
        {
            double frecuenciaTono = frecuenciaInicio + (indiceTono * espaciadoTonos);
            double magnitud = CalcularGoertzel(buffer, 0, longitud, frecuenciaTono);

            if (magnitud > magnitudMaxima && magnitud > _configuracion.UmbralMagnitudTono)
            {
                magnitudMaxima = magnitud;
                tonoMaximo = indiceTono;
            }
        }

        return tonoMaximo;
    }

    /// <summary>
    /// Decodifica una secuencia de diferencias de tonos IFK a texto ASCII.
    /// Cada diferencia de tono representa un caracter.
    /// </summary>
    /// <param name="simbolos">Lista de diferencias de tonos detectados.</param>
    /// <returns>Texto decodificado.</returns>
    private string DecodificarSimbolosIfk(List<int> simbolos)
    {
        List<char> caracteres = new();

        for (int i = 0; i < simbolos.Count; i++)
        {
            int diferencia = simbolos[i];

            if (diferencia < 0 || diferencia >= _configuracion.NumeroTonos)
            {
                continue;
            }

            // Mapear diferencia de tono a ASCII imprimible (32-126)
            int valorAscii = 32 + (diferencia % 95);
            char caracter = (char)valorAscii;

            if (char.IsLetterOrDigit(caracter) || char.IsPunctuation(caracter) || caracter == ' ')
            {
                caracteres.Add(caracter);
            }
        }

        return new string(caracteres.ToArray());
    }

    /// <summary>
    /// Determina el submodo de THOR segun la configuracion actual (BaudRate).
    /// </summary>
    /// <returns>Submodo de operacion correspondiente.</returns>
    private SubModoOperacion DeterminarSubModo()
    {
        return _configuracion.BaudRate switch
        {
            <= 5.0 => SubModoOperacion.THOR4,
            <= 10.0 => SubModoOperacion.THOR8,
            _ => SubModoOperacion.THOR16
        };
    }

    /// <summary>
    /// Calcula la magnitud de Goertzel para una frecuencia especifica en un segmento de audio.
    /// </summary>
    /// <param name="buffer">Buffer de audio.</param>
    /// <param name="inicio">Posicion de inicio.</param>
    /// <param name="longitud">Numero de muestras a procesar.</param>
    /// <param name="frecuenciaHz">Frecuencia objetivo en Hz.</param>
    /// <returns>Magnitud normalizada de la frecuencia detectada.</returns>
    private double CalcularGoertzel(float[] buffer, int inicio, int longitud, double frecuenciaHz)
    {
        double k = 0.5 + ((longitud * frecuenciaHz) / _configuracion.TasaDeMuestreo);
        double omega = (2.0 * Math.PI * k) / longitud;
        double coeficiente = 2.0 * Math.Cos(omega);

        double s0 = 0.0;
        double s1 = 0.0;
        double s2 = 0.0;

        for (int i = 0; i < longitud; i++)
        {
            s0 = buffer[inicio + i] + (coeficiente * s1) - s2;
            s2 = s1;
            s1 = s0;
        }

        double potencia = (s1 * s1) + (s2 * s2) - (coeficiente * s1 * s2);
        return Math.Sqrt(Math.Abs(potencia)) / longitud;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_dispuesto)
        {
            return;
        }

        lock (_lockEstado)
        {
            _estaActivo = false;
        }

        _dispuesto = true;
    }
}
