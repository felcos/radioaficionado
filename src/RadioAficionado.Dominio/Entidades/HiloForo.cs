namespace RadioAficionado.Dominio.Entidades;

/// <summary>
/// Representa un hilo (tema) del foro de la comunidad.
/// </summary>
public class HiloForo
{
    /// <summary>
    /// Identificador único del hilo.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Título del hilo.
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del mensaje inicial del hilo.
    /// </summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del usuario autor del hilo.
    /// </summary>
    public string AutorId { get; set; } = string.Empty;

    /// <summary>
    /// Navegación al usuario autor del hilo.
    /// </summary>
    public UsuarioRadio? Autor { get; set; }

    /// <summary>
    /// Fecha y hora de creación del hilo (UTC).
    /// </summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// Fecha y hora de la última respuesta (UTC). Igual a FechaCreacion si no hay respuestas.
    /// </summary>
    public DateTime FechaUltimaRespuesta { get; set; }

    /// <summary>
    /// Categoría del hilo.
    /// </summary>
    public CategoriaForo Categoria { get; set; }

    /// <summary>
    /// Indica si el hilo está fijado (sticky) en la parte superior de la lista.
    /// </summary>
    public bool Fijado { get; set; }

    /// <summary>
    /// Indica si el hilo está cerrado y no acepta nuevas respuestas.
    /// </summary>
    public bool Cerrado { get; set; }

    /// <summary>
    /// Colección de respuestas del hilo.
    /// </summary>
    public ICollection<RespuestaForo> Respuestas { get; set; } = new List<RespuestaForo>();
}
