using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// API REST para busqueda global de indicativos a traves de logbook y catalogo DXCC.
/// </summary>
[Route("api/busqueda")]
[ApiController]
public sealed class BusquedaGlobalApiController : ControllerBase
{
    private readonly IRepositorioQso _repositorioQso;

    /// <summary>
    /// Crea el controlador API de busqueda global.
    /// </summary>
    public BusquedaGlobalApiController(IRepositorioQso repositorioQso)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
    }

    /// <summary>
    /// Busca un indicativo en logbook y catalogo DXCC.
    /// </summary>
    /// <param name="q">Indicativo o fragmento a buscar.</param>
    /// <param name="ct">Token de cancelacion.</param>
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Ok(new { logbook = Array.Empty<object>(), dxcc = (object?)null });
        }

        string termino = q.Trim().ToUpperInvariant();

        // Buscar en DXCC
        EntidadDxcc? entidadDxcc = CatalogoDxcc.ObtenerPorPrefijo(termino);
        object? resultadoDxcc = null;
        if (entidadDxcc is not null)
        {
            resultadoDxcc = new
            {
                entidad = entidadDxcc.Nombre,
                prefijo = entidadDxcc.Prefijo,
                continente = entidadDxcc.Continente,
                zonaCq = entidadDxcc.ZonaCq,
                zonaItu = entidadDxcc.ZonaItu
            };
        }

        // Buscar en logbook (ultimos 20 QSOs con ese indicativo)
        FiltroQso filtro = new(Indicativo: termino);
        Dominio.Interfaces.ResultadoPaginado<Dominio.Entidades.Qso> resultado = await _repositorioQso
            .ObtenerPaginadoAsync(1, 20, filtro, ct);
        IReadOnlyList<Dominio.Entidades.Qso> qsos = resultado.Elementos;

        List<object> resultadosLogbook = qsos.Select(qso => (object)new
        {
            indicativo = qso.IndicativoContacto.Valor,
            fecha = qso.FechaHoraInicio.UtcDateTime.ToString("yyyy-MM-dd HH:mm"),
            frecuenciaHz = qso.Frecuencia.Hz,
            banda = qso.Frecuencia.ObtenerBanda()?.ObtenerNombre() ?? "",
            modo = qso.Modo.ToString()
        }).ToList();

        return Ok(new { logbook = resultadosLogbook, dxcc = resultadoDxcc });
    }
}
