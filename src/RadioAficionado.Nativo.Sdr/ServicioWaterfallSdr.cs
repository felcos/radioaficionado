using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Sdr;
using RadioAficionado.Nativo.Dsp;

namespace RadioAficionado.Nativo.Sdr;

/// <summary>
/// Servicio de waterfall especializado para fuentes SDR.
/// Se suscribe a las muestras IQ de un <see cref="IReceptorSdr"/>,
/// las convierte a audio mono mediante <see cref="IConvertidorIqAAudio"/>
/// y genera líneas de espectro para el waterfall en tiempo real.
/// Usa buffer con solapamiento del 50% (mismo patrón que <see cref="ServicioWaterfall"/>).
/// </summary>
public sealed class ServicioWaterfallSdr : IServicioWaterfallSdr
{
    private readonly IConvertidorIqAAudio _convertidor;
    private readonly ILogger<ServicioWaterfallSdr> _logger;
    private readonly object _bloqueo = new();

    private IReceptorSdr? _receptor;
    private ProcesadorEspectro? _procesador;
    private short[]? _bufferAcumulado;
    private int _posicionBuffer;
    private bool _descartado;

    /// <inheritdoc />
    public bool EstaActivo { get; private set; }

    /// <inheritdoc />
    public int TamanoFft { get; private set; } = 1024;

    /// <inheritdoc />
    public int TasaDeMuestreoHz { get; private set; }

    /// <inheritdoc />
    public FuenteDeDatosWaterfall FuenteDeDatos { get; private set; } = FuenteDeDatosWaterfall.Ninguna;

    /// <inheritdoc />
    public event EventHandler<LineaEspectroEventArgs>? LineaEspectroGenerada;

