using System.ComponentModel.DataAnnotations;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para el formulario de respuesta a un hilo del foro.
/// </summary>
public class ResponderHiloViewModel
{
    /// <summary>
    /// Identificador del hilo al que se responde.
    /// </summary>
    [Required]
    public Guid HiloId { get; set; }

    /// <summary>
    /// Contenido de la respuesta.
    /// </summary>
    [Required(ErrorMessage = "La respuesta no puede estar vacía.")]
    [MinLength(5, ErrorMessage = "La respuesta debe tener al menos 5 caracteres.")]
    [Display(Name = "Respuesta")]
    public string Contenido { get; set; } = string.Empty;
}
