using Microsoft.AspNetCore.Identity;

namespace RadioAficionado.Dominio.Entidades;

/// <summary>
/// Representa un usuario registrado de la plataforma RadioAficionado.
/// Extiende la identidad base de ASP.NET con datos específicos de radioaficionado.
/// </summary>
public class UsuarioRadio : IdentityUser
{
    /// <summary>
    /// Indicativo de radio del usuario (e.g., EA4ABC, LU1XYZ).
    /// Es obligatorio y único para cada usuario.
    /// </summary>
    public string Indicativo { get; set; } = string.Empty;

    /// <summary>
    /// Localizador Maidenhead del usuario (e.g., IN80dk).
    /// Opcional — indica la ubicación geográfica aproximada de la estación.
    /// </summary>
    public string? Localizador { get; set; }

    /// <summary>
    /// Nombre completo del usuario.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de registro del usuario en la plataforma (UTC).
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// Biografía o descripción del usuario. Opcional.
    /// </summary>
    public string? Biografia { get; set; }

    /// <summary>
    /// Región ITU del usuario (1-3). Opcional.
    /// Región 1: Europa, África, Oriente Medio.
    /// Región 2: América.
    /// Región 3: Asia, Oceanía.
    /// </summary>
    public int? RegionItu { get; set; }
}
