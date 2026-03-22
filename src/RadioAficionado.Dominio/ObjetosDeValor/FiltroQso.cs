namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Filtros para la consulta paginada de QSOs en el logbook.
/// </summary>
/// <param name="Indicativo">Indicativo parcial a buscar (propio o contacto).</param>
/// <param name="Banda">Banda de radio a filtrar. Null para no filtrar.</param>
/// <param name="Modo">Modo de operación a filtrar. Null para no filtrar.</param>
/// <param name="FechaDesde">Fecha de inicio del rango (inclusive). Null para no limitar.</param>
/// <param name="FechaHasta">Fecha de fin del rango (inclusive). Null para no limitar.</param>
public sealed record FiltroQso(
    string? Indicativo = null,
    BandaRadio? Banda = null,
    ModoOperacion? Modo = null,
    DateTimeOffset? FechaDesde = null,
    DateTimeOffset? FechaHasta = null);
