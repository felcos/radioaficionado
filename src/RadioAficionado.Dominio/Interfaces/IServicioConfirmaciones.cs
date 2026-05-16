using RadioAficionado.Dominio.Entidades;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicios externos de confirmación de QSOs soportados por la aplicación.
/// </summary>
public enum ServicioExterno
{
    /// <summary>Logbook of the World (ARRL).</summary>
    LoTW,

    /// <summary>Electronic QSL (eQSL.cc).</summary>
    EQsl,

    /// <summary>Club Log (clublog.org).</summary>
    ClubLog
}

/// <summary>
/// Resultado de una operación de subida de QSOs a un servicio externo.
/// </summary>
/// <param name="Exitoso">Indica si la operación fue exitosa.</param>
/// <param name="QsosSubidos">Cantidad de QSOs aceptados por el servicio.</param>
/// <param name="QsosRechazados">Cantidad de QSOs rechazados por el servicio.</param>
/// <param name="Mensaje">Mensaje descriptivo del resultado o error.</param>
/// <param name="Servicio">Servicio externo al que se subieron los QSOs.</param>
public sealed record ResultadoSubida(
    bool Exitoso,
    int QsosSubidos,
    int QsosRechazados,
    string? Mensaje,
    ServicioExterno Servicio);

/// <summary>
/// Representa la confirmación de un QSO individual por parte de un servicio externo.
/// </summary>
/// <param name="QsoId">Identificador del QSO confirmado.</param>
/// <param name="Servicio">Servicio externo que emitió la confirmación.</param>
/// <param name="FechaConfirmacion">Fecha y hora en que se confirmó el QSO.</param>
/// <param name="Estado">Estado de la confirmación (por ejemplo, "Matched", "Pending").</param>
public sealed record ConfirmacionQso(
    Guid QsoId,
    ServicioExterno Servicio,
    DateTime FechaConfirmacion,
    string? Estado);

/// <summary>
/// Servicio orquestador para gestionar la subida y consulta de confirmaciones de QSOs
/// en servicios externos (LoTW, eQSL, ClubLog).
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Orquesta la subida de QSOs y descarga de confirmaciones desde servicios externos de verificación (LoTW, eQSL, ClubLog). Abstrae los detalles de cada servicio detrás de una interfaz unificada.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se llama a <see cref="SubirQsosAsync"/> para enviar QSOs y a <see cref="ConsultarConfirmacionesAsync"/> para descargar confirmaciones.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Confirmaciones.ServicioConfirmaciones</c>.</para>
/// <para><b>Registro DI:</b> Registrada como Scoped en <c>RadioAficionado.Infraestructura.ConfiguracionServicios.AgregarCapaDeInfraestructura()</c>.</para>
/// <para><b>Configuración necesaria:</b> Credenciales de cada servicio externo (<see cref="ConfiguracionLoTW"/>, <see cref="ConfiguracionEQsl"/>, <see cref="ConfiguracionClubLog"/>).</para>
/// <para><b>Dependencias:</b> <see cref="IClienteLoTW"/>, <see cref="IClienteEQsl"/>, <see cref="IClienteClubLog"/>.</para>
/// </remarks>
public interface IServicioConfirmaciones
{
    /// <summary>
    /// Sube una lista de QSOs al servicio externo especificado.
    /// </summary>
    /// <param name="qsos">Lista de QSOs a subir.</param>
    /// <param name="servicio">Servicio externo de destino.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado de la operación de subida.</returns>
    Task<ResultadoSubida> SubirQsosAsync(IReadOnlyList<Qso> qsos, ServicioExterno servicio, CancellationToken ct = default);

    /// <summary>
    /// Consulta las confirmaciones disponibles en el servicio externo especificado.
    /// </summary>
    /// <param name="servicio">Servicio externo a consultar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de confirmaciones obtenidas.</returns>
    Task<IReadOnlyList<ConfirmacionQso>> ConsultarConfirmacionesAsync(ServicioExterno servicio, CancellationToken ct = default);

    /// <summary>
    /// Verifica si el servicio externo especificado está configurado y disponible.
    /// </summary>
    /// <param name="servicio">Servicio externo a verificar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>True si el servicio está configurado y listo para usar.</returns>
    Task<bool> ObtenerEstadoAsync(ServicioExterno servicio, CancellationToken ct = default);
}
