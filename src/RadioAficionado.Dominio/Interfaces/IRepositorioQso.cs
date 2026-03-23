using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Resultado de una consulta paginada de QSOs.
/// </summary>
/// <param name="Elementos">QSOs de la página solicitada.</param>
/// <param name="TotalElementos">Cantidad total de QSOs que coinciden con el filtro.</param>
public sealed record ResultadoPaginado<T>(IReadOnlyList<T> Elementos, int TotalElementos);

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

    /// <summary>
    /// Obtiene una página de QSOs aplicando filtros opcionales, ordenados por fecha descendente.
    /// </summary>
    /// <param name="pagina">Número de página (base 1).</param>
    /// <param name="tamano">Cantidad de elementos por página.</param>
    /// <param name="filtro">Filtros opcionales. Null para obtener todos.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado paginado con los QSOs y el total de elementos.</returns>
    Task<ResultadoPaginado<Qso>> ObtenerPaginadoAsync(int pagina, int tamano, FiltroQso? filtro, CancellationToken ct);

    /// <summary>
    /// Cuenta los QSOs que coinciden con un filtro.
    /// </summary>
    /// <param name="filtro">Filtros opcionales. Null para contar todos.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Cantidad de QSOs que coinciden.</returns>
    Task<int> ContarConFiltroAsync(FiltroQso? filtro, CancellationToken ct);

    /// <summary>
    /// Elimina un QSO del repositorio.
    /// </summary>
    /// <param name="qso">El QSO a eliminar.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task EliminarAsync(Qso qso, CancellationToken ct);

    /// <summary>
    /// Verifica si ya existe un QSO con el mismo indicativo contacto, fecha/hora de inicio, frecuencia y modo.
    /// </summary>
    /// <param name="indicativoContacto">Indicativo de la estación contactada.</param>
    /// <param name="fechaHoraInicio">Fecha y hora de inicio del contacto.</param>
    /// <param name="frecuencia">Frecuencia utilizada.</param>
    /// <param name="modo">Modo de operación.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si ya existe un QSO con esos parámetros.</returns>
    Task<bool> ExisteDuplicadoAsync(
        Indicativo indicativoContacto,
        DateTimeOffset fechaHoraInicio,
        Frecuencia frecuencia,
        ModoOperacion modo,
        CancellationToken ct);
}
