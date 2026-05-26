namespace RadioAficionado.Nativo.ModosDigitales.Mfsk128;

/// <summary>
/// Configuracion para el decodificador MFSK128 (Multi-Frequency Shift Keying con 128 tonos).
/// MFSK128 es un modo MFSK puro de alta velocidad que usa 128 tonos en 2000 Hz de ancho de banda.
/// </summary>
public sealed class ConfiguracionMfsk128
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Numero de tonos MFSK. MFSK128 usa 128 tonos.
    /// </summary>
    public int NumeroTonos { get; set; } = 128;

    /// <summary>
    /// Ancho de banda en Hz. MFSK128 usa 2000 Hz.
    /// </summary>
    public double AnchoDeBandaHz { get; set; } = 2000.0;

    /// <summary>
    /// Frecuencia base en Hz donde comienza la banda de tonos.
    /// </summary>
    public double FrecuenciaBase { get; set; } = 1000.0;

    /// <summary>
    /// Tiempo de simbolo en segundos. Se calcula automaticamente como NumeroTonos / AnchoDeBandaHz.
    /// </summary>
    public double TiempoSimboloSegundos => NumeroTonos / AnchoDeBandaHz;

    /// <summary>
    /// Umbral minimo de magnitud para deteccion de tonos.
    /// </summary>
    public double UmbralMagnitudTono { get; set; } = 0.005;

    /// <summary>
    /// Numero de simbolos a acumular antes de intentar decodificar.
    /// </summary>
    public int SimbolosPorBloque { get; set; } = 64;
}
