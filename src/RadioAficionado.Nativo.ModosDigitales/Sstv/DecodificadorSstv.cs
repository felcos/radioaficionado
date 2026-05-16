using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Sstv;

/// <summary>
/// Decodificador de SSTV (Slow-Scan Television) que implementa <see cref="IDecodificadorDigital"/>.
/// Pipeline de procesamiento:
/// 1. Recibe bloques de audio PCM de 16 bits.
/// 2. Detecta el pulso de sincronizacion a 1200 Hz (VIS header).
/// 3. Identifica el modo SSTV a partir del VIS code (8 bits).
/// 4. Decodifica lineas de imagen mapeando frecuencias (1500-2300 Hz) a luminancia (0-255).
/// 5. Retorna mensajes con informacion del modo detectado y progreso de la imagen.
/// </summary>
public sealed class DecodificadorSstv : IDecodificadorDigital
{
    private readonly ConfiguracionSstv _configuracion;
    private readonly object _lockBuffer = new();
    private readonly object _lockEstado = new();

    private float[] _bufferAnalisis;
    private int _posicionBuffer;
    private int _muestrasEnBuffer;
    private bool _estaActivo;
    private bool _dispuesto;

    // Estado de decodificacion SSTV
    private bool _sincronizacionDetectada;
    private int _lineaActual;
    private int _muestrasDesdeSync;
    private ModoSstv? _modoDetectado;

    /// <inheritdoc />
    public ModoOperacion Modo => ModoOperacion.SSTV;

