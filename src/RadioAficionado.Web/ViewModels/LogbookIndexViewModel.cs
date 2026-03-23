using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para la vista paginada del logbook público.
/// </summary>
public class LogbookIndexViewModel
{
    /// <summary>
    /// Lista de QSOs de la página actual.
    /// </summary>
    public IReadOnlyList<QsoResumenViewModel> Qsos { get; set; } = [];

    /// <summary>
    /// Número de página actual (base 1).
    /// </summary>
    public int PaginaActual { get; set; } = 1;

    /// <summary>
    /// Tamaño de página (elementos por página).
    /// </summary>
    public int TamanoPagina { get; set; } = 25;

    /// <summary>
    /// Total de elementos que coinciden con el filtro.
    /// </summary>
    public int TotalElementos { get; set; }

    /// <summary>
    /// Total de páginas calculado.
    /// </summary>
    public int TotalPaginas => (int)Math.Ceiling((double)TotalElementos / TamanoPagina);

    /// <summary>
    /// Filtro de indicativo aplicado (búsqueda parcial).
    /// </summary>
    public string? FiltroIndicativo { get; set; }

    /// <summary>
    /// Filtro de modo de operación aplicado.
    /// </summary>
    public ModoOperacion? FiltroModo { get; set; }

    /// <summary>
    /// Filtro de banda aplicado.
    /// </summary>
    public BandaRadio? FiltroBanda { get; set; }

    /// <summary>
    /// Fecha desde para filtrar.
    /// </summary>
    public DateTimeOffset? FiltroFechaDesde { get; set; }

    /// <summary>
    /// Fecha hasta para filtrar.
    /// </summary>
    public DateTimeOffset? FiltroFechaHasta { get; set; }

    /// <summary>
    /// Lista de modos disponibles para el filtro desplegable.
    /// </summary>
    public IReadOnlyList<ModoOperacion> ModosDisponibles { get; set; } = [];

    /// <summary>
    /// Lista de bandas disponibles para el filtro desplegable.
    /// </summary>
    public IReadOnlyList<BandaRadio> BandasDisponibles { get; set; } = [];
}
