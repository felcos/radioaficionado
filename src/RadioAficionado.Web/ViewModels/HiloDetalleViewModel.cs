using RadioAficionado.Dominio.Entidades;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para la vista de detalle de un hilo del foro con sus respuestas paginadas.
/// </summary>
public class HiloDetalleViewModel
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
    /// Contenido del mensaje inicial.
    /// </summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>
    /// Indicativo del autor.
    /// </summary>
    public string AutorIndicativo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del autor.
    /// </summary>
    public string AutorNombre { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de creación del hilo.
    /// </summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// Categoría del hilo.
    /// </summary>
    public CategoriaForo Categoria { get; set; }

    /// <summary>
    /// Indica si el hilo está cerrado.
    /// </summary>
    public bool Cerrado { get; set; }

    /// <summary>
    /// Respuestas de la página actual.
    /// </summary>
    public IReadOnlyList<RespuestaViewModel> Respuestas { get; set; } = [];

    /// <summary>
    /// Número de página actual de respuestas (base 1).
    /// </summary>
    public int PaginaActual { get; set; } = 1;

    /// <summary>
    /// Tamaño de página de respuestas.
    /// </summary>
    public int TamanoPagina { get; set; } = 20;

    /// <summary>
    /// Total de respuestas del hilo.
    /// </summary>
    public int TotalRespuestas { get; set; }

    /// <summary>
    /// Total de páginas de respuestas.
    /// </summary>
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRespuestas / TamanoPagina);
}
