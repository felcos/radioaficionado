namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// Modelo de vista para mostrar el perfil del usuario.
/// </summary>
public class PerfilViewModel
{
    /// <summary>
    /// Indicativo de radio del usuario.
    /// </summary>
    public string Indicativo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Localizador Maidenhead del usuario.
    /// </summary>
    public string? Localizador { get; set; }

    /// <summary>
    /// Biografía del usuario.
    /// </summary>
    public string? Biografia { get; set; }

    /// <summary>
    /// Región ITU del usuario (1-3).
    /// </summary>
    public int? RegionItu { get; set; }

    /// <summary>
    /// Fecha de registro del usuario.
    /// </summary>
    public DateTime FechaRegistro { get; set; }
}
