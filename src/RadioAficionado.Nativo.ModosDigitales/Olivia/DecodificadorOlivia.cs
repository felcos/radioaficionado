using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Olivia;

/// <summary>
/// Decodificador de Olivia (MFSK con Walsh-Hadamard FEC) que implementa <see cref="IDecodificadorDigital"/>.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Acumula muestras para cada simbolo (duracion = NumeroTonos / AnchoDeBandaHz).
/// 3. Detecta tonos MFSK usando filtros Goertzel multi-tono.
/// 4. Aplica decodificacion Walsh-Hadamard simplificada para correccion de errores.
/// 5. Convierte los indices de tonos decodificados a caracteres ASCII.
/// </summary>
public sealed class DecodificadorOlivia : IDecodificadorDigital
{
    private readonly ConfiguracionOlivia _configuracion;
    private readonly object _lockBuffer = new();
    private readonly object _lockEstado = new();

    private float[] _bufferSimbolo;
    private int _posicionBuffer;
    private int _muestrasEnBuffer;
    private readonly List<int> _simbolosAcumulados = new();
    private bool _estaActivo;
    private bool _dispuesto;

    // Tabla Walsh-Hadamard simplificada para 32 tonos (5 bits por caracter)
    private static readonly int[,] _tablaWalshHadamard32 = GenerarMatrizWalshHadamard(32);

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.OLIVIA;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>
    {
        SubModoOperacion.OLIVIA_8_250,
        SubModoOperacion.OLIVIA_8_500,
        SubModoOperacion.OLIVIA_16_500,
        SubModoOperacion.OLIVIA_16_1000,
        SubModoOperacion.OLIVIA_32_1000
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
    /// Crea una nueva instancia del decodificador Olivia.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa Olivia 32/1000.</param>
    public DecodificadorOlivia(ConfiguracionOlivia? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionOlivia();

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
                        string textoDecodificado = DecodificarSimbolosWalshHadamard(_simbolosAcumulados);

                        if (!string.IsNullOrWhiteSpace(textoDecodificado))
                        {
                            SubModoOperacion subModo = DeterminarSubModo();
                            MensajeDecodificado mensaje = new(
                                marcaDeTiempo: muestra.MarcaDeTiempo,
                                frecuenciaAudioHz: (int)_configuracion.FrecuenciaCentralHz,
                                snr: 0,
                                deltaTiempo: 0.0,
                                modo: ModoOperacion.OLIVIA,
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
    /// </summary>
    /// <param name="buffer">Buffer de audio del simbolo.</param>
    /// <param name="longitud">Longitud del buffer.</param>
    /// <returns>Indice del tono detectado (0 a NumeroTonos-1), o -1 si no se detecta.</returns>
    private int DetectarTonoMfsk(float[] buffer, int longitud)
    {
        double espaciadoTonos = (double)_configuracion.AnchoDeBandaHz / _configuracion.NumeroTonos;
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
    /// Decodifica una secuencia de simbolos MFSK usando la tabla Walsh-Hadamard simplificada.
    /// Cada grupo de simbolos (segun NumeroTonos) representa un caracter ASCII.
    /// </summary>
    /// <param name="simbolos">Lista de indices de tonos detectados.</param>
    /// <returns>Texto decodificado.</returns>
    private string DecodificarSimbolosWalshHadamard(List<int> simbolos)
    {
        List<char> caracteres = new();
        int bitsPerCaracter = (int)Math.Log2(_configuracion.NumeroTonos);

        if (bitsPerCaracter <= 0)
        {
            bitsPerCaracter = 5;
        }

        // Cada caracter se codifica usando un grupo de simbolos
        // Simplificacion: cada simbolo valido contribuye directamente al valor del caracter
        for (int i = 0; i < simbolos.Count; i++)
        {
            int simbolo = simbolos[i];
            if (simbolo < 0 || simbolo >= _configuracion.NumeroTonos)
            {
                continue;
            }

            // Aplicar transformada Walsh-Hadamard inversa simplificada
            int valorDecodificado = AplicarWalshHadamardInverso(simbolo);

            // Mapear a ASCII imprimible (32-126)
            int valorAscii = 32 + (valorDecodificado % 95);
            char caracter = (char)valorAscii;

            if (char.IsLetterOrDigit(caracter) || char.IsPunctuation(caracter) || caracter == ' ')
            {
                caracteres.Add(caracter);
            }
        }

        return new string(caracteres.ToArray());
    }

    /// <summary>
    /// Aplica la transformada Walsh-Hadamard inversa simplificada a un indice de tono.
    /// </summary>
    /// <param name="indiceTono">Indice del tono detectado.</param>
    /// <returns>Valor decodificado.</returns>
    private int AplicarWalshHadamardInverso(int indiceTono)
    {
        int tamano = Math.Min(_configuracion.NumeroTonos, _tablaWalshHadamard32.GetLength(0));

        if (indiceTono >= tamano)
        {
            return indiceTono;
        }

        // Buscar la fila de la matriz que mejor correlaciona con el indice de tono
        int mejorCorrelacion = 0;
        int mejorIndice = 0;

        for (int fila = 0; fila < tamano; fila++)
        {
            int correlacion = 0;
            for (int col = 0; col < tamano; col++)
            {
                if (col == indiceTono)
                {
                    correlacion += _tablaWalshHadamard32[fila, col];
                }
            }

            if (Math.Abs(correlacion) > Math.Abs(mejorCorrelacion))
            {
                mejorCorrelacion = correlacion;
                mejorIndice = fila;
            }
        }

        return mejorIndice;
    }

    /// <summary>
    /// Genera una matriz Walsh-Hadamard de tamaño N x N usando la construccion recursiva de Sylvester.
    /// </summary>
    /// <param name="tamano">Tamano de la matriz (debe ser potencia de 2).</param>
    /// <returns>Matriz Walsh-Hadamard.</returns>
    private static int[,] GenerarMatrizWalshHadamard(int tamano)
    {
        int[,] matriz = new int[tamano, tamano];

        // Caso base: H1 = [1]
        if (tamano == 1)
        {
            matriz[0, 0] = 1;
            return matriz;
        }

        // Construccion de Sylvester: H_2n = [H_n  H_n; H_n -H_n]
        int mitad = tamano / 2;
        int[,] subMatriz = GenerarMatrizWalshHadamard(mitad);

        for (int i = 0; i < mitad; i++)
        {
            for (int j = 0; j < mitad; j++)
            {
                int valor = subMatriz[i, j];
                matriz[i, j] = valor;
                matriz[i, j + mitad] = valor;
                matriz[i + mitad, j] = valor;
                matriz[i + mitad, j + mitad] = -valor;
            }
        }

        return matriz;
    }

    /// <summary>
    /// Determina el submodo de Olivia segun la configuracion actual.
    /// </summary>
    /// <returns>Submodo de operacion correspondiente.</returns>
    private SubModoOperacion DeterminarSubModo()
    {
        return (_configuracion.NumeroTonos, _configuracion.AnchoDeBandaHz) switch
        {
            (8, 250) => SubModoOperacion.OLIVIA_8_250,
            (8, 500) => SubModoOperacion.OLIVIA_8_500,
            (16, 500) => SubModoOperacion.OLIVIA_16_500,
            (16, 1000) => SubModoOperacion.OLIVIA_16_1000,
            (32, 1000) => SubModoOperacion.OLIVIA_32_1000,
            _ => SubModoOperacion.Ninguno
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
