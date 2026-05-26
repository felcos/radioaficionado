using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Fsq;

/// <summary>
/// FSQ (Fast Simple QSO) — modo conversacional rapido y robusto basado en MFSK.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Acumula muestras para cada simbolo (duracion = 1 / BaudRate).
/// 3. Detecta tonos MFSK usando filtros Goertzel multi-tono (33 tonos en 100 Hz).
/// 4. El tono con mayor magnitud se mapea directamente a un caracter ASCII (sin FEC, sin IFK).
/// 5. Acumula caracteres y emite mensajes cuando se completa un bloque.
/// </summary>
public sealed class DecodificadorFsq : IDecodificadorDigital
{
    private readonly ConfiguracionFsq _configuracion;
    private readonly object _lockBuffer = new();
    private readonly object _lockEstado = new();

    private float[] _bufferSimbolo;
    private int _posicionBuffer;
    private int _muestrasEnBuffer;
    private readonly List<int> _simbolosAcumulados = new();
    private bool _estaActivo;
    private bool _dispuesto;

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.FSQ;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>
    {
        SubModoOperacion.FSQ2,
        SubModoOperacion.FSQ3,
        SubModoOperacion.FSQ4_5,
        SubModoOperacion.FSQ6
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
    /// Crea una nueva instancia del decodificador FSQ.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa FSQ4.5 por defecto.</param>
    public DecodificadorFsq(ConfiguracionFsq? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionFsq();

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
                    // Simbolo completo — detectar tono MFSK
                    int tonoDetectado = DetectarTonoMfsk(_bufferSimbolo, _bufferSimbolo.Length);
                    _simbolosAcumulados.Add(tonoDetectado);

                    _posicionBuffer = 0;
                    _muestrasEnBuffer = 0;
                    Array.Clear(_bufferSimbolo, 0, _bufferSimbolo.Length);

                    // Emitir mensaje cuando se acumulan suficientes simbolos
                    if (_simbolosAcumulados.Count >= _configuracion.SimbolosPorBloque)
                    {
                        string textoDecodificado = MapearTonosATexto(_simbolosAcumulados);

                        if (!string.IsNullOrWhiteSpace(textoDecodificado))
                        {
                            SubModoOperacion subModo = DeterminarSubModo();
                            MensajeDecodificado mensaje = new(
                                marcaDeTiempo: muestra.MarcaDeTiempo,
                                frecuenciaAudioHz: (int)_configuracion.FrecuenciaBase,
                                snr: 0,
                                deltaTiempo: 0.0,
                                modo: ModoOperacion.FSQ,
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
    /// Detecta el tono MFSK dominante en un segmento de audio usando filtros Goertzel.
    /// FSQ usa 33 tonos espaciados uniformemente en 100 Hz de ancho de banda.
    /// </summary>
    /// <param name="buffer">Buffer de audio del simbolo.</param>
    /// <param name="longitud">Longitud del buffer.</param>
    /// <returns>Indice del tono detectado (0 a 32), o -1 si no se detecta.</returns>
    private int DetectarTonoMfsk(float[] buffer, int longitud)
    {
        double espaciadoTonos = _configuracion.AnchoDeBandaHz / _configuracion.NumeroTonos;
        double frecuenciaInicio = _configuracion.FrecuenciaBase;

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
    /// Mapea una secuencia de indices de tono directamente a caracteres ASCII.
    /// FSQ usa mapeo directo sin FEC ni codificacion Walsh-Hadamard:
    /// cada tono valido (0-32) se convierte en un caracter imprimible.
    /// </summary>
    /// <param name="simbolos">Lista de indices de tonos detectados.</param>
    /// <returns>Texto decodificado.</returns>
    private static string MapearTonosATexto(List<int> simbolos)
    {
        List<char> caracteres = new();

        for (int i = 0; i < simbolos.Count; i++)
        {
            int simbolo = simbolos[i];

            // Ignorar tonos no detectados
            if (simbolo < 0 || simbolo >= 33)
            {
                continue;
            }

            // Tono 0 es espacio, tonos 1-26 son letras A-Z, tonos 27-32 son signos de puntuacion
            char caracter;
            if (simbolo == 0)
            {
                caracter = ' ';
            }
            else if (simbolo >= 1 && simbolo <= 26)
            {
                caracter = (char)('A' + simbolo - 1);
            }
            else
            {
                // Tonos 27-32: puntuacion basica
                char[] puntuacion = { '.', ',', '?', '!', '-', '/' };
                int indicePuntuacion = simbolo - 27;
                caracter = puntuacion[indicePuntuacion];
            }

            caracteres.Add(caracter);
        }

        return new string(caracteres.ToArray());
    }

    /// <summary>
    /// Determina el submodo FSQ segun el baud rate configurado.
    /// </summary>
    /// <returns>Submodo de operacion correspondiente.</returns>
    private SubModoOperacion DeterminarSubModo()
    {
        return _configuracion.BaudRate switch
        {
            <= 2.1 => SubModoOperacion.FSQ2,
            <= 3.1 => SubModoOperacion.FSQ3,
            <= 4.6 => SubModoOperacion.FSQ4_5,
            _ => SubModoOperacion.FSQ6
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
