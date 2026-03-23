using System.ComponentModel.DataAnnotations;
using RadioAficionado.Dominio.Entidades;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para el formulario de creación de un nuevo hilo en el foro.
/// </summary>
public class CrearHiloViewModel
{
    /// <summary>
    /// Título del nuevo hilo.
    /// </summary>
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 200 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Categoría del nuevo hilo.
    /// </summary>
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Display(Name = "Categoría")]
    public CategoriaForo Categoria { get; set; }

    /// <summary>
    /// Contenido del mensaje inicial del hilo.
    /// </summary>
    [Required(ErrorMessage = "El contenido es obligatorio.")]
    [MinLength(10, ErrorMessage = "El contenido debe tener al menos 10 caracteres.")]
    [Display(Name = "Contenido")]
    public string Contenido { get; set; } = string.Empty;
}
