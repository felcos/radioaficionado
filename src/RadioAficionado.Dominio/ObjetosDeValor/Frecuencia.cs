namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Frecuencia de radio representada como objeto de valor inmutable.
/// Se almacena internamente en hercios (Hz).
/// </summary>
public readonly record struct Frecuencia : IEquatable<Frecuencia>, IComparable<Frecuencia>
{
    /// <summary>
    /// Frecuencia en hercios.
    /// </summary>
    public long Hz { get; }

    /// <summary>
    /// Frecuencia en kilohercios.
    /// </summary>
    public double KHz => Hz / 1_000.0;

    /// <summary>
    /// Frecuencia en megahercios.
    /// </summary>
    public double MHz => Hz / 1_000_000.0;

    private Frecuencia(long hz)
    {
        if (hz <= 0)
        {
            throw new ArgumentException(
                "La frecuencia debe ser un valor positivo.",
                nameof(hz));
        }

        Hz = hz;
    }

    /// <summary>
    /// Crea una frecuencia a partir de un valor en hercios.
    /// </summary>
    /// <param name="hz">Valor en hercios (debe ser positivo).</param>
    /// <returns>Nueva instancia de <see cref="Frecuencia"/>.</returns>
    /// <exception cref="ArgumentException">Si el valor no es positivo.</exception>
    public static Frecuencia DesdeHz(long hz)
    {
        return new Frecuencia(hz);
    }

    /// <summary>
    /// Crea una frecuencia a partir de un valor en kilohercios.
    /// </summary>
    /// <param name="khz">Valor en kilohercios (debe ser positivo).</param>
    /// <returns>Nueva instancia de <see cref="Frecuencia"/>.</returns>
    /// <exception cref="ArgumentException">Si el valor no es positivo.</exception>
    public static Frecuencia DesdeKHz(double khz)
    {
        return new Frecuencia((long)(khz * 1_000.0));
    }

    /// <summary>
    /// Crea una frecuencia a partir de un valor en megahercios.
    /// </summary>
    /// <param name="mhz">Valor en megahercios (debe ser positivo).</param>
    /// <returns>Nueva instancia de <see cref="Frecuencia"/>.</returns>
    /// <exception cref="ArgumentException">Si el valor no es positivo.</exception>
    public static Frecuencia DesdeMHz(double mhz)
    {
        return new Frecuencia((long)(mhz * 1_000_000.0));
    }

    /// <summary>
    /// Determina a qué banda de radioaficionado pertenece esta frecuencia.
    /// </summary>
    /// <returns>La <see cref="BandaRadio"/> correspondiente, o null si no pertenece a ninguna banda.</returns>
    public BandaRadio? ObtenerBanda()
    {
        return BandaRadioExtensiones.DesdeFrecuencia(this);
    }

    /// <summary>
    /// Compara esta frecuencia con otra para ordenamiento.
    /// </summary>
    /// <param name="other">Otra frecuencia a comparar.</param>
    /// <returns>Valor negativo si es menor, cero si es igual, positivo si es mayor.</returns>
    public int CompareTo(Frecuencia other)
    {
        return Hz.CompareTo(other.Hz);
    }

    /// <summary>
    /// Devuelve la frecuencia en formato legible (por ejemplo, "14.074 MHz" o "7100.000 KHz").
    /// </summary>
    /// <returns>Representación textual de la frecuencia.</returns>
    public override string ToString()
    {
        if (Hz >= 1_000_000)
        {
            return $"{MHz:F3} MHz";
        }

        if (Hz >= 1_000)
        {
            return $"{KHz:F3} KHz";
        }

        return $"{Hz} Hz";
    }
}
