using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Propagacion;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Configuracion para el servicio de propagacion HF.
/// </summary>
public sealed class ConfiguracionPropagacion
{
    /// <summary>URL de la fuente de datos de indices solares.</summary>
    public string UrlDatosSolares { get; set; } = "https://services.swpc.noaa.gov/json/solar-cycle/predicted-solar-cycle.json";

    /// <summary>Intervalo minimo en minutos entre consultas a la fuente de datos solares.</summary>
    public int IntervaloActualizacionMinutos { get; set; } = 30;
}

/// <summary>
/// Servicio de prediccion de propagacion HF basado en indices solares, hora del dia y geometria de la trayectoria.
/// Permite estimar que bandas de HF estan abiertas en un momento dado entre dos ubicaciones.
/// </summary>
public interface IServicioPropagacion
{
    /// <summary>
    /// Obtiene los indices solares actuales desde la fuente de datos configurada.
    /// Utiliza cache interna para no exceder el intervalo de actualizacion.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Los indices solares mas recientes disponibles.</returns>
    Task<IndicesSolares> ObtenerIndicesSolaresAsync(CancellationToken ct = default);

    /// <summary>
    /// Predice la propagacion para todas las bandas HF en un momento y ubicacion dados.
    /// </summary>
    /// <param name="origen">Coordenadas de la estacion transmisora.</param>
    /// <param name="destino">Coordenadas de la estacion receptora (null para prediccion general).</param>
    /// <param name="hora">Fecha y hora UTC para la prediccion.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Lista de predicciones, una por cada banda HF.</returns>
    Task<IReadOnlyList<PrediccionBanda>> PredecirPropagacionAsync(
        Coordenadas origen,
        Coordenadas? destino,
        DateTime hora,
        CancellationToken ct = default);

    /// <summary>
    /// Determina la mejor banda HF para comunicar entre dos puntos en un momento dado.
    /// </summary>
    /// <param name="origen">Coordenadas de la estacion transmisora.</param>
    /// <param name="destino">Coordenadas de la estacion receptora.</param>
    /// <param name="hora">Fecha y hora UTC para la consulta.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>La banda con mejor propagacion, o null si ninguna banda esta abierta.</returns>
    Task<BandaRadio?> ObtenerMejorBandaAsync(
        Coordenadas origen,
        Coordenadas destino,
        DateTime hora,
        CancellationToken ct = default);
}
