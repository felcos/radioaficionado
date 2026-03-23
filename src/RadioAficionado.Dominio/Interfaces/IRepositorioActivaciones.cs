using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Repositorio para operaciones de persistencia de activaciones de radio (POTA, SOTA, etc.).
/// </summary>
public interface IRepositorioActivaciones
{
    /// <summary>
    /// Agrega una nueva activación al repositorio.
    /// </summary>
    /// <param name="activacion">La activación a agregar.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task AgregarAsync(Activacion activacion, CancellationToken ct);

    /// <summary>
    /// Actualiza una activación existente en el repositorio.
    /// </summary>
    /// <param name="activacion">La activación con los datos actualizados.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ActualizarAsync(Activacion activacion, CancellationToken ct);

    /// <summary>
    /// Obtiene una activación por su identificador único.
    /// </summary>
    /// <param name="id">Identificador de la activación.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La activación encontrada, o null si no existe.</returns>
    Task<Activacion?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Obtiene todas las activaciones de un tipo específico.
    /// </summary>
    /// <param name="tipo">Tipo de activación a filtrar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de solo lectura con las activaciones del tipo indicado.</returns>
    Task<IReadOnlyList<Activacion>> ObtenerPorTipoAsync(TipoActivacion tipo, CancellationToken ct);

    /// <summary>
    /// Obtiene la activación actualmente en curso, si existe.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La activación en estado EnCurso, o null si no hay ninguna.</returns>
    Task<Activacion?> ObtenerActivaAsync(CancellationToken ct);

    /// <summary>
    /// Obtiene todas las activaciones sin filtrar por tipo.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de solo lectura con todas las activaciones.</returns>
    Task<IReadOnlyList<Activacion>> ObtenerTodasAsync(CancellationToken ct);
}
