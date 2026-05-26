namespace RadioAficionado.Nativo.ModosDigitales.Psk250;

/// <summary>
/// Configuracion para el decodificador PSK250 (Phase Shift Keying a 250 baudios).
/// PSK250 es un modo digital de alta velocidad en HF que usa BPSK con codificacion Varicode.
/// </summary>
public sealed class ConfiguracionPsk250
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
    /// Velocidad de transmision en baudios. PSK250 usa 250 baudios.
    /// </summary>
    public double BaudRate { get; set; } = 250.0;

    /// <summary>
    /// Umbral de deteccion para determinar cambio de fase.
    /// Valores mas bajos son mas sensibles pero mas propensos a falsos positivos.
    /// </summary>
    public double UmbralDeteccion { get; set; } = 0.5;
}
