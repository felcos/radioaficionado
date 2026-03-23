using RadioAficionado.Dominio.Entidades;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para la vista principal del foro con lista paginada de hilos.
/// </summary>
public class ForoIndexViewModel
{
    /// <summary>
    /// Lista de hilos de la página actual.
    /// </summary>
    public IReadOnlyList<HiloResumenViewModel> Hilos { get; set; } = [];

    /// <summary>
    /// Número de página actual (base 1).
    /// </summary>
    public int PaginaActual { get; set; } = 1;

    /// <summary>
    /// Tamaño de página (elementos por página).
    /// </summary>
    public int TamanoPagina { get; set; } = 20;

    /// <summary>
    /// Total de elementos que coinciden con el filtro.
    /// </summary>
    public int TotalElementos { get; set; }

    /// <summary>
    /// Total de páginas calculado.
    /// </summary>
    public int TotalPaginas => (int)Math.Ceiling((double)TotalElementos / TamanoPagina);

    /// <summary>
    /// Filtro de categoría aplicado. Null muestra todas.
    /// </summary>
    public CategoriaForo? FiltroCategoria { get; set; }
}
