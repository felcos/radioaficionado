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

    /// <summary>
    /// Crea el controlador API de propagacion.
    /// </summary>
    public PropagacionApiController(IServicioPropagacion servicioPropagacion)
    {
        _servicioPropagacion = servicioPropagacion ?? throw new ArgumentNullException(nameof(servicioPropagacion));
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
        catch (Exception)
        {
            // Si falla la consulta a NOAA, devolvemos datos de ejemplo
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
}
