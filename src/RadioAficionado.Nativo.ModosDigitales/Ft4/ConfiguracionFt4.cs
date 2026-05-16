namespace RadioAficionado.Nativo.ModosDigitales.Ft4;

/// <summary>
/// Configuracion para el decodificador FT4.
/// FT4 es un modo rapido diseñado para contests con ventanas de 7.5 segundos.
/// </summary>
public sealed class ConfiguracionFt4
{
    /// <summary>
    /// Frecuencia base de operacion en Hz. Por defecto 7.074 MHz (40m FT4).
    /// </summary>
    public long FrecuenciaBase { get; set; } = 7_074_000;

    /// <summary>
    /// Ancho de la ventana de decodificacion en segundos. FT4 usa 7.5 segundos.
    /// </summary>
    public double AnchoDeVentana { get; set; } = 7.5;

    /// <summary>
    /// Umbral minimo de SNR en dB para aceptar una decodificacion.
    /// FT4 puede decodificar hasta -21 dB aproximadamente.
    /// </summary>
    public int UmbralSnr { get; set; } = -21;

    /// <summary>
    /// Tasa de muestreo del audio en Hz. FT4 requiere 12000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Frecuencia de audio minima en Hz dentro de la banda pasante.
    /// </summary>
    public int FrecuenciaAudioMinima { get; set; } = 200;

    /// <summary>
    /// Frecuencia de audio maxima en Hz dentro de la banda pasante.
    /// </summary>
    public int FrecuenciaAudioMaxima { get; set; } = 3000;
}
