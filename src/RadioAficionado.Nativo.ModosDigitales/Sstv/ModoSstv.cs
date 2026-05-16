namespace RadioAficionado.Nativo.ModosDigitales.Sstv;

/// <summary>
/// Modos SSTV (Slow-Scan Television) soportados.
/// Cada modo tiene diferentes resoluciones, tiempos de transmision y esquemas de color.
/// </summary>
public enum ModoSstv
{
    /// <summary>
    /// Scottie 1 — 320x256, color RGB, ~110 segundos.
    /// </summary>
    Scottie1,

    /// <summary>
    /// Scottie 2 — 320x256, color RGB, ~71 segundos.
    /// </summary>
    Scottie2,

    /// <summary>
    /// Martin 1 — 320x256, color RGB, ~114 segundos.
    /// </summary>
    Martin1,

    /// <summary>
    /// Martin 2 — 320x256, color RGB, ~58 segundos.
    /// </summary>
    Martin2,

    /// <summary>
    /// Robot 36 — 320x240, color YCrCb, ~36 segundos.
    /// </summary>
    Robot36
}
