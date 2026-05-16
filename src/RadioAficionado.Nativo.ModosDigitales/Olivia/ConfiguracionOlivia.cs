namespace RadioAficionado.Nativo.ModosDigitales.Olivia;

/// <summary>
/// Configuracion para el decodificador Olivia.
/// Olivia es un modo MFSK extremadamente robusto que usa Walsh-Hadamard FEC.
/// Los modos se expresan como Olivia N/BW donde N es el numero de tonos y BW el ancho de banda.
/// El modo por defecto es Olivia 32/1000.
/// </summary>
public sealed class ConfiguracionOlivia
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz. Olivia usa 8000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 8_000;

    /// <summary>
    /// Numero de tonos MFSK. Valores validos: 4, 8, 16, 32, 64, 128, 256.
    /// </summary>
    public int NumeroTonos { get; set; } = 32;

    /// <summary>
    /// Ancho de banda en Hz. Valores validos: 125, 250, 500, 1000, 2000.
    /// </summary>
    public int AnchoDeBandaHz { get; set; } = 1000;

    /// <summary>
    /// Tiempo de simbolo en segundos. Se calcula automaticamente como NumeroTonos / AnchoDeBandaHz.
    /// </summary>
    public double TiempoSimboloSegundos => (double)NumeroTonos / AnchoDeBandaHz;

    /// <summary>
    /// Frecuencia central en Hz donde se ubica la senal Olivia.
    /// </summary>
    public double FrecuenciaCentralHz { get; set; } = 1500.0;

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
        int[] tonosValidos = { 4, 8, 16, 32, 64, 128, 256 };
        int[] anchosBandaValidos = { 125, 250, 500, 1000, 2000 };

        bool tonosOk = false;
        foreach (int tono in tonosValidos)
        {
            if (NumeroTonos == tono)
            {
                tonosOk = true;
                break;
            }
        }

        bool anchoOk = false;
        foreach (int ancho in anchosBandaValidos)
        {
            if (AnchoDeBandaHz == ancho)
            {
                anchoOk = true;
                break;
            }
        }

        return tonosOk && anchoOk;
    }
}
