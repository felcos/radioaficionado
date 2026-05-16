namespace RadioAficionado.Nativo.ModosDigitales.Js8;

/// <summary>
/// Velocidades de operacion disponibles en JS8Call.
/// Cada velocidad define la duracion de la ventana temporal.
/// </summary>
public enum VelocidadJs8
{
    /// <summary>Velocidad normal: ventana de 15 segundos.</summary>
    Normal = 15,

    /// <summary>Velocidad rapida: ventana de 10 segundos.</summary>
    Rapido = 10,

    /// <summary>Velocidad lenta: ventana de 30 segundos.</summary>
    Lento = 30,

    /// <summary>Velocidad turbo: ventana de 6 segundos.</summary>
    Turbo = 6
}

/// <summary>
/// Configuracion para el decodificador JS8Call.
/// JS8Call es un modo de mensajeria keyboard-to-keyboard basado en la modulacion FT8 (MFSK).
/// </summary>
public sealed class ConfiguracionJs8
{
    /// <summary>
    /// Frecuencia base de operacion en Hz. Por defecto 7.078 MHz (40m JS8).
    /// </summary>
    public long FrecuenciaBase { get; set; } = 7_078_000;

    /// <summary>
    /// Velocidad de operacion JS8. Determina el ancho de ventana temporal.
    /// </summary>
    public VelocidadJs8 Velocidad { get; set; } = VelocidadJs8.Normal;

    /// <summary>
    /// Tasa de muestreo del audio en Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Umbral minimo de SNR en dB para aceptar una decodificacion.
    /// </summary>
    public int UmbralSnr { get; set; } = -24;

    /// <summary>
    /// Frecuencia de audio minima en Hz dentro de la banda pasante.
    /// </summary>
    public int FrecuenciaAudioMinima { get; set; } = 200;

    /// <summary>
    /// Frecuencia de audio maxima en Hz dentro de la banda pasante.
    /// </summary>
    public int FrecuenciaAudioMaxima { get; set; } = 3000;

    /// <summary>
    /// Obtiene el ancho de ventana en segundos segun la velocidad configurada.
    /// </summary>
    /// <returns>Duracion de la ventana temporal en segundos.</returns>
    public double ObtenerAnchoDeVentana()
    {
        return (double)Velocidad;
    }
}
