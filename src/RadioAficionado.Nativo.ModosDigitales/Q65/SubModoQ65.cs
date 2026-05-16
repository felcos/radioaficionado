using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales.Q65;

/// <summary>
/// Submodos de Q65 que determinan la duracion del periodo de transmision.
/// A mayor periodo, mayor sensibilidad para senales debiles.
/// </summary>
public enum SubModoQ65
{
    /// <summary>Q65A — periodo de 15 segundos.</summary>
    A,

    /// <summary>Q65B — periodo de 30 segundos.</summary>
    B,

    /// <summary>Q65C — periodo de 60 segundos.</summary>
    C,

    /// <summary>Q65D — periodo de 120 segundos.</summary>
    D,

    /// <summary>Q65E — periodo de 300 segundos. Maxima sensibilidad (hasta -28 dB SNR).</summary>
    E
}

/// <summary>
/// Metodos de extension para <see cref="SubModoQ65"/>.
/// </summary>
public static class SubModoQ65Extensiones
{
    /// <summary>
    /// Obtiene la duracion del periodo de transmision en segundos para el submodo especificado.
    /// </summary>
    /// <param name="subModo">El submodo Q65.</param>
    /// <returns>Duracion del periodo en segundos.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si el submodo no esta reconocido.</exception>
    public static int ObtenerDuracionPeriodoSegundos(this SubModoQ65 subModo)
    {
        return subModo switch
        {
            SubModoQ65.A => 15,
            SubModoQ65.B => 30,
            SubModoQ65.C => 60,
            SubModoQ65.D => 120,
            SubModoQ65.E => 300,
            _ => throw new ArgumentOutOfRangeException(nameof(subModo), subModo, "Submodo Q65 no reconocido.")
        };
    }

    /// <summary>
    /// Convierte un <see cref="SubModoQ65"/> al <see cref="SubModoOperacion"/> correspondiente.
    /// </summary>
    /// <param name="subModo">El submodo Q65.</param>
    /// <returns>El <see cref="SubModoOperacion"/> equivalente.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si el submodo no esta reconocido.</exception>
    public static SubModoOperacion ASubModoOperacion(this SubModoQ65 subModo)
    {
        return subModo switch
        {
            SubModoQ65.A => SubModoOperacion.Q65A,
            SubModoQ65.B => SubModoOperacion.Q65B,
            SubModoQ65.C => SubModoOperacion.Q65C,
            SubModoQ65.D => SubModoOperacion.Q65D,
            SubModoQ65.E => SubModoOperacion.Q65E,
            _ => throw new ArgumentOutOfRangeException(nameof(subModo), subModo, "Submodo Q65 no reconocido.")
        };
    }
}
