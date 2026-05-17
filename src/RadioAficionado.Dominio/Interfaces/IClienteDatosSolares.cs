using RadioAficionado.Dominio.Propagacion;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Cliente para obtener datos solares y de propagación de fuentes externas (NOAA SWPC y N0NBH).
/// </summary>
public interface IClienteDatosSolares
{
    /// <summary>
    /// Obtiene los datos solares completos combinando fuentes NOAA y N0NBH.
    /// Incluye caché de 5 minutos para evitar peticiones excesivas.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Datos solares completos con índices, condiciones de banda y alertas.</returns>
    Task<DatosSolaresCompletos> ObtenerDatosSolaresCompletosAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene el histórico de 30 días del índice de flujo solar de 10 cm.
    /// Incluye caché de 30 minutos.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista ordenada por fecha de puntos históricos de SFI.</returns>
    Task<IReadOnlyList<PuntoHistoricoSfi>> ObtenerHistoricoSfiAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene el histórico de 7 días del índice planetario Kp (cada 3 horas).
    /// Incluye caché de 30 minutos.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista ordenada por fecha de puntos históricos de Kp.</returns>
    Task<IReadOnlyList<PuntoHistoricoKp>> ObtenerHistoricoKpAsync(CancellationToken ct = default);
}
