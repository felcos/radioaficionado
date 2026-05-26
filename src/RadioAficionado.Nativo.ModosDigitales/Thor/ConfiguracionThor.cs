namespace RadioAficionado.Nativo.ModosDigitales.Thor;

/// <summary>
/// Configuracion para el decodificador THOR.
/// THOR es un modo IFK (Incremental Frequency Keying) basado en DominoEX con FEC.
/// Usa 18 tonos y codifica caracteres por la diferencia entre tono actual y anterior.
/// El modo por defecto es THOR16 (16 baudios).
/// </summary>
public sealed class ConfiguracionThor
{
    /// <summary>
    /// Numero de tonos IFK. THOR usa 18 tonos.
    /// </summary>
    public int NumeroTonos { get; set; } = 18;

    /// <summary>
    /// Ancho de banda en Hz.
    /// </summary>
    public double AnchoDeBandaHz { get; set; } = 500.0;

    /// <summary>
    /// Tasa de muestreo del audio en Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Frecuencia base en Hz donde se ubica la senal THOR.
    /// </summary>
    public double FrecuenciaBase { get; set; } = 1000.0;

    /// <summary>
    /// Velocidad de simbolo en baudios. THOR16 por defecto.
    /// </summary>
    public double BaudRate { get; set; } = 16.0;

    /// <summary>
    /// Tiempo de simbolo en segundos. Se calcula automaticamente como 1 / BaudRate.
    /// </summary>
    public double TiempoSimboloSegundos => 1.0 / BaudRate;

    /// <summary>
    /// Frecuencia central en Hz (FrecuenciaBase + AnchoDeBandaHz / 2).
    /// </summary>
    public double FrecuenciaCentralHz => FrecuenciaBase + (AnchoDeBandaHz / 2.0);

    /// <summary>
    /// Umbral minimo de magnitud para deteccion de tonos.
    /// </summary>
    public double UmbralMagnitudTono { get; set; } = 0.005;

    /// <summary>
    /// Numero de simbolos a acumular antes de intentar decodificar.
    /// </summary>
    public int SimbolosPorBloque { get; set; } = 64;

    /// <summary>
    /// Valida que la configuracion tenga valores correctos.
    /// </summary>
    /// <returns>True si la configuracion es valida.</returns>
    public bool EsValida()
    {
        return NumeroTonos == 18
               && AnchoDeBandaHz > 0
               && TasaDeMuestreo > 0
               && BaudRate > 0
               && FrecuenciaBase > 0;
    }
}
