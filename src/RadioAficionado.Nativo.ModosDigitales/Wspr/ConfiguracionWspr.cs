namespace RadioAficionado.Nativo.ModosDigitales.Wspr;

/// <summary>
/// Configuracion para el decodificador WSPR (Weak Signal Propagation Reporter).
/// WSPR usa 4-FSK con simbolos muy lentos (~0.683s cada uno, 162 simbolos por transmision)
/// y tonos espaciados 1.4648 Hz, lo que le da una sensibilidad extremadamente alta.
/// </summary>
public sealed class ConfiguracionWspr
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz. WSPR requiere 12000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Duracion total de una transmision WSPR en segundos (~110.6s).
    /// </summary>
    public double DuracionTransmisionSegundos { get; set; } = 110.6;

    /// <summary>
    /// Numero de tonos usados en la modulacion 4-FSK.
    /// </summary>
    public int NumeroTonos { get; set; } = 4;

    /// <summary>
    /// Espaciado entre tonos en Hz (1.4648 Hz — muy estrecho para alta sensibilidad).
    /// </summary>
    public double EspaciadoTonosHz { get; set; } = 1.4648;

    /// <summary>
    /// Frecuencia base en Hz desde donde comienzan los tonos.
    /// </summary>
    public double FrecuenciaBaseHz { get; set; } = 1500.0;

    /// <summary>
    /// Ancho de banda total de la senal WSPR en Hz.
    /// </summary>
    public double AnchoDeBandaHz { get; set; } = 6.0;

    /// <summary>
    /// Potencia maxima permitida en dBm para reportes WSPR.
    /// </summary>
    public int PotenciaMaximaDbm { get; set; } = 37;

    /// <summary>
    /// Umbral minimo de magnitud Goertzel para considerar un tono valido.
    /// </summary>
    public double UmbralMagnitudTono { get; set; } = 0.005;
}
