namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio que coordina el pipeline de waterfall en vivo:
/// captura de audio → procesamiento FFT → generacion de lineas de espectro.
/// Conecta <see cref="IAudioPipeline"/> con el procesador de espectro
/// para alimentar el control de waterfall en tiempo real.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Coordina el pipeline completo de waterfall: se suscribe al <see cref="IAudioPipeline"/> para recibir audio, calcula la FFT y genera líneas de espectro para visualización en tiempo real.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se inicia con <see cref="IniciarAsync"/> y se suscribe a <see cref="LineaEspectroGenerada"/> para pintar el waterfall en la UI.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Nativo.Dsp.ServicioWaterfall</c>.</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.Escritorio.App.ConfigurarServicios()</c>. No disponible en mobile ni web.</para>
/// <para><b>Configuración necesaria:</b> Tamaño de FFT configurable al iniciar (potencia de 2, por defecto 2048).</para>
/// <para><b>Dependencias:</b> <see cref="IAudioPipeline"/> (para recibir muestras de audio).</para>
/// </remarks>
public interface IServicioWaterfall : IAsyncDisposable
{
    /// <summary>
    /// Indica si el servicio esta activo generando lineas de espectro.
    /// </summary>
    bool EstaActivo { get; }

    /// <summary>
    /// Tamano de la FFT configurado.
    /// </summary>
    int TamanoFft { get; }

    /// <summary>
    /// Tasa de muestreo actual en Hz.
    /// </summary>
    int TasaDeMuestreoHz { get; }

    /// <summary>
    /// Evento disparado cada vez que se genera una nueva linea de espectro.
    /// Los suscriptores reciben los datos listos para el waterfall.
    /// </summary>
    event EventHandler<LineaEspectroEventArgs>? LineaEspectroGenerada;

    /// <summary>
    /// Inicia el procesamiento de waterfall suscribiendose al pipeline de audio.
    /// </summary>
    /// <param name="tamanoFft">Tamano de la FFT (potencia de 2). Por defecto 2048.</param>
    /// <param name="ct">Token de cancelacion.</param>
    Task IniciarAsync(int tamanoFft = 2048, CancellationToken ct = default);

    /// <summary>
    /// Detiene el procesamiento de waterfall y libera la suscripcion de audio.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    Task DetenerAsync(CancellationToken ct = default);
}

/// <summary>
/// Argumentos del evento de linea de espectro generada.
/// Encapsula los datos de magnitud en dB y metadatos de frecuencia.
/// </summary>
public sealed class LineaEspectroEventArgs : EventArgs
{
    /// <summary>
    /// Marca de tiempo de la captura.
    /// </summary>
    public DateTimeOffset MarcaDeTiempo { get; init; }

    /// <summary>
    /// Magnitudes en dB por bin de frecuencia.
    /// </summary>
    public required double[] MagnitudesDb { get; init; }

    /// <summary>
    /// Resolucion de frecuencia en Hz por bin.
    /// </summary>
    public double ResolucionHz { get; init; }

    /// <summary>
    /// Frecuencia minima representada (Hz).
    /// </summary>
    public double FrecuenciaMinHz { get; init; }

    /// <summary>
    /// Frecuencia maxima representada (Hz).
    /// </summary>
    public double FrecuenciaMaxHz { get; init; }
}
