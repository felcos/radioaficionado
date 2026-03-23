namespace RadioAficionado.Dominio.Entidades;

/// <summary>
/// Representa una respuesta a un hilo del foro.
/// </summary>
public class RespuestaForo
{
    /// <summary>
    /// Identificador único de la respuesta.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identificador del hilo al que pertenece la respuesta.
    /// </summary>
    public Guid HiloId { get; set; }

    /// <summary>
    /// Navegación al hilo padre.
    /// </summary>
    public HiloForo? Hilo { get; set; }

    /// <summary>
    /// Contenido de la respuesta.
    /// </summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del usuario autor de la respuesta.
    /// </summary>
    public string AutorId { get; set; } = string.Empty;

    /// <summary>
    /// Navegación al usuario autor de la respuesta.
    /// </summary>
    public UsuarioRadio? Autor { get; set; }

    /// <summary>
    /// Fecha y hora de creación de la respuesta (UTC).
    /// </summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// Fecha y hora de la última edición (UTC). Null si nunca fue editada.
    /// </summary>
    public DateTime? FechaEdicion { get; set; }
}
