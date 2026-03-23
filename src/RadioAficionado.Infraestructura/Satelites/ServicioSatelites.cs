using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Satelites;
using Microsoft.Extensions.Logging;

namespace RadioAficionado.Infraestructura.Satelites;

/// <summary>
/// Implementación del servicio de tracking de satélites amateur.
/// Combina el catálogo estático de satélites con el calculador orbital SGP4 simplificado.
/// Descarga y cachea TLEs desde Celestrak para cálculos de posición y predicción de pasos.
/// </summary>
public sealed class ServicioSatelites : IServicioSatelites
{
    private readonly ILogger<ServicioSatelites> _logger;
    private readonly HttpClient _clienteHttp;
    private readonly ConfiguracionSatelites _configuracion;

    /// <summary>Cache de TLEs descargados, indexados por número NORAD.</summary>
    private Dictionary<int, Tle> _cacheTle = new();

    /// <summary>Momento de la última actualización de TLEs.</summary>
    private DateTime _ultimaActualizacionTle = DateTime.MinValue;

    /// <summary>Semáforo para proteger la descarga concurrente de TLEs.</summary>
    private readonly SemaphoreSlim _semaforoTle = new(1, 1);

    /// <summary>
    /// Crea una nueva instancia del servicio de satélites.
    /// </summary>
    /// <param name="logger">Logger de Microsoft.Extensions.Logging.</param>
    /// <param name="clienteHttp">Cliente HTTP para descargar TLEs.</param>
    /// <param name="configuracion">Configuración del servicio.</param>
    public ServicioSatelites(ILogger<ServicioSatelites> logger, HttpClient clienteHttp, ConfiguracionSatelites configuracion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clienteHttp = clienteHttp ?? throw new ArgumentNullException(nameof(clienteHttp));
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SateliteAmateur>> ObtenerSatelitesAsync(CancellationToken ct = default)
    {
        IReadOnlyList<SateliteAmateur> satelites = CatalogoSatelites.ObtenerTodos();
        return Task.FromResult(satelites);
    }

    /// <inheritdoc />
    public async Task<PosicionSatelite> CalcularPosicionAsync(
        int noradId,
        Coordenadas observador,
        DateTime momento,
        CancellationToken ct = default)
    {
        Tle tle = await ObtenerTleAsync(noradId, ct).ConfigureAwait(false);
        PosicionSatelite posicion = CalculadorOrbital.CalcularPosicion(tle, observador, momento);
        return posicion;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PasoSatelite>> PredecirPasosAsync(
        int noradId,
        Coordenadas observador,
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default)
    {
        Tle tle = await ObtenerTleAsync(noradId, ct).ConfigureAwait(false);
        SateliteAmateur? satelite = CatalogoSatelites.BuscarPorNorad(noradId);

        if (satelite is null)
        {
            satelite = new SateliteAmateur(noradId, $"NORAD-{noradId}", $"N-{noradId}", Array.Empty<TransponderSatelite>(), true);
        }

        IReadOnlyList<PasoSatelite> pasos = CalculadorOrbital.PredecirPasos(
            tle, satelite, observador, desde, hasta, _configuracion.ElevacionMinimaPaso);

        _logger.LogInformation(
            "Predichos {CantidadPasos} pasos para {Satelite} entre {Desde} y {Hasta}",
            pasos.Count, satelite.Nombre, desde, hasta);

        return pasos;
    }

    /// <inheritdoc />
    public async Task<PasoSatelite?> ObtenerProximoPasoAsync(
        int noradId,
        Coordenadas observador,
        CancellationToken ct = default)
    {
        DateTime ahora = DateTime.UtcNow;
        DateTime hasta = ahora.AddHours(24);

        IReadOnlyList<PasoSatelite> pasos = await PredecirPasosAsync(
            noradId, observador, ahora, hasta, ct).ConfigureAwait(false);

        if (pasos.Count == 0)
        {
            return null;
        }

        return pasos[0];
    }

    /// <summary>
    /// Obtiene el TLE de un satélite, descargando desde la fuente si la cache ha expirado.
    /// </summary>
    /// <param name="noradId">Número NORAD del satélite.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>TLE del satélite.</returns>
    /// <exception cref="InvalidOperationException">Si no se encuentra el TLE del satélite.</exception>
    private async Task<Tle> ObtenerTleAsync(int noradId, CancellationToken ct)
    {
        await ActualizarCacheTleAsync(ct).ConfigureAwait(false);

        if (_cacheTle.TryGetValue(noradId, out Tle? tle))
        {
            return tle;
        }

        throw new InvalidOperationException(
            $"No se encontró TLE para el satélite NORAD {noradId}. " +
            "Verifique que el satélite existe y que la descarga de TLEs fue exitosa.");
    }

    /// <summary>
    /// Actualiza la cache de TLEs si ha pasado el intervalo configurado.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    private async Task ActualizarCacheTleAsync(CancellationToken ct)
    {
        TimeSpan tiempoDesdeUltimaActualizacion = DateTime.UtcNow - _ultimaActualizacionTle;
        TimeSpan intervalo = TimeSpan.FromMinutes(_configuracion.IntervaloActualizacionTleMinutos);

        if (tiempoDesdeUltimaActualizacion < intervalo && _cacheTle.Count > 0)
        {
            return;
        }

        await _semaforoTle.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Doble verificación después de obtener el semáforo
            tiempoDesdeUltimaActualizacion = DateTime.UtcNow - _ultimaActualizacionTle;
            if (tiempoDesdeUltimaActualizacion < intervalo && _cacheTle.Count > 0)
            {
                return;
            }

            _logger.LogInformation("Descargando TLEs desde {Url}", _configuracion.UrlTle);

            string textoTle = await _clienteHttp.GetStringAsync(_configuracion.UrlTle, ct).ConfigureAwait(false);
            IReadOnlyList<Tle> tles = CalculadorOrbital.ParsearMultiplesTle(textoTle);

            Dictionary<int, Tle> nuevaCache = new();
            foreach (Tle tle in tles)
            {
                nuevaCache[tle.NumeroNorad] = tle;
            }

            _cacheTle = nuevaCache;
            _ultimaActualizacionTle = DateTime.UtcNow;

            _logger.LogInformation("TLEs actualizados: {Cantidad} satélites cargados", tles.Count);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Error descargando TLEs. Se usará la cache anterior si existe.");

            if (_cacheTle.Count == 0)
            {
                throw new InvalidOperationException(
                    "No se pudieron descargar los TLEs y no hay cache disponible.", ex);
            }
        }
        finally
        {
            _semaforoTle.Release();
        }
    }
}
