namespace RadioAficionado.Nativo.ModosDigitales.Ft8;

/// <summary>
/// Configuración para el decodificador FT8/FT4.
/// Contiene los parámetros ajustables del proceso de decodificación.
/// </summary>
public sealed class ConfiguracionFt8
{
    /// <summary>
    /// Frecuencia base de operación en Hz. Por defecto 14.074 MHz (20m FT8).
    /// </summary>
    public long FrecuenciaBase { get; set; } = 14_074_000;

    /// <summary>
    /// Ancho de la ventana de decodificación en segundos.
    /// FT8 usa 15 segundos, FT4 usa 7.5 segundos.
    /// </summary>
    public double AnchoDeVentana { get; set; } = 15.0;

    /// <summary>
    /// Umbral mínimo de SNR en dB para aceptar una decodificación.
    /// FT8 puede decodificar hasta -21 dB aproximadamente.
    /// </summary>
    public int UmbralSnr { get; set; } = -21;

    /// <summary>
    /// Frecuencia de audio mínima en Hz dentro de la banda pasante.
    /// </summary>
    public int FrecuenciaAudioMinima { get; set; } = 200;

    /// <summary>
    /// Frecuencia de audio máxima en Hz dentro de la banda pasante.
    /// </summary>
    public int FrecuenciaAudioMaxima { get; set; } = 3000;

    /// <summary>
    /// Tasa de muestreo del audio en Hz. FT8 requiere 12000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Número máximo de hilos para la decodificación en paralelo.
    /// </summary>
    public int MaximoHilosDecodificacion { get; set; } = Environment.ProcessorCount;
}
