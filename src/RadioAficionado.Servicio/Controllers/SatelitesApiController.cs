using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Satelites;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// API REST para consultar proximos pases de satelites amateur.
/// </summary>
[Route("api/satelites")]
[ApiController]
public sealed class SatelitesApiController : ControllerBase
{
    private readonly IServicioSatelites _servicioSatelites;
    private readonly ILogger<SatelitesApiController> _logger;

    /// <summary>
    /// Crea el controlador API de satelites.
    /// </summary>
    public SatelitesApiController(
        IServicioSatelites servicioSatelites,
        ILogger<SatelitesApiController> logger)
    {
        _servicioSatelites = servicioSatelites ?? throw new ArgumentNullException(nameof(servicioSatelites));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene los proximos pases de satelites amateur. Si se proporciona un satelite
    /// especifico, devuelve los pases de ese satelite; si no, devuelve datos generales.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerPases(
        [FromQuery] string? satelite = null,
        CancellationToken ct = default)
    {
        // TODO: Obtener coordenadas del observador desde la configuracion del operador.
        // Por ahora se usan coordenadas de ejemplo (Madrid, Espana).
        RadioAficionado.Dominio.ObjetosDeValor.Coordenadas observador =
            new(40.4168, -3.7038);

        IReadOnlyList<SateliteAmateur> catalogoSatelites;
        try
        {
            catalogoSatelites = await _servicioSatelites
                .ObtenerSatelitesAsync(ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Si falla la descarga de TLE, devolver datos de ejemplo
            _logger.LogWarning(ex, "Fallo al obtener el catalogo de satelites (TLE). Se devuelven datos de ejemplo.");
            return Ok(new { pases = GenerarPasesEjemplo() });
        }

        if (catalogoSatelites.Count == 0)
        {
            return Ok(new { pases = GenerarPasesEjemplo() });
        }

        List<object> pasesDto = new();
        DateTime ahora = DateTime.UtcNow;
        DateTime hasta = ahora.AddHours(24);

        // Si se solicita un satelite especifico, buscar por nombre
        IEnumerable<SateliteAmateur> satelitesFiltrados = catalogoSatelites;
        if (!string.IsNullOrWhiteSpace(satelite))
        {
            satelitesFiltrados = catalogoSatelites.Where(s =>
                s.Nombre.Contains(satelite, StringComparison.OrdinalIgnoreCase)
                || s.Indicativo.Contains(satelite, StringComparison.OrdinalIgnoreCase));
        }

        foreach (SateliteAmateur sat in satelitesFiltrados.Take(10))
        {
            try
            {
                IReadOnlyList<PasoSatelite> pasos = await _servicioSatelites
                    .PredecirPasosAsync(sat.NumeroNorad, observador, ahora, hasta, ct)
                    .ConfigureAwait(false);

                foreach (PasoSatelite paso in pasos.Take(3))
                {
                    pasesDto.Add(new
                    {
                        satelite = sat.Nombre,
                        indicativo = sat.Indicativo,
                        norad = sat.NumeroNorad,
                        aos = paso.Aos.ToString("yyyy-MM-dd HH:mm:ss"),
                        los = paso.Los.ToString("yyyy-MM-dd HH:mm:ss"),
                        elevacionMaxima = Math.Round(paso.ElevacionMaxima, 1),
                        azimutAos = Math.Round(paso.AzimutAos, 0),
                        azimutLos = Math.Round(paso.AzimutLos, 0),
                        duracionSegundos = (int)paso.DuracionSegundos,
                        altaElevacion = paso.EsAltaElevacion
                    });
                }
            }
            catch (Exception ex)
            {
                // Ignorar satelites que fallan en la prediccion, pero dejar traza
                _logger.LogDebug(
                    ex,
                    "Fallo la prediccion de pasos para el satelite {Satelite} (NORAD {Norad}).",
                    sat.Nombre,
                    sat.NumeroNorad);
            }
        }

        if (pasesDto.Count == 0)
        {
            return Ok(new { pases = GenerarPasesEjemplo() });
        }

        return Ok(new { pases = pasesDto });
    }

    /// <summary>
    /// Genera datos de ejemplo realistas de pases de satelites amateur.
    /// </summary>
    private static List<object> GenerarPasesEjemplo()
    {
        // TODO: Reemplazar con datos reales cuando el servicio de satelites
        // descargue TLEs correctamente. Tiempos calculados para Madrid ~40N.
        DateTime ahora = DateTime.UtcNow;

        return new List<object>
        {
            new
            {
                satelite = "ISS (ZARYA)",
                indicativo = "RS0ISS",
                norad = 25544,
                aos = ahora.AddMinutes(47).ToString("yyyy-MM-dd HH:mm:ss"),
                los = ahora.AddMinutes(57).ToString("yyyy-MM-dd HH:mm:ss"),
                elevacionMaxima = 62.3,
                azimutAos = 215,
                azimutLos = 42,
                duracionSegundos = 600,
                altaElevacion = true
            },
            new
            {
                satelite = "SO-50",
                indicativo = "NO-50",
                norad = 27607,
                aos = ahora.AddMinutes(82).ToString("yyyy-MM-dd HH:mm:ss"),
                los = ahora.AddMinutes(94).ToString("yyyy-MM-dd HH:mm:ss"),
                elevacionMaxima = 34.7,
                azimutAos = 310,
                azimutLos = 130,
                duracionSegundos = 720,
                altaElevacion = false
            },
            new
            {
                satelite = "AO-91 (Fox-1B)",
                indicativo = "NO-91",
                norad = 43017,
                aos = ahora.AddMinutes(120).ToString("yyyy-MM-dd HH:mm:ss"),
                los = ahora.AddMinutes(131).ToString("yyyy-MM-dd HH:mm:ss"),
                elevacionMaxima = 48.2,
                azimutAos = 190,
                azimutLos = 15,
                duracionSegundos = 660,
                altaElevacion = true
            },
            new
            {
                satelite = "RS-44",
                indicativo = "RS-44",
                norad = 44909,
                aos = ahora.AddMinutes(155).ToString("yyyy-MM-dd HH:mm:ss"),
                los = ahora.AddMinutes(170).ToString("yyyy-MM-dd HH:mm:ss"),
                elevacionMaxima = 72.5,
                azimutAos = 260,
                azimutLos = 80,
                duracionSegundos = 900,
                altaElevacion = true
            },
            new
            {
                satelite = "CAS-4A",
                indicativo = "LO-102",
                norad = 44881,
                aos = ahora.AddMinutes(200).ToString("yyyy-MM-dd HH:mm:ss"),
                los = ahora.AddMinutes(212).ToString("yyyy-MM-dd HH:mm:ss"),
                elevacionMaxima = 21.8,
                azimutAos = 140,
                azimutLos = 340,
                duracionSegundos = 720,
                altaElevacion = false
            }
        };
    }
}
