namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para la página de listado de operadores registrados, con paginación y búsqueda.
/// </summary>
public class OperadoresIndexViewModel
{
    /// <summary>
    /// Lista de operadores de la página actual.
    /// </summary>
    public IReadOnlyList<OperadorResumenViewModel> Operadores { get; set; } = [];

    /// <summary>
    /// Número de página actual (base 1).
    /// </summary>
    public int PaginaActual { get; set; } = 1;

    /// <summary>
    /// Tamaño de página (elementos por página).
    /// </summary>
    public int TamanoPagina { get; set; } = 20;

    /// <summary>
    /// Total de operadores que coinciden con la búsqueda.
    /// </summary>
    public int TotalElementos { get; set; }

    /// <summary>
    /// Total de páginas calculado.
    /// </summary>
    public int TotalPaginas => (int)Math.Ceiling((double)TotalElementos / TamanoPagina);

    /// <summary>
    /// Término de búsqueda aplicado (indicativo o nombre).
    /// </summary>
    public string? Busqueda { get; set; }
}

/// <summary>
/// ViewModel resumido de un operador para el listado.
/// </summary>
public class OperadorResumenViewModel
{
    /// <summary>
    /// Indicativo de radio del operador.
    /// </summary>
    public string Indicativo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del operador.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Localizador Maidenhead del operador.
    /// </summary>
    public string? Localizador { get; set; }

    /// <summary>
    /// Fecha de registro en la plataforma.
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// Número total de QSOs del operador.
    /// </summary>
    public int TotalQsos { get; set; }
}
