using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio de dominio para gestionar activaciones de radio (POTA, SOTA, WWFF, IOTA).
/// Coordina la creación, inicio, completado y consulta de activaciones.
/// </summary>
public interface IServicioActivaciones
{
    /// <summary>
    /// Crea una nueva activación de radio.
    /// </summary>
    /// <param name="tipoActivacion">Tipo de programa de activación.</param>
    /// <param name="referencia">Referencia del lugar activado.</param>
    /// <param name="indicativoActivador">Indicativo del operador activador.</param>
    /// <param name="localizador">Localizador Maidenhead de la ubicación (opcional).</param>
    /// <param name="notas">Notas adicionales (opcional).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La activación creada.</returns>
    Task<Activacion> CrearActivacionAsync(
        TipoActivacion tipoActivacion,
        string referencia,
        Indicativo indicativoActivador,
        Localizador? localizador = null,
        string? notas = null,
        CancellationToken ct = default);

    /// <summary>
    /// Inicia una activación planificada, cambiando su estado a EnCurso.
    /// </summary>
    /// <param name="idActivacion">Identificador de la activación a iniciar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La activación iniciada.</returns>
    Task<Activacion> IniciarAsync(Guid idActivacion, CancellationToken ct = default);

    /// <summary>
    /// Completa una activación en curso, cambiando su estado a Completada.
    /// </summary>
    /// <param name="idActivacion">Identificador de la activación a completar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La activación completada.</returns>
    Task<Activacion> CompletarAsync(Guid idActivacion, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las activaciones de un tipo específico.
    /// </summary>
    /// <param name="tipo">Tipo de activación a filtrar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de solo lectura con las activaciones encontradas.</returns>
    Task<IReadOnlyList<Activacion>> ObtenerActivacionesAsync(TipoActivacion tipo, CancellationToken ct = default);

    /// <summary>
    /// Obtiene la activación actualmente en curso, si existe.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La activación en curso, o null si no hay ninguna.</returns>
    Task<Activacion?> ObtenerActivacionActualAsync(CancellationToken ct = default);
}