    /// <summary>
    /// Crea una nueva instancia del servicio de waterfall SDR.
    /// </summary>
    /// <param name="convertidor">Convertidor de muestras IQ a audio mono.</param>
    /// <param name="logger">Logger para diagnóstico.</param>
    public ServicioWaterfallSdr(
        IConvertidorIqAAudio convertidor,
        ILogger<ServicioWaterfallSdr> logger)
    {
        _convertidor = convertidor ?? throw new ArgumentNullException(nameof(convertidor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task IniciarConSdrAsync(IReceptorSdr receptor, int tamanoFft = 2048, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_descartado, this);
        ArgumentNullException.ThrowIfNull(receptor, nameof(receptor));

        if (EstaActivo)
        {
            _logger.LogWarning("El waterfall SDR ya está activo. Deteniéndolo primero.");
            DetenerInterno();
        }

        TamanoFft = tamanoFft;
        // Usar la tasa de muestreo del SDR (convertir de double a int)
        TasaDeMuestreoHz = (int)receptor.TasaDeMuestreoHz;
        _procesador = new ProcesadorEspectro(TasaDeMuestreoHz, tamanoFft);
        _bufferAcumulado = new short[tamanoFft];
        _posicionBuffer = 0;

        _receptor = receptor;
        _receptor.MuestrasRecibidas += AlRecibirMuestrasIq;

        EstaActivo = true;
        FuenteDeDatos = FuenteDeDatosWaterfall.Sdr;

        _logger.LogInformation(
            "Waterfall SDR iniciado. FFT: {TamanoFft}, Tasa: {TasaMuestreo} Hz.",
            tamanoFft, TasaDeMuestreoHz);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DetenerAsync(CancellationToken ct = default)
    {
        DetenerInterno();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void ConfigurarConvertidor(double gananciaDigital)
    {
        _convertidor.GananciaDigital = gananciaDigital;

        _logger.LogDebug("Ganancia digital del convertidor IQ configurada a {Ganancia}.", gananciaDigital);
    }

    /// <summary>
    /// Maneja las muestras IQ recibidas del receptor SDR.
    /// Convierte las muestras IQ a audio mono, las acumula en el buffer
    /// y genera líneas de espectro cuando el buffer está lleno.
    /// </summary>
    private void AlRecibirMuestrasIq(object? sender, MuestrasSdrEventArgs e)
    {
        if (!EstaActivo || _procesador is null || _bufferAcumulado is null)
        {
            return;
        }

        try
        {
            // Convertir IQ a audio mono (magnitud normalizada)
            double[] audioMono = _convertidor.Convertir(e.MuestrasI, e.MuestrasQ);

            // Convertir double[-1,1] a short (PCM 16 bits) para el ProcesadorEspectro
            int posicionDatos = 0;

            while (posicionDatos < audioMono.Length)
            {
                int espacioDisponible = TamanoFft - _posicionBuffer;
                int muestrasACopiar = Math.Min(espacioDisponible, audioMono.Length - posicionDatos);

                lock (_bloqueo)
                {
                    for (int i = 0; i < muestrasACopiar; i++)
                    {
                        // Convertir de double normalizado [-1,1] a short PCM 16 bits
                        double valorLimitado = Math.Clamp(audioMono[posicionDatos + i], -1.0, 1.0);
                        _bufferAcumulado[_posicionBuffer + i] = (short)(valorLimitado * 32767.0);
                    }

                    _posicionBuffer += muestrasACopiar;
                }

                posicionDatos += muestrasACopiar;

                if (_posicionBuffer >= TamanoFft)
                {
                    GenerarLineaEspectro(e.FrecuenciaCentralHz);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar muestras IQ para el waterfall.");
        }
    }

    /// <summary>
    /// Genera una línea de espectro a partir del buffer acumulado
    /// y dispara el evento para que el waterfall se actualice.
    /// Mantiene 50% de solapamiento copiando la segunda mitad al inicio.
    /// </summary>
    /// <param name="frecuenciaCentralHz">Frecuencia central del SDR en Hz.</param>
    private void GenerarLineaEspectro(double frecuenciaCentralHz)
    {
        if (_procesador is null || _bufferAcumulado is null)
        {
            return;
        }

        LineaEspectro linea;

        lock (_bloqueo)
        {
            linea = _procesador.Procesar(_bufferAcumulado.AsSpan(0, TamanoFft));

            // Mantener 50% de solapamiento: copiar segunda mitad al inicio
            int mitad = TamanoFft / 2;
            Array.Copy(_bufferAcumulado, mitad, _bufferAcumulado, 0, mitad);
            _posicionBuffer = mitad;
        }

        // Calcular frecuencias basadas en la frecuencia central del SDR
        double anchoBandaTotal = TasaDeMuestreoHz;
        double frecuenciaMinHz = frecuenciaCentralHz - anchoBandaTotal / 2.0;
        double frecuenciaMaxHz = frecuenciaCentralHz + anchoBandaTotal / 2.0;

        LineaEspectroEventArgs args = new()
        {
            MarcaDeTiempo = linea.MarcaDeTiempo,
            MagnitudesDb = linea.MagnitudesDb,
            ResolucionHz = linea.ResolucionHz,
            FrecuenciaMinHz = frecuenciaMinHz,
            FrecuenciaMaxHz = frecuenciaMaxHz
        };

        LineaEspectroGenerada?.Invoke(this, args);
    }

    /// <summary>
    /// Detiene el procesamiento de waterfall y libera los recursos internos.
    /// </summary>
    private void DetenerInterno()
    {
        if (!EstaActivo)
        {
            return;
        }

        if (_receptor is not null)
        {
            _receptor.MuestrasRecibidas -= AlRecibirMuestrasIq;
            _receptor = null;
        }

        _procesador?.Dispose();
        _procesador = null;
        _bufferAcumulado = null;
        _posicionBuffer = 0;
        EstaActivo = false;
        FuenteDeDatos = FuenteDeDatosWaterfall.Ninguna;

        _logger.LogInformation("Waterfall SDR detenido.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_descartado)
        {
            _descartado = true;
            await DetenerAsync();
        }
    }
}
