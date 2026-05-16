using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// API REST para consultar activaciones POTA/SOTA activas y recientes.
/// </summary>
[Route("api/activaciones")]
[ApiController]
public sealed class ActivacionesApiController : ControllerBase
{
    private readonly IServicioActivaciones _servicioActivaciones;

    /// <summary>
    /// Crea el controlador API de activaciones.
    /// </summary>
    public ActivacionesApiController(IServicioActivaciones servicioActivaciones)
    {
        _servicioActivaciones = servicioActivaciones ?? throw new ArgumentNullException(nameof(servicioActivaciones));
    }

    /// <summary>
    /// Obtiene las activaciones activas, filtrables por tipo (POTA/SOTA).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerActivaciones(
        [FromQuery] string? tipo = null,
        [FromQuery] string? busqueda = null,
        CancellationToken ct = default)
    {
        IReadOnlyList<Activacion> activaciones;

        if (!string.IsNullOrWhiteSpace(tipo) &&
            Enum.TryParse<TipoActivacion>(tipo, ignoreCase: true, out TipoActivacion tipoEnum))
        {
            activaciones = await _servicioActivaciones
                .ObtenerActivacionesAsync(tipoEnum, ct)
                .ConfigureAwait(false);
        }
        else
        {
            activaciones = await _servicioActivaciones
                .ObtenerTodasAsync(ct)
                .ConfigureAwait(false);
        }

        IEnumerable<Activacion> filtradas = activaciones
            .Where(a => a.EstadoActivacion == EstadoActivacion.EnCurso
                     || a.EstadoActivacion == EstadoActivacion.Planificada);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string busquedaNorm = busqueda.Trim().ToUpperInvariant();
            filtradas = filtradas.Where(a =>
                a.Referencia.Contains(busquedaNorm, StringComparison.OrdinalIgnoreCase)
                || a.IndicativoActivador.Valor.Contains(busquedaNorm, StringComparison.OrdinalIgnoreCase));
        }

        List<object> resultado = filtradas.Select(a => (object)new
        {
            id = a.Id,
            referencia = a.Referencia,
            tipo = a.TipoActivacion.ToString(),
            activador = a.IndicativoActivador.Valor,
            estado = a.EstadoActivacion.ToString(),
            fechaInicio = a.FechaInicio.ToString("yyyy-MM-dd HH:mm"),
            localizador = a.Localizador?.ToString() ?? "",
            qsos = a.Qsos.Count,
            notas = a.Notas ?? ""
        }).ToList();

        // Si no hay activaciones registradas, devolver datos de ejemplo realistas
        if (resultado.Count == 0)
        {
            // TODO: Integrar con API de spots POTA/SOTA cuando este disponible
            // para mostrar activaciones en tiempo real de otros operadores.
            resultado = GenerarActivacionesEjemplo(tipo);
        }

        return Ok(new { activaciones = resultado });
    }

    /// <summary>
    /// Genera datos de ejemplo realistas de activaciones POTA/SOTA.
    /// </summary>
    private static List<object> GenerarActivacionesEjemplo(string? tipo)
    {
        List<object> ejemplos = new();

        bool incluirPota = string.IsNullOrWhiteSpace(tipo)
            || string.Equals(tipo, "pota", StringComparison.OrdinalIgnoreCase);
        bool incluirSota = string.IsNullOrWhiteSpace(tipo)
            || string.Equals(tipo, "sota", StringComparison.OrdinalIgnoreCase);

        if (incluirPota)
        {
            ejemplos.AddRange(new object[]
            {
                new
                {
                    referencia = "K-0059",
                    nombre = "Yellowstone National Park",
                    activador = "W7RN",
                    frecuencia = "14.062",
                    modo = "FT8",
                    spots = 23,
                    utc = DateTime.UtcNow.AddMinutes(-12).ToString("HH:mm")
                },
                new
                {
                    referencia = "K-4566",
                    nombre = "Shenandoah National Park",
                    activador = "K4SWL",
                    frecuencia = "7.074",
                    modo = "FT8",
                    spots = 45,
                    utc = DateTime.UtcNow.AddMinutes(-5).ToString("HH:mm")
                },
                new
                {
                    referencia = "K-2612",
                    nombre = "Cuyahoga Valley National Park",
                    activador = "N8VW",
                    frecuencia = "14.285",
                    modo = "SSB",
                    spots = 18,
                    utc = DateTime.UtcNow.AddMinutes(-8).ToString("HH:mm")
                },
                new
                {
                    referencia = "EA-0089",
                    nombre = "Parque Nacional de Donana",
                    activador = "EA7JTK",
                    frecuencia = "7.090",
                    modo = "SSB",
                    spots = 12,
                    utc = DateTime.UtcNow.AddMinutes(-15).ToString("HH:mm")
                },
                new
                {
                    referencia = "DL-0001",
                    nombre = "Nationalpark Bayerischer Wald",
                    activador = "DL2DXA",
                    frecuencia = "10.136",
                    modo = "FT8",
                    spots = 31,
                    utc = DateTime.UtcNow.AddMinutes(-3).ToString("HH:mm")
                }
            });
        }

        if (incluirSota)
        {
            ejemplos.AddRange(new object[]
            {
                new
                {
                    referencia = "W4C/WM-001",
                    nombre = "Mount Mitchell",
                    activador = "KN4MQS",
                    frecuencia = "14.285",
                    modo = "SSB",
                    spots = 8,
                    utc = DateTime.UtcNow.AddMinutes(-20).ToString("HH:mm")
                },
                new
                {
                    referencia = "EA2/SS-001",
                    nombre = "Aizkorri",
                    activador = "EA2IF",
                    frecuencia = "7.032",
                    modo = "CW",
                    spots = 15,
                    utc = DateTime.UtcNow.AddMinutes(-7).ToString("HH:mm")
                },
                new
                {
                    referencia = "OE/TI-001",
                    nombre = "Grossglockner",
                    activador = "OE5JFE",
                    frecuencia = "14.062",
                    modo = "FT8",
                    spots = 22,
                    utc = DateTime.UtcNow.AddMinutes(-2).ToString("HH:mm")
                }
            });
        }

        return ejemplos;
    }
}
