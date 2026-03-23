namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para la página de inicio con estadísticas generales del logbook.
/// </summary>
public class InicioViewModel
{
    /// <summary>
    /// Número total de QSOs registrados en el sistema.
    /// </summary>
    public int TotalQsos { get; set; }

    /// <summary>
    /// Número de indicativos únicos contactados.
    /// </summary>
    public int TotalIndicativosUnicos { get; set; }

    /// <summary>
    /// Número de bandas distintas utilizadas.
    /// </summary>
    public int TotalBandas { get; set; }

    /// <summary>
    /// Número de modos distintos utilizados.
    /// </summary>
    public int TotalModos { get; set; }

    /// <summary>
    /// Fecha y hora del QSO más reciente. Null si no hay QSOs.
    /// </summary>
    public DateTimeOffset? UltimoQso { get; set; }

    /// <summary>
    /// Los últimos 5 QSOs registrados para mostrar en la página de inicio.
    /// </summary>
    public IReadOnlyList<QsoResumenViewModel> UltimosQsos { get; set; } = [];
}

/// <summary>
/// ViewModel resumido de un QSO para listados breves.
/// </summary>
public class QsoResumenViewModel
{
    /// <summary>
    /// Identificador único del QSO.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Indicativo de la estación propia.
    /// </summary>
    public string IndicativoPropio { get; set; } = string.Empty;

    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    public string IndicativoContacto { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora del contacto formateada.
    /// </summary>
    public DateTimeOffset FechaHora { get; set; }

    /// <summary>
    /// Frecuencia en formato legible (ej: "14.074 MHz").
    /// </summary>
    public string Frecuencia { get; set; } = string.Empty;

    /// <summary>
    /// Modo de operación utilizado.
    /// </summary>
    public string Modo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la banda (ej: "20 metros"). Null si la frecuencia no corresponde a ninguna banda.
    /// </summary>
    public string? Banda { get; set; }
}
