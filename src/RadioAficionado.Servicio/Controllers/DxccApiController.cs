using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// API REST para consultar el estado DXCC del operador, cruzando el catalogo
/// de entidades DXCC con los QSOs registrados en el logbook.
/// </summary>
[Route("api/dxcc")]
[ApiController]
public sealed class DxccApiController : ControllerBase
{
    private readonly IRepositorioQso _repositorioQso;

    /// <summary>
    /// Crea el controlador API de DXCC.
    /// </summary>
    public DxccApiController(IRepositorioQso repositorioQso)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
    }

    /// <summary>
    /// Obtiene la lista de entidades DXCC activas con su estado trabajado/confirmado por banda.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerEntidadesDxcc(
        [FromQuery] string? banda = null,
        [FromQuery] string? estado = null,
        CancellationToken ct = default)
    {
        IReadOnlyList<EntidadDxcc> entidadesActivas = CatalogoDxcc.ObtenerActivas();
        IReadOnlyList<RadioAficionado.Dominio.Entidades.Qso> todosLosQsos = await _repositorioQso
            .ObtenerTodosAsync(ct)
            .ConfigureAwait(false);

        // Agrupar QSOs por prefijo DXCC y banda
        Dictionary<string, HashSet<string>> trabajadosPorEntidad = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, HashSet<string>> confirmadosPorEntidad = new(StringComparer.OrdinalIgnoreCase);

        string[] bandasHf = ["160m", "80m", "40m", "20m", "15m", "10m"];

        foreach (RadioAficionado.Dominio.Entidades.Qso qso in todosLosQsos)
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
            if (entidad is null || entidad.Eliminada)
            {
                continue;
            }

            string bandaQso = qso.Frecuencia.ObtenerBanda()?.ToString() ?? "";
            string bandaCorta = ConvertirBandaACorta(bandaQso);
            if (string.IsNullOrWhiteSpace(bandaCorta))
            {
                continue;
            }

            string clave = entidad.Prefijo;

            if (!trabajadosPorEntidad.ContainsKey(clave))
            {
                trabajadosPorEntidad[clave] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            trabajadosPorEntidad[clave].Add(bandaCorta);

            if (qso.Sincronizado)
            {
                if (!confirmadosPorEntidad.ContainsKey(clave))
                {
                    confirmadosPorEntidad[clave] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                confirmadosPorEntidad[clave].Add(bandaCorta);
            }
        }

        List<object> entidadesDto = new();
        int totalTrabajados = 0;
        int totalConfirmados = 0;

        foreach (EntidadDxcc entidad in entidadesActivas)
        {
            Dictionary<string, string> estadoPorBanda = new();
            bool entidadTrabajada = false;
            bool entidadConfirmada = false;

            foreach (string b in bandasHf)
            {
                bool trabajado = trabajadosPorEntidad.TryGetValue(entidad.Prefijo, out HashSet<string>? tSet)
                    && tSet.Contains(b);
                bool confirmado = confirmadosPorEntidad.TryGetValue(entidad.Prefijo, out HashSet<string>? cSet)
                    && cSet.Contains(b);

                if (confirmado)
                {
                    estadoPorBanda[b] = "confirmado";
                    entidadConfirmada = true;
                    entidadTrabajada = true;
                }
                else if (trabajado)
                {
                    estadoPorBanda[b] = "trabajado";
                    entidadTrabajada = true;
                }
                else
                {
                    estadoPorBanda[b] = "necesitado";
                }
            }

            // Aplicar filtro de banda
            if (!string.IsNullOrWhiteSpace(banda) && estadoPorBanda.ContainsKey(banda))
            {
                string estadoBanda = estadoPorBanda[banda];
                if (!string.IsNullOrWhiteSpace(estado) && estadoBanda != estado)
                {
                    continue;
                }
            }
            else if (!string.IsNullOrWhiteSpace(estado))
            {
                string estadoEntidad = entidadConfirmada ? "confirmado"
                    : entidadTrabajada ? "trabajado"
                    : "necesitado";
                if (estadoEntidad != estado)
                {
                    continue;
                }
            }

            if (entidadTrabajada) { totalTrabajados++; }
            if (entidadConfirmada) { totalConfirmados++; }

            entidadesDto.Add(new
            {
                numero = entidad.Numero,
                nombre = entidad.Nombre,
                prefijo = entidad.Prefijo,
                continente = entidad.Continente,
                bandas = estadoPorBanda
            });
        }

        object respuesta = new
        {
            trabajados = totalTrabajados,
            confirmados = totalConfirmados,
            total = entidadesActivas.Count,
            entidades = entidadesDto
        };

        return Ok(respuesta);
    }

    /// <summary>
    /// Convierte el nombre del enum BandaRadio a formato corto (ej. "Banda20m" a "20m").
    /// </summary>
    private static string ConvertirBandaACorta(string bandaEnum)
    {
        if (string.IsNullOrWhiteSpace(bandaEnum))
        {
            return "";
        }

        // El enum tiene formato "Banda160m", "Banda80m", etc.
        if (bandaEnum.StartsWith("Banda", StringComparison.OrdinalIgnoreCase))
        {
            return bandaEnum[5..].ToLowerInvariant();
        }

        return bandaEnum.ToLowerInvariant();
    }
}
