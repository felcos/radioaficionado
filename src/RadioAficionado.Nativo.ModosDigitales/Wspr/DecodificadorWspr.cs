using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Wspr;

/// <summary>
/// Decodificador de WSPR (Weak Signal Propagation Reporter) que implementa <see cref="IDecodificadorDigital"/>.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Acumula muestras en un buffer circular de ~110.6 segundos.
/// 3. Cuando el buffer esta lleno, analiza cada simbolo (~0.683s) usando filtros Goertzel
///    para los 4 tonos posibles, espaciados 1.4648 Hz.
/// 4. Determina el tono dominante de cada simbolo y genera la secuencia de simbolos.
/// 5. Los mensajes WSPR contienen: indicativo, localizador (4 caracteres), potencia en dBm.
/// </summary>
public sealed class DecodificadorWspr : IDecodificadorDigital
{
    private readonly ConfiguracionWspr _configuracion;
    private readonly object _lockBuffer = new();
    private readonly object _lockEstado = new();

    private float[] _bufferAudio;
    private int _posicionBuffer;
    private int _muestrasEnBuffer;
    private bool _estaActivo;
    private bool _dispuesto;

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.WSPR;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>().AsReadOnly();

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
    /// Crea una nueva instancia del decodificador WSPR.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa la configuracion por defecto.</param>
    public DecodificadorWspr(ConfiguracionWspr? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionWspr();

        int tamanoBuffer = (int)(_configuracion.TasaDeMuestreo * _configuracion.DuracionTransmisionSegundos);
        _bufferAudio = new float[tamanoBuffer];
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
            Array.Clear(_bufferAudio, 0, _bufferAudio.Length);
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

    /// <summary>
    /// Decodifica el buffer de audio completo analizando los tonos de cada simbolo WSPR.
    /// Cada simbolo dura ~0.683 segundos (110.6s / 162 simbolos).
    /// </summary>
    /// <param name="buffer">Buffer de audio normalizado.</param>
    /// <param name="marcaDeTiempo">Marca de tiempo de la captura.</param>
    /// <returns>Lista de mensajes decodificados.</returns>
    private List<MensajeDecodificado> DecodificarBuffer(float[] buffer, DateTimeOffset marcaDeTiempo)
    {
        List<MensajeDecodificado> resultados = new();

        // WSPR: 162 simbolos en ~110.6 segundos => ~0.683s por simbolo
        double duracionSimboloSegundos = _configuracion.DuracionTransmisionSegundos / 162.0;
        int muestrasPorSimbolo = (int)(_configuracion.TasaDeMuestreo * duracionSimboloSegundos);
        int numeroSimbolos = buffer.Length / muestrasPorSimbolo;

        List<int> simbolosDecodificados = new();

        for (int indiceSimbolo = 0; indiceSimbolo < numeroSimbolos; indiceSimbolo++)
        {
            int inicio = indiceSimbolo * muestrasPorSimbolo;
            int longitud = Math.Min(muestrasPorSimbolo, buffer.Length - inicio);

            if (longitud <= 0)
            {
                break;
            }

            int tonoDetectado = AnalizarTonos(buffer, inicio, longitud);
            simbolosDecodificados.Add(tonoDetectado);
        }

        // Verificar que se detectaron simbolos validos (al menos uno con tono != -1)
        bool haySimbolosValidos = false;
        foreach (int simbolo in simbolosDecodificados)
        {
            if (simbolo >= 0)
            {
                haySimbolosValidos = true;
                break;
            }
        }

        if (haySimbolosValidos)
        {
            string textoSimbolos = string.Join(",", simbolosDecodificados);
            MensajeDecodificado mensaje = new(
                marcaDeTiempo: marcaDeTiempo,
                frecuenciaAudioHz: (int)_configuracion.FrecuenciaBaseHz,
                snr: 0,
                deltaTiempo: 0.0,
                modo: ModoOperacion.WSPR,
                texto: $"WSPR simbolos: [{textoSimbolos}]");

            resultados.Add(mensaje);
        }

        return resultados;
    }

    /// <summary>
    /// Analiza los 4 tonos posibles en un segmento de audio usando el algoritmo de Goertzel
    /// y retorna el indice del tono con mayor magnitud.
    /// </summary>
    /// <param name="buffer">Buffer de audio.</param>
    /// <param name="inicio">Posicion de inicio en el buffer.</param>
    /// <param name="longitud">Longitud del segmento a analizar.</param>
    /// <returns>Indice del tono dominante (0-3), o -1 si no se detecta ningun tono valido.</returns>
    private int AnalizarTonos(float[] buffer, int inicio, int longitud)
    {
        double magnitudMaxima = 0.0;
        int tonoMaximo = -1;

        for (int indiceTono = 0; indiceTono < _configuracion.NumeroTonos; indiceTono++)
        {
            double frecuenciaTono = _configuracion.FrecuenciaBaseHz +
                                    (indiceTono * _configuracion.EspaciadoTonosHz);

            double magnitud = CalcularGoertzel(buffer, inicio, longitud, frecuenciaTono);

            if (magnitud > magnitudMaxima && magnitud > _configuracion.UmbralMagnitudTono)
            {
                magnitudMaxima = magnitud;
                tonoMaximo = indiceTono;
            }
        }

        return tonoMaximo;
    }

    /// <summary>
    /// Calcula la magnitud de Goertzel para una frecuencia especifica en un segmento de audio.
    /// El algoritmo de Goertzel es mas eficiente que la FFT cuando se necesita evaluar
    /// un numero pequeno de frecuencias especificas.
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
