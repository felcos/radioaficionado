namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para la página de perfil público de un operador.
/// Incluye datos del usuario y estadísticas de QSOs.
/// </summary>
public class PerfilPublicoViewModel
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
    /// Biografía del operador.
    /// </summary>
    public string? Biografia { get; set; }

    /// <summary>
    /// Región ITU del operador (1-3).
    /// </summary>
    public int? RegionItu { get; set; }

    /// <summary>
    /// Fecha de registro en la plataforma.
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// Número total de QSOs del operador.
    /// </summary>
    public int TotalQsos { get; set; }

    /// <summary>
    /// Número de indicativos únicos contactados (DXCC aproximado).
    /// </summary>
    public int IndicativosUnicos { get; set; }

    /// <summary>
    /// Bandas más utilizadas por el operador, ordenadas por frecuencia de uso.
    /// </summary>
    public IReadOnlyList<BandaFavoritaViewModel> BandasFavoritas { get; set; } = [];

    /// <summary>
    /// Últimos QSOs públicos del operador.
    /// </summary>
    public IReadOnlyList<QsoResumenViewModel> UltimosQsos { get; set; } = [];
}

/// <summary>
/// ViewModel para representar una banda favorita con su conteo de QSOs.
/// </summary>
public class BandaFavoritaViewModel
{
    /// <summary>
    /// Nombre de la banda (ej: "20 metros").
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Número de QSOs realizados en esta banda.
    /// </summary>
    public int CantidadQsos { get; set; }
}
