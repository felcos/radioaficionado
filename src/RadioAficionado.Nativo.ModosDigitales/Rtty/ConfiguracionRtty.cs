namespace RadioAficionado.Nativo.ModosDigitales.Rtty;

/// <summary>
/// Configuracion para el decodificador RTTY (Radio TeleType).
/// RTTY usa FSK con dos tonos (mark y space) separados por un shift tipico de 170 Hz.
/// </summary>
public sealed class ConfiguracionRtty
{
    /// <summary>
    /// Frecuencia del tono mark en Hz. Estandar: 2125 Hz.
    /// </summary>
    public double FrecuenciaMark { get; set; } = 2125.0;

    /// <summary>
    /// Frecuencia del tono space en Hz. Estandar: 2295 Hz.
    /// </summary>
    public double FrecuenciaSpace { get; set; } = 2295.0;

    /// <summary>
    /// Shift entre mark y space en Hz. Estandar: 170 Hz.
    /// </summary>
    public double Shift { get; set; } = 170.0;

    /// <summary>
    /// Velocidad de transmision en baudios. Estandar: 45.45 baudios.
    /// </summary>
    public double Baudios { get; set; } = 45.45;

    /// <summary>
    /// Numero de bits de parada. Estandar: 1.5 bits.
    /// </summary>
    public double BitsParada { get; set; } = 1.5;

    /// <summary>
    /// Tasa de muestreo del audio en Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Umbral minimo de diferencia entre magnitudes mark/space para considerar bit valido.
    /// </summary>
    public double UmbralDeteccion { get; set; } = 0.3;
}
