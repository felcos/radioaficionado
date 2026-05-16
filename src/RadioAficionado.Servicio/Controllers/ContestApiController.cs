using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// API REST para consultar estadisticas del contest activo.
/// Usa datos reales de QSOs cuando hay un contest seleccionado y existen contactos
/// en las ultimas 48 horas; de lo contrario devuelve datos de ejemplo como fallback.
/// </summary>
[Route("api/contest")]
[ApiController]
public sealed class ContestApiController : ControllerBase
{
    private readonly IRepositorioQso _repositorioQso;

    /// <summary>Bandas tipicas de contest HF.</summary>
    private static readonly string[] BandasContest = ["160m", "80m", "40m", "20m", "15m", "10m"];

    /// <summary>
    /// Mapeo de <see cref="BandaRadio"/> a etiqueta corta de contest.
    /// </summary>
    private static readonly Dictionary<BandaRadio, string> MapaBandaAEtiqueta = new()
    {
        [BandaRadio.Banda160m] = "160m",
        [BandaRadio.Banda80m] = "80m",
        [BandaRadio.Banda40m] = "40m",
        [BandaRadio.Banda20m] = "20m",
        [BandaRadio.Banda15m] = "15m",
        [BandaRadio.Banda10m] = "10m"
    };

    /// <summary>
    /// Crea el controlador API de contest.
    /// </summary>
    /// <param name="repositorioQso">Repositorio de QSOs para obtener datos reales.</param>
    public ContestApiController(IRepositorioQso repositorioQso)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
    }

    /// <summary>
    /// Obtiene las estadisticas del contest activo: QSOs, puntos, multiplicadores y rate.
    /// Si hay QSOs en las ultimas 48 horas devuelve datos reales; si no, datos de ejemplo.
    /// </summary>
    /// <param name="contest">Identificador del contest (por ejemplo, "cqww-ssb"). Null para ninguno.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Estadisticas del contest en formato JSON.</returns>
    [HttpGet]
    public async Task<IActionResult> ObtenerEstadisticasAsync(
        [FromQuery] string? contest = null,
        CancellationToken ct = default)
    {
        string nombreContest = ObtenerNombreContest(contest);

        if (string.IsNullOrWhiteSpace(contest))
        {
            object respuestaVacia = new
            {
                activo = false,
                contest = "Ninguno activo",
                qsos = 0,
                multiplicadores = 0,
                puntos = 0,
                score = 0,
                rate = 0,
                bandas = Array.Empty<object>()
            };

            return Ok(respuestaVacia);
        }

        // Obtener QSOs de las ultimas 48 horas (periodo tipico de contest)
        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct).ConfigureAwait(false);
        DateTimeOffset hace48Horas = DateTimeOffset.UtcNow.AddHours(-48);
        DateTimeOffset haceUnaHora = DateTimeOffset.UtcNow.AddHours(-1);

        List<Qso> qsosContest = todosLosQsos
            .Where(q => q.FechaHoraInicio >= hace48Horas)
            .ToList();

        if (qsosContest.Count == 0)
        {
            return Ok(CrearRespuestaEjemplo(nombreContest));
        }

        // Calcular estadisticas reales
        int totalQsos = qsosContest.Count;
        int rate = qsosContest.Count(q => q.FechaHoraInicio >= haceUnaHora);

        // Agrupar por banda y calcular contadores
        List<object> estadisticasBandas = new();
        int totalMultiplicadores = 0;
        int totalPuntos = 0;

        // Determinar continente propio a partir del primer QSO
        Qso primerQso = qsosContest[0];
        EntidadDxcc? entidadPropia = CatalogoDxcc.ObtenerPorIndicativo(primerQso.IndicativoPropio);
        string continentePropio = entidadPropia?.Continente ?? "EU";

        foreach (string etiquetaBanda in BandasContest)
        {
            List<Qso> qsosDeBanda = qsosContest
                .Where(q => ObtenerEtiquetaBanda(q.Frecuencia) == etiquetaBanda)
                .ToList();

            // Multiplicadores = entidades DXCC unicas en esta banda
            HashSet<int> entidadesDxccUnicas = new();
            foreach (Qso qso in qsosDeBanda)
            {
                EntidadDxcc? entidadContacto = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
                if (entidadContacto is not null)
                {
                    entidadesDxccUnicas.Add(entidadContacto.Numero);
                }
            }

            int multiplicadoresBanda = entidadesDxccUnicas.Count;
            totalMultiplicadores += multiplicadoresBanda;

            // Puntos: 3 por QSO de distinto continente, 1 por mismo continente
            int puntosBanda = 0;
            foreach (Qso qso in qsosDeBanda)
            {
                EntidadDxcc? entidadContacto = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
                string continenteContacto = entidadContacto?.Continente ?? "??";
                puntosBanda += continenteContacto == continentePropio ? 1 : 3;
            }
            totalPuntos += puntosBanda;

            estadisticasBandas.Add(new
            {
                banda = etiquetaBanda,
                qsos = qsosDeBanda.Count,
                multiplicadores = multiplicadoresBanda
            });
        }

        long score = (long)totalPuntos * totalMultiplicadores;

        object respuesta = new
        {
            activo = true,
            contest = nombreContest,
            qsos = totalQsos,
            multiplicadores = totalMultiplicadores,
            puntos = totalPuntos,
            score,
            rate,
            bandas = estadisticasBandas
        };

        return Ok(respuesta);
    }

    /// <summary>
    /// Traduce el identificador de contest a su nombre legible.
    /// </summary>
    /// <param name="contest">Identificador del contest.</param>
    /// <returns>Nombre legible del contest.</returns>
    private static string ObtenerNombreContest(string? contest)
    {
        return contest switch
        {
            "cqww-ssb" => "CQ WW SSB 2026",
            "cqww-cw" => "CQ WW CW 2026",
            "cqwpx-ssb" => "CQ WPX SSB 2026",
            "cqwpx-cw" => "CQ WPX CW 2026",
            "arrl-dx-ssb" => "ARRL DX SSB 2026",
            "arrl-dx-cw" => "ARRL DX CW 2026",
            "iaru-hf" => "IARU HF Championship 2026",
            _ => "Ninguno activo"
        };
    }

    /// <summary>
    /// Obtiene la etiqueta de banda de contest a partir de la frecuencia del QSO.
    /// </summary>
    /// <param name="frecuencia">Frecuencia del QSO.</param>
    /// <returns>Etiqueta de banda (por ejemplo, "20m") o null si no es banda de contest.</returns>
    private static string? ObtenerEtiquetaBanda(Frecuencia frecuencia)
    {
        BandaRadio? banda = frecuencia.ObtenerBanda();

        if (banda is null)
        {
            return null;
        }

        return MapaBandaAEtiqueta.GetValueOrDefault(banda.Value);
    }

    /// <summary>
    /// Crea una respuesta de ejemplo cuando no hay QSOs reales en el periodo del contest.
    /// </summary>
    /// <param name="nombreContest">Nombre del contest activo.</param>
    /// <returns>Objeto anonimo con datos de ejemplo realistas.</returns>
    private static object CrearRespuestaEjemplo(string nombreContest)
    {
        return new
        {
            activo = true,
            contest = nombreContest,
            qsos = 347,
            multiplicadores = 89,
            puntos = 1041,
            score = 92649,
            rate = 42,
            bandas = new[]
            {
                new { banda = "160m", qsos = 12, multiplicadores = 8 },
                new { banda = "80m", qsos = 45, multiplicadores = 18 },
                new { banda = "40m", qsos = 78, multiplicadores = 22 },
                new { banda = "20m", qsos = 112, multiplicadores = 25 },
                new { banda = "15m", qsos = 67, multiplicadores = 12 },
                new { banda = "10m", qsos = 33, multiplicadores = 4 }
            }
        };
    }
}
