namespace RadioAficionado.Nativo.ModosDigitales.Psk31;

/// <summary>
/// Configuracion para el decodificador PSK31 (Phase Shift Keying a 31.25 baudios).
/// PSK31 es un modo digital muy popular en HF que usa BPSK con codificacion Varicode.
/// </summary>
public sealed class ConfiguracionPsk31
{
    /// <summary>
    /// Frecuencia de la portadora de audio en Hz. Tipicamente 1000 Hz.
    /// </summary>
    public double FrecuenciaPortadora { get; set; } = 1000.0;

    /// <summary>
    /// Tasa de muestreo del audio en Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Velocidad de transmision en baudios. PSK31 usa 31.25 baudios.
    /// </summary>
    public double BaudRate { get; set; } = 31.25;

    /// <summary>
    /// Umbral de deteccion para determinar cambio de fase.
    /// Valores mas bajos son mas sensibles pero mas propensos a falsos positivos.
    /// </summary>
    public double UmbralDeteccion { get; set; } = 0.5;
}
