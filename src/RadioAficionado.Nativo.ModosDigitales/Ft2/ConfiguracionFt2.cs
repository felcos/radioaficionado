namespace RadioAficionado.Nativo.ModosDigitales.Ft2;

/// <summary>
/// Configuracion para el decodificador FT2 (Franke-Taylor 2 — experimental).
/// FT2 usa 4-GFSK (Gaussian Frequency Shift Keying) con una ventana de 6 segundos,
/// similar a FT8 pero mas rapido (6s vs 12.64s), con tonos espaciados 6.25 Hz.
/// </summary>
public sealed class ConfiguracionFt2
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz. FT2 requiere 12000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Duracion de la ventana de transmision en segundos (6.0s para FT2).
    /// </summary>
    public double DuracionVentanaSegundos { get; set; } = 6.0;

    /// <summary>
    /// Numero de tonos usados en la modulacion 4-GFSK.
    /// </summary>
    public int NumeroTonos { get; set; } = 4;

    /// <summary>
    /// Espaciado entre tonos en Hz (6.25 Hz para FT2).
    /// </summary>
    public double EspaciadoTonosHz { get; set; } = 6.25;

    /// <summary>
    /// Frecuencia base en Hz desde donde comienzan los tonos.
    /// </summary>
    public double FrecuenciaBaseHz { get; set; } = 1000.0;

    /// <summary>
    /// Duracion de cada simbolo en segundos (0.04s para FT2).
    /// </summary>
    public double TiempoSimboloSegundos { get; set; } = 0.04;

    /// <summary>
    /// Umbral minimo de magnitud Goertzel para considerar un tono valido.
    /// </summary>
    public double UmbralMagnitudTono { get; set; } = 0.01;
}
