namespace RadioAficionado.Dominio.Satelites;

/// <summary>
/// Representa un paso (pase) de un satélite sobre una ubicación del observador.
/// Incluye los momentos de adquisición y pérdida de señal, y la geometría del paso.
/// </summary>
public sealed class PasoSatelite
{
    /// <summary>
    /// Satélite que realiza el paso.
    /// </summary>
    public SateliteAmateur Satelite { get; }

    /// <summary>
    /// Acquisition of Signal — momento en que el satélite aparece sobre el horizonte.
    /// </summary>
    public DateTime Aos { get; }

    /// <summary>
    /// Loss of Signal — momento en que el satélite desaparece bajo el horizonte.
    /// </summary>
    public DateTime Los { get; }

    /// <summary>
    /// Elevación máxima alcanzada durante el paso, en grados (0-90).
    /// </summary>
    public double ElevacionMaxima { get; }

    /// <summary>
    /// Azimut de aparición del satélite en grados (0-360, Norte=0).
    /// </summary>
    public double AzimutAos { get; }

    /// <summary>
    /// Azimut de desaparición del satélite en grados (0-360, Norte=0).
    /// </summary>
    public double AzimutLos { get; }

    /// <summary>
    /// Crea una nueva instancia de <see cref="PasoSatelite"/> validando la coherencia de los datos.
    /// </summary>
    /// <param name="satelite">Satélite que realiza el paso.</param>
    /// <param name="aos">Momento de adquisición de señal.</param>
    /// <param name="los">Momento de pérdida de señal.</param>
    /// <param name="elevacionMaxima">Elevación máxima en grados.</param>
    /// <param name="azimutAos">Azimut de aparición en grados.</param>
    /// <param name="azimutLos">Azimut de desaparición en grados.</param>
    /// <exception cref="ArgumentNullException">Si el satélite es nulo.</exception>
    /// <exception cref="ArgumentException">Si LOS es anterior a AOS.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si la elevación está fuera de rango.</exception>
    public PasoSatelite(
        SateliteAmateur satelite,
        DateTime aos,
        DateTime los,
        double elevacionMaxima,
        double azimutAos,
        double azimutLos)
    {
        ArgumentNullException.ThrowIfNull(satelite);

        if (los <= aos)
        {
            throw new ArgumentException(
                "La pérdida de señal (LOS) debe ser posterior a la adquisición (AOS).",
                nameof(los));
        }

        if (elevacionMaxima < 0.0 || elevacionMaxima > 90.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elevacionMaxima),
                elevacionMaxima,
                "La elevación máxima debe estar entre 0 y 90 grados.");
        }

        Satelite = satelite;
        Aos = aos;
        Los = los;
        ElevacionMaxima = elevacionMaxima;
        AzimutAos = azimutAos;
        AzimutLos = azimutLos;
    }

    /// <summary>
    /// Duración total del paso en segundos.
    /// </summary>
    public double DuracionSegundos => (Los - Aos).TotalSeconds;

    /// <summary>
    /// Indica si el paso es de alta elevación (mayor a 45 grados), ideal para comunicaciones.
    /// </summary>
    public bool EsAltaElevacion => ElevacionMaxima >= 45.0;

    /// <summary>
    /// Devuelve una representación textual del paso.
    /// </summary>
    public override string ToString()
    {
        return $"{Satelite.Nombre}: AOS {Aos:HH:mm:ss} → LOS {Los:HH:mm:ss} " +
               $"(Elev máx: {ElevacionMaxima:F1}°, Duración: {DuracionSegundos:F0}s)";
    }
}
