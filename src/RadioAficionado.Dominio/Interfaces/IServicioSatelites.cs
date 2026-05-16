using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Satelites;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Configuración para el servicio de tracking de satélites amateur.
/// </summary>
public sealed class ConfiguracionSatelites
{
    /// <summary>URL para descargar elementos TLE (Two-Line Element) de satélites amateur.</summary>
    public string UrlTle { get; set; } = "https://celestrak.org/NORAD/elements/gp.php?GROUP=amateur&FORMAT=tle";

    /// <summary>Intervalo mínimo en minutos entre actualizaciones de TLE.</summary>
    public int IntervaloActualizacionTleMinutos { get; set; } = 360;

    /// <summary>Elevación mínima en grados para considerar un paso válido.</summary>
    public double ElevacionMinimaPaso { get; set; } = 5.0;
}

/// <summary>
/// Servicio de tracking de satélites amateur.
/// Permite obtener el catálogo de satélites, calcular posiciones instantáneas
/// y predecir pasos futuros sobre una ubicación.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Proporciona tracking en tiempo real de satélites amateur: catálogo con transponders, cálculo de posiciones instantáneas y predicción de pasos futuros sobre una ubicación.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se llama a <see cref="ObtenerSatelitesAsync"/> para el catálogo, <see cref="CalcularPosicionAsync"/> para posición instantánea y <see cref="PredecirPasosAsync"/> para futuros pasos.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Satelites.ServicioSatelites</c>.</para>
/// <para><b>Registro DI:</b> Registrada con <c>AddHttpClient</c> (Transient con HttpClient gestionado) en <c>RadioAficionado.Infraestructura.ConfiguracionServicios.AgregarCapaDeInfraestructura()</c>.</para>
/// <para><b>Configuración necesaria:</b> URL de datos TLE (CelesTrak por defecto), intervalo de actualización y elevación mínima (ver <see cref="ConfiguracionSatelites"/>).</para>
/// <para><b>Dependencias:</b> <c>HttpClient</c> (inyectado por factory) para descargar TLE de CelesTrak.</para>
/// </remarks>
public interface IServicioSatelites
{
    /// <summary>
    /// Obtiene la lista completa de satélites amateur conocidos.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de satélites amateur con sus transponders.</returns>
    Task<IReadOnlyList<SateliteAmateur>> ObtenerSatelitesAsync(CancellationToken ct = default);

    /// <summary>
    /// Calcula la posición instantánea de un satélite visto desde un observador.
    /// </summary>
    /// <param name="noradId">Número NORAD del satélite.</param>
    /// <param name="observador">Coordenadas del observador en tierra.</param>
    /// <param name="momento">Fecha y hora UTC del cálculo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Posición del satélite relativa al observador.</returns>
    Task<PosicionSatelite> CalcularPosicionAsync(
        int noradId,
        Coordenadas observador,
        DateTime momento,
        CancellationToken ct = default);

    /// <summary>
    /// Predice los pasos de un satélite sobre una ubicación en un rango de tiempo.
    /// </summary>
    /// <param name="noradId">Número NORAD del satélite.</param>
    /// <param name="observador">Coordenadas del observador en tierra.</param>
    /// <param name="desde">Inicio del rango de predicción (UTC).</param>
    /// <param name="hasta">Fin del rango de predicción (UTC).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de pasos predichos ordenados cronológicamente.</returns>
    Task<IReadOnlyList<PasoSatelite>> PredecirPasosAsync(
        int noradId,
        Coordenadas observador,
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el próximo paso de un satélite sobre una ubicación a partir del momento actual.
    /// </summary>
    /// <param name="noradId">Número NORAD del satélite.</param>
    /// <param name="observador">Coordenadas del observador en tierra.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El próximo paso, o null si no se encuentra ninguno en las próximas 24 horas.</returns>
    Task<PasoSatelite?> ObtenerProximoPasoAsync(
        int noradId,
        Coordenadas observador,
        CancellationToken ct = default);
}
