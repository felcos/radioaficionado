namespace RadioAficionado.Dominio.Awards;

/// <summary>
/// Catalogo de los 50 estados de Estados Unidos para el diploma WAS (Worked All States).
/// Incluye abreviatura, nombre completo y prefijos de indicativo asociados.
/// </summary>
public static class CatalogoEstadosUsa
{
    /// <summary>
    /// Representa un estado de EEUU con su abreviatura y nombre.
    /// </summary>
    /// <param name="Abreviatura">Codigo de 2 letras (AL, AK, AZ...).</param>
    /// <param name="Nombre">Nombre completo del estado.</param>
    public sealed record EstadoUsa(string Abreviatura, string Nombre);

    /// <summary>
    /// Lista completa de los 50 estados de EEUU.
    /// </summary>
    public static IReadOnlyList<EstadoUsa> Estados { get; } =
    [
        new("AL", "Alabama"),
        new("AK", "Alaska"),
        new("AZ", "Arizona"),
        new("AR", "Arkansas"),
        new("CA", "California"),
        new("CO", "Colorado"),
        new("CT", "Connecticut"),
        new("DE", "Delaware"),
        new("FL", "Florida"),
        new("GA", "Georgia"),
        new("HI", "Hawaii"),
        new("ID", "Idaho"),
        new("IL", "Illinois"),
        new("IN", "Indiana"),
        new("IA", "Iowa"),
        new("KS", "Kansas"),
        new("KY", "Kentucky"),
        new("LA", "Louisiana"),
        new("ME", "Maine"),
        new("MD", "Maryland"),
        new("MA", "Massachusetts"),
        new("MI", "Michigan"),
        new("MN", "Minnesota"),
        new("MS", "Mississippi"),
        new("MO", "Missouri"),
        new("MT", "Montana"),
        new("NE", "Nebraska"),
        new("NV", "Nevada"),
        new("NH", "New Hampshire"),
        new("NJ", "New Jersey"),
        new("NM", "New Mexico"),
        new("NY", "New York"),
        new("NC", "North Carolina"),
        new("ND", "North Dakota"),
        new("OH", "Ohio"),
        new("OK", "Oklahoma"),
        new("OR", "Oregon"),
        new("PA", "Pennsylvania"),
        new("RI", "Rhode Island"),
        new("SC", "South Carolina"),
        new("SD", "South Dakota"),
        new("TN", "Tennessee"),
        new("TX", "Texas"),
        new("UT", "Utah"),
        new("VT", "Vermont"),
        new("VA", "Virginia"),
        new("WA", "Washington"),
        new("WV", "West Virginia"),
        new("WI", "Wisconsin"),
        new("WY", "Wyoming")
    ];

    /// <summary>
    /// Total de estados (siempre 50).
    /// </summary>
    public static int TotalEstados => 50;

    /// <summary>
    /// Obtiene un estado por su abreviatura.
    /// </summary>
    /// <param name="abreviatura">Codigo de 2 letras.</param>
    /// <returns>El estado si existe; null si no.</returns>
    public static EstadoUsa? ObtenerPorAbreviatura(string abreviatura)
    {
        if (string.IsNullOrWhiteSpace(abreviatura))
        {
            return null;
        }

        string upper = abreviatura.Trim().ToUpperInvariant();

        foreach (EstadoUsa estado in Estados)
        {
            if (estado.Abreviatura == upper)
            {
                return estado;
            }
        }

        return null;
    }
}
