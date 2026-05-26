using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Mfsk128;

/// <summary>
/// Decodificador de MFSK128 (Multi-Frequency Shift Keying con 128 tonos) que implementa <see cref="IDecodificadorDigital"/>.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Acumula muestras para cada simbolo (duracion = NumeroTonos / AnchoDeBandaHz).
/// 3. Detecta tonos MFSK usando filtros Goertzel multi-tono.
/// 4. Mapea el indice del tono detectado directamente a caracter ASCII (sin FEC Walsh-Hadamard).
/// 5. Acumula caracteres y emite mensajes decodificados.
/// </summary>
public sealed class DecodificadorMfsk128 : IDecodificadorDigital
{
    private readonly ConfiguracionMfsk128 _configuracion;
    private readonly object _lockBuffer = new();
    private readonly object _lockEstado = new();

    private float[] _bufferSimbolo;
    private int _posicionBuffer;
    private int _muestrasEnBuffer;
    private readonly List<int> _simbolosAcumulados = new();
    private bool _estaActivo;
    private bool _dispuesto;

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.MFSK;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>
    {
        SubModoOperacion.MFSK128
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
    /// Crea una nueva instancia del decodificador MFSK128.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa la configuracion por defecto.</param>
    public DecodificadorMfsk128(ConfiguracionMfsk128? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionMfsk128();

        int muestrasPorSimbolo = (int)(_configuracion.TasaDeMuestreo * _configuracion.TiempoSimboloSegundos);
        _bufferSimbolo = new float[muestrasPorSimbolo];
        _posicionBuffer = 0;
        _muestrasEnBuffer = 0;
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
                    // Simbolo completo — detectar tono
                    int tonoDetectado = DetectarTonoMfsk(_bufferSimbolo, _bufferSimbolo.Length);
                    _simbolosAcumulados.Add(tonoDetectado);

                    _posicionBuffer = 0;
                    _muestrasEnBuffer = 0;
                    Array.Clear(_bufferSimbolo, 0, _bufferSimbolo.Length);

                    // Intentar decodificar cuando se acumulan suficientes simbolos
                    if (_simbolosAcumulados.Count >= _configuracion.SimbolosPorBloque)
                    {
                        string textoDecodificado = DecodificarSimbolosMfsk(_simbolosAcumulados);

                        if (!string.IsNullOrWhiteSpace(textoDecodificado))
                        {
                            MensajeDecodificado mensaje = new(
                                marcaDeTiempo: muestra.MarcaDeTiempo,
                                frecuenciaAudioHz: (int)_configuracion.FrecuenciaBase,
                                snr: 0,
                                deltaTiempo: 0.0,
                                modo: ModoOperacion.MFSK,
                                texto: textoDecodificado,
                                subModo: SubModoOperacion.MFSK128);

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
    /// Detecta el tono MFSK dominante en un segmento de audio usando filtros Goertzel.
    /// </summary>
    /// <param name="buffer">Buffer de audio del simbolo.</param>
    /// <param name="longitud">Longitud del buffer.</param>
    /// <returns>Indice del tono detectado (0 a NumeroTonos-1), o -1 si no se detecta.</returns>
    private int DetectarTonoMfsk(float[] buffer, int longitud)
    {
        double espaciadoTonos = _configuracion.AnchoDeBandaHz / _configuracion.NumeroTonos;

        double magnitudMaxima = 0.0;
        int tonoMaximo = -1;

        for (int indiceTono = 0; indiceTono < _configuracion.NumeroTonos; indiceTono++)
        {
            double frecuenciaTono = _configuracion.FrecuenciaBase + (indiceTono * espaciadoTonos);
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
    /// Decodifica una secuencia de simbolos MFSK mapeando cada indice de tono directamente a caracter ASCII.
    /// MFSK128 es MFSK puro sin FEC Walsh-Hadamard: cada tono representa directamente un caracter.
    /// </summary>
    /// <param name="simbolos">Lista de indices de tonos detectados.</param>
    /// <returns>Texto decodificado.</returns>
    private string DecodificarSimbolosMfsk(List<int> simbolos)
    {
        List<char> caracteres = new();

        for (int i = 0; i < simbolos.Count; i++)
        {
            int simbolo = simbolos[i];
            if (simbolo < 0 || simbolo >= _configuracion.NumeroTonos)
            {
                continue;
            }

            // Mapear indice de tono directamente a ASCII imprimible (32-126)
            int valorAscii = 32 + (simbolo % 95);
            char caracter = (char)valorAscii;

            if (char.IsLetterOrDigit(caracter) || char.IsPunctuation(caracter) || caracter == ' ')
            {
                caracteres.Add(caracter);
            }
        }

        return new string(caracteres.ToArray());
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
