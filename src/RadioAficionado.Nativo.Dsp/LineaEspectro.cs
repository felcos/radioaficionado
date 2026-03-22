namespace RadioAficionado.Nativo.Dsp;

/// <summary>
/// Datos de una línea del waterfall (espectro en un instante de tiempo).
/// Cada instancia representa el resultado de una FFT sobre una ventana de muestras.
/// </summary>
public sealed class LineaEspectro
{
    /// <summary>
    /// Marca de tiempo de la captura.
    /// </summary>
    public DateTimeOffset MarcaDeTiempo { get; init; }

    /// <summary>
    /// Magnitudes en dB por bin de frecuencia (N/2+1 valores).
    /// El índice 0 corresponde a DC (0 Hz) y el último a la frecuencia de Nyquist.
    /// </summary>
    public required double[] MagnitudesDb { get; init; }

    /// <summary>
    /// Resolución de frecuencia en Hz por bin (tasa de muestreo / tamaño FFT).
    /// </summary>
    public double ResolucionHz { get; init; }

    /// <summary>
    /// Frecuencia mínima representada (Hz).
    /// </summary>
    public double FrecuenciaMinHz { get; init; }

    /// <summary>
    /// Frecuencia máxima representada (Hz), correspondiente a la frecuencia de Nyquist.
    /// </summary>
    public double FrecuenciaMaxHz { get; init; }
}
