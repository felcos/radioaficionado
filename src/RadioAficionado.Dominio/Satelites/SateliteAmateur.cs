namespace RadioAficionado.Dominio.Satelites;

/// <summary>
/// Satélite amateur con su información de identificación y transponders disponibles.
/// </summary>
public sealed class SateliteAmateur
{
    /// <summary>
    /// Número de catálogo NORAD del satélite.
    /// </summary>
    public int NumeroNorad { get; }

    /// <summary>
    /// Nombre común del satélite (ej. "ISS", "SO-50").
    /// </summary>
    public string Nombre { get; }

    /// <summary>
    /// Indicativo de radioaficionado del satélite (ej. "RS0ISS", "NO-84").
    /// </summary>
    public string Indicativo { get; }

    /// <summary>
    /// Lista de transponders disponibles en el satélite.
    /// </summary>
    public IReadOnlyList<TransponderSatelite> Transponders { get; }

    /// <summary>
    /// Indica si el satélite está operativo actualmente.
    /// </summary>
    public bool Activo { get; }

    /// <summary>
    /// Crea una nueva instancia de <see cref="SateliteAmateur"/> validando los datos.
    /// </summary>
    /// <param name="numeroNorad">Número de catálogo NORAD.</param>
    /// <param name="nombre">Nombre común del satélite.</param>
    /// <param name="indicativo">Indicativo de radioaficionado.</param>
    /// <param name="transponders">Lista de transponders disponibles.</param>
    /// <param name="activo">Indica si está operativo.</param>
    /// <exception cref="ArgumentOutOfRangeException">Si el número NORAD es inválido.</exception>
    /// <exception cref="ArgumentException">Si el nombre o indicativo están vacíos.</exception>
    public SateliteAmateur(
        int numeroNorad,
        string nombre,
        string indicativo,
        IReadOnlyList<TransponderSatelite>? transponders,
        bool activo)
    {
        if (numeroNorad <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numeroNorad),
                numeroNorad,
                "El número NORAD debe ser positivo.");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "El nombre del satélite no puede estar vacío.",
                nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(indicativo))
        {
            throw new ArgumentException(
                "El indicativo del satélite no puede estar vacío.",
                nameof(indicativo));
        }

        NumeroNorad = numeroNorad;
        Nombre = nombre;
        Indicativo = indicativo;
        Transponders = transponders ?? Array.Empty<TransponderSatelite>();
        Activo = activo;
    }

    /// <summary>
    /// Devuelve una representación textual del satélite.
    /// </summary>
    public override string ToString()
    {
        string estado = Activo ? "Activo" : "Inactivo";
        return $"{Nombre} ({Indicativo}) - NORAD {NumeroNorad} [{estado}] - {Transponders.Count} transponder(es)";
    }
}
