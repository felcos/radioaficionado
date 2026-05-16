namespace RadioAficionado.Dominio.Sdr;

/// <summary>
/// Define la fuente de datos que alimenta el waterfall.
/// Permite cambiar dinámicamente entre audio convencional y SDR.
/// </summary>
public enum FuenteDeDatosWaterfall
{
    /// <summary>
    /// Sin fuente de datos activa.
    /// </summary>
    Ninguna = 0,

    /// <summary>
    /// Fuente de datos desde el pipeline de audio convencional.
    /// </summary>
    Audio = 1,

    /// <summary>
    /// Fuente de datos desde el receptor SDR (muestras IQ).
    /// </summary>
    Sdr = 2
}
