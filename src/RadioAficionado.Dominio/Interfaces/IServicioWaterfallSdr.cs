using RadioAficionado.Dominio.Sdr;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio de waterfall especializado para fuentes SDR.
/// Se suscribe a las muestras IQ de un <see cref="IReceptorSdr"/>,
/// las convierte a audio mono mediante <see cref="IConvertidorIqAAudio"/>
/// y genera líneas de espectro para el waterfall en tiempo real.
/// Complementa a <see cref="IServicioWaterfall"/> para fuentes SDR.
/// </summary>
public interface IServicioWaterfallSdr : IAsyncDisposable
{
    /// <summary>
    /// Indica si el servicio está activo generando líneas de espectro.
    /// </summary>
    bool EstaActivo { get; }

    /// <summary>
    /// Tamaño de la FFT configurado.
    /// </summary>
    int TamanoFft { get; }

    /// <summary>
    /// Tasa de muestreo actual en Hz.
    /// </summary>
    int TasaDeMuestreoHz { get; }

    /// <summary>
    /// Fuente de datos activa del waterfall.
    /// </summary>
    FuenteDeDatosWaterfall FuenteDeDatos { get; }

    /// <summary>
    /// Evento disparado cada vez que se genera una nueva línea de espectro
    /// a partir de las muestras IQ del SDR.
    /// </summary>
    event EventHandler<LineaEspectroEventArgs>? LineaEspectroGenerada;

    /// <summary>
    /// Inicia el procesamiento de waterfall suscribiéndose a las muestras IQ del receptor SDR.
    /// </summary>
    /// <param name="receptor">Receptor SDR del cual obtener las muestras IQ.</param>
    /// <param name="tamanoFft">Tamaño de la FFT (potencia de 2). Por defecto 2048.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <exception cref="ArgumentNullException">Si el receptor es nulo.</exception>
    Task IniciarConSdrAsync(IReceptorSdr receptor, int tamanoFft = 2048, CancellationToken ct = default);

    /// <summary>
    /// Detiene el procesamiento de waterfall y desuscribe del receptor SDR.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    Task DetenerAsync(CancellationToken ct = default);

    /// <summary>
    /// Configura la ganancia digital del convertidor IQ a audio.
    /// </summary>
    /// <param name="gananciaDigital">Ganancia digital a aplicar (1.0 = sin cambio).</param>
    void ConfigurarConvertidor(double gananciaDigital);
}
