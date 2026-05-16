namespace RadioAficionado.Nativo.ModosDigitales.Jt65;

/// <summary>
/// Configuracion para el decodificador JT65.
/// JT65 es un modo de senal debil disenado para EME (moonbounce) que usa 65-FSK
/// con simbolos de ~0.372 segundos y un ciclo de transmision de 46.8 segundos.
/// </summary>
public sealed class ConfiguracionJt65
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz. JT65 requiere 12000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Duracion de cada simbolo en segundos (~0.372s para JT65).
    /// </summary>
    public double DuracionSimboloSegundos { get; set; } = 0.372;

    /// <summary>
    /// Numero de tonos usados en la modulacion FSK (65 para JT65).
    /// </summary>
    public int NumeroTonos { get; set; } = 65;

    /// <summary>
    /// Frecuencia base en Hz desde donde comienzan los tonos.
    /// </summary>
    public double FrecuenciaBaseHz { get; set; } = 1270.5;

    /// <summary>
    /// Tiempo total de transmision en segundos (46.8s para JT65).
    /// </summary>
    public double TiempoTransmisionSegundos { get; set; } = 46.8;

    /// <summary>
    /// Espaciado entre tonos en Hz (2.6917 Hz para JT65).
    /// </summary>
    public double EspaciadoTonosHz { get; set; } = 2.6917;

    /// <summary>
    /// Umbral minimo de magnitud Goertzel para considerar un tono valido.
    /// </summary>
    public double UmbralMagnitudTono { get; set; } = 0.01;
}
