namespace RadioAficionado.Nativo.ModosDigitales.Fsq;

/// <summary>
/// Configuracion para el decodificador FSQ (Fast Simple QSO).
/// FSQ es un modo MFSK narrowband conversacional que usa 33 tonos en 100 Hz de ancho de banda.
/// Los submodos se expresan como FSQ seguido del baud rate: FSQ2, FSQ3, FSQ4.5, FSQ6.
/// El modo por defecto es FSQ4.5 (4.5 baudios).
/// </summary>
public sealed class ConfiguracionFsq
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz. FSQ usa 12000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Numero de tonos MFSK. FSQ usa 33 tonos.
    /// </summary>
    public int NumeroTonos { get; set; } = 33;

    /// <summary>
    /// Ancho de banda en Hz. FSQ es narrowband, 100 Hz.
    /// </summary>
    public double AnchoDeBandaHz { get; set; } = 100.0;

    /// <summary>
    /// Baud rate del modo FSQ. Valores validos: 2, 3, 4.5, 6.
    /// </summary>
    public double BaudRate { get; set; } = 4.5;

    /// <summary>
    /// Tiempo de simbolo en segundos. Se calcula automaticamente como 1 / BaudRate.
    /// </summary>
    public double TiempoSimboloSegundos => 1.0 / BaudRate;

    /// <summary>
    /// Frecuencia base en Hz donde comienza la senal FSQ.
    /// </summary>
    public double FrecuenciaBase { get; set; } = 1500.0;

    /// <summary>
    /// Umbral minimo de magnitud para deteccion de tonos.
    /// </summary>
    public double UmbralMagnitudTono { get; set; } = 0.005;

    /// <summary>
    /// Numero de simbolos a acumular antes de intentar emitir un mensaje.
    /// FSQ es conversacional, asi que se emite con bloques mas pequenos que Olivia.
    /// </summary>
    public int SimbolosPorBloque { get; set; } = 32;

    /// <summary>
    /// Valida que la configuracion tenga valores correctos.
    /// </summary>
    /// <returns>True si la configuracion es valida.</returns>
    public bool EsValida()
    {
        double[] baudRatesValidos = { 2.0, 3.0, 4.5, 6.0 };

        bool baudRateOk = false;
        foreach (double baudRate in baudRatesValidos)
        {
            if (Math.Abs(BaudRate - baudRate) < 0.01)
            {
                baudRateOk = true;
                break;
            }
        }

        bool tonosOk = NumeroTonos == 33;
        bool anchoOk = Math.Abs(AnchoDeBandaHz - 100.0) < 0.01;

        return baudRateOk && tonosOk && anchoOk;
    }
}
