using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Repositorio para operaciones de persistencia de contactos de radio (QSOs).
/// </summary>
public interface IRepositorioQso
{
    /// <summary>
    /// Obtiene un QSO por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del QSO.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El QSO encontrado, o null si no existe.</returns>
    Task<Qso?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Obtiene todos los QSOs registrados.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de solo lectura con todos los QSOs.</returns>
    Task<IReadOnlyList<Qso>> ObtenerTodosAsync(CancellationToken ct);

    /// <summary>
    /// Busca QSOs que involucren un indicativo específico (como propio o como contacto).
    /// </summary>
    /// <param name="indicativo">El indicativo a buscar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de solo lectura con los QSOs encontrados.</returns>
    Task<IReadOnlyList<Qso>> BuscarPorIndicativoAsync(Indicativo indicativo, CancellationToken ct);

    /// <summary>
    /// Agrega un nuevo QSO al repositorio.
    /// </summary>
    /// <param name="qso">El QSO a agregar.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task AgregarAsync(Qso qso, CancellationToken ct);

    /// <summary>
    /// Actualiza un QSO existente en el repositorio.
    /// </summary>
    /// <param name="qso">El QSO con los datos actualizados.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ActualizarAsync(Qso qso, CancellationToken ct);

    /// <summary>
    /// Obtiene el número total de QSOs registrados.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Cantidad de QSOs.</returns>
    Task<int> ContarAsync(CancellationToken ct);
}
