using System.ComponentModel.DataAnnotations;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// Modelo de vista para el formulario de inicio de sesión.
/// </summary>
public class IniciarSesionViewModel
{
    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un correo electrónico válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Indica si se debe recordar la sesión del usuario.
    /// </summary>
    [Display(Name = "Recordarme")]
    public bool Recordarme { get; set; }
}