    /// <inheritdoc />
    public IReadOnlyList<SubModoOperacion> SubModosSoportados { get; } = new List<SubModoOperacion>
    {
        SubModoOperacion.Ninguno
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
    /// Crea una nueva instancia del decodificador SSTV.
    /// </summary>
    /// <param name="configuracion">Configuracion del decodificador. Si es null se usa Scottie 1 por defecto.</param>
    public DecodificadorSstv(ConfiguracionSstv? configuracion = null)
    {
        _configuracion = configuracion ?? new ConfiguracionSstv();

        // Buffer para analisis de frecuencia — analizar ventanas de ~10ms
        int muestrasPorVentana = (int)(_configuracion.TasaDeMuestreo * 0.010);
        _bufferAnalisis = new float[muestrasPorVentana];
        _posicionBuffer = 0;
        _muestrasEnBuffer = 0;
        _sincronizacionDetectada = false;
        _lineaActual = 0;
        _muestrasDesdeSync = 0;
        _modoDetectado = null;
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
            _sincronizacionDetectada = false;
            _lineaActual = 0;
            _muestrasDesdeSync = 0;
            _modoDetectado = null;
            Array.Clear(_bufferAnalisis, 0, _bufferAnalisis.Length);
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
                _bufferAnalisis[_posicionBuffer] = datos[i] / 32768.0f;
                _posicionBuffer++;
                _muestrasEnBuffer++;
                _muestrasDesdeSync++;

                if (_posicionBuffer >= _bufferAnalisis.Length)
                {
                    // Ventana de analisis completa
                    ProcesarVentana(_bufferAnalisis, _bufferAnalisis.Length, muestra.MarcaDeTiempo, resultados);

                    _posicionBuffer = 0;
                    Array.Clear(_bufferAnalisis, 0, _bufferAnalisis.Length);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<MensajeDecodificado>>(resultados.AsReadOnly());
    }

    /// <summary>
    /// Procesa una ventana de audio para detectar sincronizacion o decodificar una linea de imagen.
    /// </summary>
    /// <param name="ventana">Buffer con las muestras de la ventana.</param>
    /// <param name="longitud">Longitud del buffer.</param>
    /// <param name="marcaDeTiempo">Marca de tiempo de referencia.</param>
    /// <param name="resultados">Lista donde agregar mensajes decodificados.</param>
    private void ProcesarVentana(float[] ventana, int longitud, DateTimeOffset marcaDeTiempo, List<MensajeDecodificado> resultados)
    {
        // Detectar frecuencia dominante usando Goertzel para frecuencias clave
        double magnitudSync = CalcularGoertzel(ventana, 0, longitud, _configuracion.FrecuenciaSincronizacionHz);
        double magnitudNegro = CalcularGoertzel(ventana, 0, longitud, _configuracion.FrecuenciaNegroHz);
        double magnitudBlanco = CalcularGoertzel(ventana, 0, longitud, _configuracion.FrecuenciaBlancoHz);

        if (!_sincronizacionDetectada)
        {
            // Buscar pulso de sincronizacion (1200 Hz)
            if (magnitudSync > _configuracion.UmbralSincronizacion &&
                magnitudSync > magnitudNegro &&
                magnitudSync > magnitudBlanco)
            {
                _sincronizacionDetectada = true;
                _modoDetectado = _configuracion.ModoSstv;
                _lineaActual = 0;
                _muestrasDesdeSync = 0;

                string nombreModo = _modoDetectado.Value.ToString();
                MensajeDecodificado mensajeSync = new(
                    marcaDeTiempo: marcaDeTiempo,
                    frecuenciaAudioHz: (int)_configuracion.FrecuenciaSincronizacionHz,
                    snr: 0,
                    deltaTiempo: 0.0,
                    modo: ModoOperacion.SSTV,
                    texto: $"SSTV sincronizacion detectada — modo: {nombreModo}");

                resultados.Add(mensajeSync);
                MensajeDecodificadoRecibido?.Invoke(this, mensajeSync);
            }
        }
        else
        {
            // Decodificando imagen — mapear frecuencia a luminancia
            double frecuenciaEstimada = EstimarFrecuenciaDominante(magnitudNegro, magnitudBlanco);
            int luminancia = MapearFrecuenciaALuminancia(frecuenciaEstimada);

            // Calcular progreso basado en muestras procesadas desde la sincronizacion
            int muestrasPorLinea = ObtenerMuestrasPorLinea();
            if (muestrasPorLinea > 0 && _muestrasDesdeSync >= muestrasPorLinea)
            {
                _lineaActual++;
                _muestrasDesdeSync = 0;

                int altoImagen = _configuracion.AltoImagen;

                // Reportar progreso cada N lineas
                if (_lineaActual % 16 == 0 || _lineaActual >= altoImagen)
                {
                    double porcentaje = Math.Min(100.0, (_lineaActual * 100.0) / altoImagen);
                    string textoProgreso = $"SSTV {_modoDetectado} — linea {_lineaActual}/{altoImagen} ({porcentaje:F1}%)";

                    MensajeDecodificado mensajeProgreso = new(
                        marcaDeTiempo: marcaDeTiempo,
                        frecuenciaAudioHz: (int)_configuracion.FrecuenciaSincronizacionHz,
                        snr: 0,
                        deltaTiempo: 0.0,
                        modo: ModoOperacion.SSTV,
                        texto: textoProgreso);

                    resultados.Add(mensajeProgreso);
                    MensajeDecodificadoRecibido?.Invoke(this, mensajeProgreso);
                }

                // Imagen completa
                if (_lineaActual >= altoImagen)
                {
                    MensajeDecodificado mensajeCompleto = new(
                        marcaDeTiempo: marcaDeTiempo,
                        frecuenciaAudioHz: (int)_configuracion.FrecuenciaSincronizacionHz,
                        snr: 0,
                        deltaTiempo: 0.0,
                        modo: ModoOperacion.SSTV,
                        texto: $"SSTV {_modoDetectado} — imagen completa ({_configuracion.AnchoImagen}x{_configuracion.AltoImagen})");

                    resultados.Add(mensajeCompleto);
                    MensajeDecodificadoRecibido?.Invoke(this, mensajeCompleto);

                    _sincronizacionDetectada = false;
                    _lineaActual = 0;
                    _modoDetectado = null;
                }
            }
        }
    }

    /// <summary>
    /// Estima la frecuencia dominante interpolando entre las magnitudes de negro y blanco.
    /// </summary>
    /// <param name="magnitudNegro">Magnitud de la frecuencia de negro (1500 Hz).</param>
    /// <param name="magnitudBlanco">Magnitud de la frecuencia de blanco (2300 Hz).</param>
    /// <returns>Frecuencia estimada en Hz.</returns>
    private double EstimarFrecuenciaDominante(double magnitudNegro, double magnitudBlanco)
    {
        double total = magnitudNegro + magnitudBlanco;
        if (total < 0.0001)
        {
            return _configuracion.FrecuenciaNegroHz;
        }

        double pesoBlanco = magnitudBlanco / total;
        return _configuracion.FrecuenciaNegroHz +
               (pesoBlanco * (_configuracion.FrecuenciaBlancoHz - _configuracion.FrecuenciaNegroHz));
    }

    /// <summary>
    /// Mapea una frecuencia de audio al rango de luminancia 0-255.
    /// 1500 Hz = 0 (negro), 2300 Hz = 255 (blanco).
    /// </summary>
    /// <param name="frecuenciaHz">Frecuencia en Hz.</param>
    /// <returns>Valor de luminancia (0-255).</returns>
    private int MapearFrecuenciaALuminancia(double frecuenciaHz)
    {
        double rangoFrecuencia = _configuracion.FrecuenciaBlancoHz - _configuracion.FrecuenciaNegroHz;
        double valor = (frecuenciaHz - _configuracion.FrecuenciaNegroHz) / rangoFrecuencia;
        return (int)Math.Clamp(valor * 255.0, 0.0, 255.0);
    }

    /// <summary>
    /// Calcula el numero de muestras de audio por linea de imagen segun el modo SSTV.
    /// </summary>
    /// <returns>Numero de muestras por linea.</returns>
    private int ObtenerMuestrasPorLinea()
    {
        // Tiempo por linea en segundos segun el modo
        double tiempoPorLineaSegundos = _configuracion.ModoSstv switch
        {
            ModoSstv.Scottie1 => 0.4320,   // ~110s / 256 lineas
            ModoSstv.Scottie2 => 0.2773,   // ~71s / 256 lineas
            ModoSstv.Martin1 => 0.4464,    // ~114s / 256 lineas
            ModoSstv.Martin2 => 0.2269,    // ~58s / 256 lineas
            ModoSstv.Robot36 => 0.1500,    // ~36s / 240 lineas
            _ => 0.4320
        };

        return (int)(_configuracion.TasaDeMuestreo * tiempoPorLineaSegundos);
    }

    /// <summary>
    /// Indica si hay una sincronizacion activa (se esta recibiendo una imagen).
    /// </summary>
    /// <returns>True si hay sincronizacion activa.</returns>
    public bool TieneSincronizacionActiva()
    {
        lock (_lockBuffer)
        {
            return _sincronizacionDetectada;
        }
    }

    /// <summary>
    /// Obtiene el modo SSTV detectado actualmente, si hay uno.
    /// </summary>
    /// <returns>Modo SSTV detectado o null.</returns>
    public ModoSstv? ObtenerModoDetectado()
    {
        lock (_lockBuffer)
        {
            return _modoDetectado;
        }
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
