using System.ComponentModel.DataAnnotations;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// Modelo de vista para el formulario de edición de perfil.
/// </summary>
public class EditarPerfilViewModel
{
    /// <summary>
    /// Indicativo de radio del usuario.
    /// </summary>
    [Required(ErrorMessage = "El indicativo es obligatorio.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "El indicativo debe tener entre 3 y 20 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9/]+$", ErrorMessage = "El indicativo solo puede contener letras, números y /.")]
    [Display(Name = "Indicativo")]
    public string Indicativo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario.
    /// </summary>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 200 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Localizador Maidenhead (opcional).
    /// </summary>
    [StringLength(8, ErrorMessage = "El localizador no puede superar 8 caracteres.")]
    [RegularExpression(@"^[A-Ra-r]{2}[0-9]{2}([A-Xa-x]{2}([0-9]{2})?)?$",
        ErrorMessage = "El localizador Maidenhead no es válido (ej: IN80dk).")]
    [Display(Name = "Localizador Maidenhead")]
    public string? Localizador { get; set; }

    /// <summary>
    /// Biografía del usuario (opcional).
    /// </summary>
    [StringLength(2000, ErrorMessage = "La biografía no puede superar 2000 caracteres.")]
    [Display(Name = "Biografía")]
    public string? Biografia { get; set; }

    /// <summary>
    /// Región ITU (opcional, 1-3).
    /// </summary>
    [Range(1, 3, ErrorMessage = "La región ITU debe ser 1, 2 o 3.")]
    [Display(Name = "Región ITU")]
    public int? RegionItu { get; set; }
}
