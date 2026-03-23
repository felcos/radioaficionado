namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para una respuesta individual del foro.
/// </summary>
public class RespuestaViewModel
{
    /// <summary>
    /// Identificador de la respuesta.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Contenido de la respuesta.
    /// </summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>
    /// Indicativo del autor de la respuesta.
    /// </summary>
    public string AutorIndicativo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del autor de la respuesta.
    /// </summary>
    public string AutorNombre { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de creación de la respuesta.
    /// </summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// Fecha de la última edición. Null si no fue editada.
    /// </summary>
    public DateTime? FechaEdicion { get; set; }
}
