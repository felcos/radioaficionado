using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Propagacion;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// API REST para consultar indices solares y condiciones de propagacion HF.
/// </summary>
[Route("api/propagacion")]
[ApiController]
public sealed class PropagacionApiController : ControllerBase
{
    private readonly IServicioPropagacion _servicioPropagacion;
    private readonly IClienteDatosSolares _clienteDatosSolares;
    private readonly ILogger<PropagacionApiController> _logger;

    /// <summary>
    /// Crea el controlador API de propagacion.
    /// </summary>
    public PropagacionApiController(
        IServicioPropagacion servicioPropagacion,
        IClienteDatosSolares clienteDatosSolares,
        ILogger<PropagacionApiController> logger)
    {
        _servicioPropagacion = servicioPropagacion ?? throw new ArgumentNullException(nameof(servicioPropagacion));
        _clienteDatosSolares = clienteDatosSolares ?? throw new ArgumentNullException(nameof(clienteDatosSolares));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene los indices solares actuales y las condiciones de propagacion por banda.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerPropagacion(CancellationToken ct = default)
    {
        IndicesSolares? indices = null;
        try
        {
            indices = await _servicioPropagacion
                .ObtenerIndicesSolaresAsync(ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Si falla la consulta a NOAA, devolvemos datos de ejemplo
            _logger.LogWarning(ex, "Fallo al obtener indices solares de NOAA. Se devuelven datos de ejemplo.");
        }

        if (indices is not null)
        {
            object respuesta = new
            {
                sfi = indices.Sfi,
                sn = (int)indices.NumeroManchasSolares,
                a = indices.Ap,
                k = indices.Kp,
                actualizacion = indices.FechaActualizacion.ToString("yyyy-MM-dd HH:mm UTC"),
                bandas = GenerarCondicionesBanda(indices)
            };

            return Ok(respuesta);
        }

        // TODO: Reemplazar datos de ejemplo cuando el servicio de propagacion
        // tenga conectividad estable a NOAA. Valores basados en ciclo solar 25 (mayo 2026).
        object datosEjemplo = new
        {
            sfi = 165,
            sn = 128,
            a = 8,
            k = 2,
            actualizacion = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"),
            bandas = new[]
            {
                new { banda = "80m-40m", dia = "Buena", noche = "Buena" },
                new { banda = "30m-20m", dia = "Buena", noche = "Regular" },
                new { banda = "17m-15m", dia = "Buena", noche = "Regular" },
                new { banda = "12m-10m", dia = "Regular", noche = "Pobre" }
            }
        };

        return Ok(datosEjemplo);
    }

    /// <summary>
    /// Genera condiciones de propagacion por grupo de bandas basadas en los indices solares reales.
    /// </summary>
    private static object[] GenerarCondicionesBanda(IndicesSolares indices)
    {
        // Logica simplificada: SFI alto mejora bandas altas, Kp alto degrada todo
        string condicionBase = indices.Kp >= 5 ? "Pobre"
            : indices.Kp >= 3 ? "Regular"
            : "Buena";

        string condicionBandasAltas = indices.Sfi >= 150 ? condicionBase
            : indices.Sfi >= 100 ? ReducirNivel(condicionBase)
            : ReducirNivel(ReducirNivel(condicionBase));

        string condicionBandasMedias = indices.Sfi >= 100 ? condicionBase
            : ReducirNivel(condicionBase);

        return
        [
            new { banda = "80m-40m", dia = condicionBase, noche = condicionBase },
            new { banda = "30m-20m", dia = condicionBase, noche = condicionBandasMedias },
            new { banda = "17m-15m", dia = condicionBandasMedias, noche = condicionBandasAltas },
            new { banda = "12m-10m", dia = condicionBandasAltas, noche = ReducirNivel(condicionBandasAltas) }
        ];
    }

    /// <summary>
    /// Reduce un nivel de propagacion en un paso.
    /// </summary>
    private static string ReducirNivel(string nivel)
    {
        return nivel switch
        {
            "Buena" => "Regular",
            "Regular" => "Pobre",
            _ => "Pobre"
        };
    }

    /// <summary>
    /// Obtiene los datos solares completos en tiempo real combinando NOAA y N0NBH.
    /// Incluye índices solares, condiciones de banda HF/VHF, escalas NOAA y alertas activas.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Datos solares completos.</returns>
    [HttpGet("solar")]
    [ProducesResponseType(typeof(DatosSolaresCompletos), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DatosSolaresCompletos>> ObtenerDatosSolares(CancellationToken ct)
    {
        DatosSolaresCompletos datos = await _clienteDatosSolares.ObtenerDatosSolaresCompletosAsync(ct)
            .ConfigureAwait(false);
        return Ok(datos);
    }

    /// <summary>
    /// Obtiene datos históricos de SFI (30 días) y Kp (7 días).
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Objeto con arrays de puntos históricos de SFI y Kp.</returns>
    [HttpGet("historico")]
    [ProducesResponseType(typeof(HistoricoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HistoricoResponse>> ObtenerHistorico(CancellationToken ct)
    {
        Task<IReadOnlyList<PuntoHistoricoSfi>> tareaSfi = _clienteDatosSolares.ObtenerHistoricoSfiAsync(ct);
        Task<IReadOnlyList<PuntoHistoricoKp>> tareaKp = _clienteDatosSolares.ObtenerHistoricoKpAsync(ct);

        await Task.WhenAll(tareaSfi, tareaKp).ConfigureAwait(false);

        IReadOnlyList<PuntoHistoricoSfi> sfi = await tareaSfi.ConfigureAwait(false);
        IReadOnlyList<PuntoHistoricoKp> kp = await tareaKp.ConfigureAwait(false);

        HistoricoResponse respuesta = new(sfi, kp);
        return Ok(respuesta);
    }

    /// <summary>
    /// Obtiene las alertas solares activas de NOAA SWPC.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de alertas solares activas.</returns>
    [HttpGet("alertas")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertaSolar>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<AlertaSolar>>> ObtenerAlertas(CancellationToken ct)
    {
        DatosSolaresCompletos datos = await _clienteDatosSolares.ObtenerDatosSolaresCompletosAsync(ct)
            .ConfigureAwait(false);
        return Ok(datos.AlertasActivas);
    }
}

/// <summary>
/// Respuesta del endpoint de datos históricos con SFI y Kp.
/// </summary>
public sealed record HistoricoResponse(
    IReadOnlyList<PuntoHistoricoSfi> Sfi,
    IReadOnlyList<PuntoHistoricoKp> Kp);
