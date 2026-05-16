namespace RadioAficionado.Nativo.ModosDigitales.Jt9;

/// <summary>
/// Configuracion para el decodificador JT9.
/// JT9 es un modo de senal debil que usa 9-FSK con simbolos de ~0.576 segundos
/// y tonos espaciados 1.7361 Hz. Ofrece ~1 dB mejor sensibilidad que JT65.
/// </summary>
public sealed class ConfiguracionJt9
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz. JT9 requiere 12000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Duracion de cada simbolo en segundos (~0.576s para JT9).
    /// </summary>
    public double DuracionSimboloSegundos { get; set; } = 0.576;

    /// <summary>
    /// Numero de tonos usados en la modulacion FSK (9 para JT9).
    /// </summary>
    public int NumeroTonos { get; set; } = 9;

    /// <summary>
    /// Espaciado entre tonos en Hz (1.7361 Hz para JT9).
    /// </summary>
    public double EspaciadoTonosHz { get; set; } = 1.7361;

    /// <summary>
    /// Frecuencia base en Hz desde donde comienzan los tonos.
    /// </summary>
    public double FrecuenciaBaseHz { get; set; } = 1500.0;

    /// <summary>
    /// Tiempo total de transmision en segundos (49.0s para JT9).
    /// </summary>
    public double TiempoTransmisionSegundos { get; set; } = 49.0;

    /// <summary>
    /// Umbral minimo de magnitud Goertzel para considerar un tono valido.
    /// </summary>
    public double UmbralMagnitudTono { get; set; } = 0.01;
}
