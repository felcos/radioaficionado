namespace RadioAficionado.Web.Api.Dtos;

/// <summary>
/// DTO con parámetros de consulta para filtrar y paginar QSOs en la API.
/// </summary>
public sealed class FiltroQsoDto
{
    /// <summary>
    /// Número de página (base 1). Por defecto 1.
    /// </summary>
    public int Pagina { get; set; } = 1;

    /// <summary>
    /// Cantidad de elementos por página. Por defecto 50, máximo 200.
    /// </summary>
    public int Tamano { get; set; } = 50;

    /// <summary>
    /// Indicativo parcial a buscar (propio o contacto). Null para no filtrar.
    /// </summary>
    public string? Indicativo { get; set; }

    /// <summary>
    /// Banda de radio a filtrar (ej: "20m", "40m"). Null para no filtrar.
    /// </summary>
    public string? Banda { get; set; }

    /// <summary>
    /// Modo de operación a filtrar (ej: "FT8", "SSB"). Null para no filtrar.
    /// </summary>
    public string? Modo { get; set; }

    /// <summary>
    /// Fecha de inicio del rango (inclusive, UTC). Null para no limitar.
    /// </summary>
    public DateTimeOffset? FechaDesde { get; set; }

    /// <summary>
    /// Fecha de fin del rango (inclusive, UTC). Null para no limitar.
    /// </summary>
    public DateTimeOffset? FechaHasta { get; set; }
}
