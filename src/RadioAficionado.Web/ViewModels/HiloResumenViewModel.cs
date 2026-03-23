using RadioAficionado.Dominio.Entidades;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel resumido de un hilo del foro para la vista de listado.
/// </summary>
public class HiloResumenViewModel
{
    /// <summary>
    /// Identificador del hilo.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Título del hilo.
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Indicativo del autor del hilo.
    /// </summary>
    public string AutorIndicativo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del autor del hilo.
    /// </summary>
    public string AutorNombre { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de creación del hilo.
    /// </summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// Fecha de la última respuesta.
    /// </summary>
    public DateTime FechaUltimaRespuesta { get; set; }

    /// <summary>
    /// Categoría del hilo.
    /// </summary>
    public CategoriaForo Categoria { get; set; }

    /// <summary>
    /// Indica si el hilo está fijado.
    /// </summary>
    public bool Fijado { get; set; }

    /// <summary>
    /// Indica si el hilo está cerrado.
    /// </summary>
    public bool Cerrado { get; set; }

    /// <summary>
    /// Número total de respuestas en el hilo.
    /// </summary>
    public int NumeroRespuestas { get; set; }
}
