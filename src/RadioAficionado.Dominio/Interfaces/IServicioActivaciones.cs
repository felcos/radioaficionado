using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio de dominio para gestionar activaciones de radio (POTA, SOTA, WWFF, IOTA).
/// Coordina la creación, inicio, completado y consulta de activaciones.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Orquesta el ciclo de vida de activaciones de radio portátiles (POTA, SOTA, WWFF, IOTA): creación, inicio, registro de QSOs durante la activación y finalización.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se llama a <see cref="CrearActivacionAsync"/> para planificar, <see cref="IniciarAsync"/> para activar, y <see cref="CompletarAsync"/> para finalizar.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Activaciones.ServicioActivaciones</c>.</para>
/// <para><b>Registro DI:</b> Registrada como Scoped en <c>RadioAficionado.Infraestructura.ConfiguracionServicios.AgregarCapaDeInfraestructura()</c>.</para>
/// <para><b>Configuración necesaria:</b> Ninguna configuración adicional. Opera con los repositorios registrados.</para>
/// <para><b>Dependencias:</b> <see cref="IRepositorioActivaciones"/>, <see cref="IUnidadDeTrabajo"/>.</para>
/// </remarks>
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
    /// Cancela una activación planificada o en curso.
    /// </summary>
    /// <param name="idActivacion">Identificador de la activación a cancelar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La activación cancelada.</returns>
    Task<Activacion> CancelarAsync(Guid idActivacion, CancellationToken ct = default);

    /// <summary>
    /// Obtiene la activación actualmente en curso, si existe.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La activación en curso, o null si no hay ninguna.</returns>
    Task<Activacion?> ObtenerActivacionActualAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las activaciones sin filtrar por tipo.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de solo lectura con todas las activaciones.</returns>
    Task<IReadOnlyList<Activacion>> ObtenerTodasAsync(CancellationToken ct = default);
}
