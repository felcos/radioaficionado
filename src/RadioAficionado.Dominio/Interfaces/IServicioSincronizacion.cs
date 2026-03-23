namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Configuración necesaria para conectar con el servidor de sincronización.
/// </summary>
/// <param name="UrlServidor">URL base del servidor de la API web (ej. https://miservidor.com).</param>
/// <param name="Token">Token de autenticación para el servidor.</param>
/// <param name="IndicativoPropio">Indicativo del operador local.</param>
/// <param name="SincronizacionAutomatica">Indica si la sincronización periódica está habilitada.</param>
/// <param name="IntervaloMinutos">Intervalo en minutos entre sincronizaciones automáticas.</param>
public sealed record ConfiguracionSincronizacion(
    string UrlServidor,
    string Token,
    string IndicativoPropio,
    bool SincronizacionAutomatica = false,
    int IntervaloMinutos = 15);

/// <summary>
/// Resultado de una operación de sincronización con el servidor.
/// </summary>
/// <param name="QsosEnviados">Cantidad de QSOs enviados al servidor.</param>
/// <param name="QsosRecibidos">Cantidad de QSOs recibidos del servidor.</param>
/// <param name="QsosDuplicados">Cantidad de QSOs que ya existían en ambos lados.</param>
/// <param name="Errores">Lista de errores ocurridos durante la sincronización.</param>
/// <param name="FechaSincronizacion">Fecha y hora en que se realizó la sincronización.</param>
public sealed record ResultadoSincronizacion(
    int QsosEnviados,
    int QsosRecibidos,
    int QsosDuplicados,
    IReadOnlyList<string> Errores,
    DateTimeOffset FechaSincronizacion);

/// <summary>
/// Estado actual del servicio de sincronización.
/// </summary>
/// <param name="UltimaSincronizacion">Fecha de la última sincronización exitosa. Null si nunca se ha sincronizado.</param>
/// <param name="ConexionActiva">Indica si el servidor está accesible.</param>
/// <param name="QsosPendientesSincronizar">Cantidad de QSOs locales pendientes de enviar al servidor.</param>
public sealed record EstadoSincronizacion(
    DateTimeOffset? UltimaSincronizacion,
    bool ConexionActiva,
    int QsosPendientesSincronizar);

/// <summary>
/// Servicio de sincronización bidireccional de QSOs entre el cliente de escritorio y la API web.
/// </summary>
public interface IServicioSincronizacion : IDisposable
{
    /// <summary>
    /// Ejecuta una sincronización completa: envía QSOs locales pendientes y recibe QSOs nuevos del servidor.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado detallado de la sincronización.</returns>
    Task<ResultadoSincronizacion> SincronizarAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene el estado actual de la sincronización (última fecha, conexión, pendientes).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Estado actual del servicio de sincronización.</returns>
    Task<EstadoSincronizacion> ObtenerEstadoAsync(CancellationToken ct = default);

    /// <summary>
    /// Configura los parámetros de conexión y comportamiento del servicio de sincronización.
    /// Si la sincronización automática está habilitada, inicia el temporizador.
    /// </summary>
    /// <param name="configuracion">Parámetros de configuración del servidor.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ConfigurarAsync(ConfiguracionSincronizacion configuracion, CancellationToken ct = default);
}
