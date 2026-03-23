using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para la página de inicio con estadísticas generales del logbook.
/// </summary>
public class InicioController(IRepositorioQso repositorioQso, ILogger<InicioController> logger) : Controller
{
    private readonly IRepositorioQso _repositorioQso = repositorioQso;
    private readonly ILogger<InicioController> _logger = logger;

    /// <summary>
    /// Muestra la página de inicio con estadísticas generales y los últimos QSOs.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Vista de inicio con estadísticas.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        _logger.LogDebug("Cargando página de inicio con estadísticas generales");

        int totalQsos = await _repositorioQso.ContarAsync(ct);

        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);

        int totalIndicativosUnicos = todosLosQsos
            .Select(q => q.IndicativoContacto.Valor)
            .Distinct()
            .Count();

        int totalBandas = todosLosQsos
            .Select(q => q.Frecuencia.ObtenerBanda())
            .Where(b => b.HasValue)
            .Select(b => b!.Value)
            .Distinct()
            .Count();

        int totalModos = todosLosQsos
            .Select(q => q.Modo)
            .Distinct()
            .Count();

        DateTimeOffset? ultimoQso = todosLosQsos
            .OrderByDescending(q => q.FechaHoraInicio)
            .Select(q => (DateTimeOffset?)q.FechaHoraInicio)
            .FirstOrDefault();

        IReadOnlyList<QsoResumenViewModel> ultimosQsos = todosLosQsos
            .OrderByDescending(q => q.FechaHoraInicio)
            .Take(5)
            .Select(MapearAResumen)
            .ToList();

        InicioViewModel viewModel = new()
        {
            TotalQsos = totalQsos,
            TotalIndicativosUnicos = totalIndicativosUnicos,
            TotalBandas = totalBandas,
            TotalModos = totalModos,
            UltimoQso = ultimoQso,
            UltimosQsos = ultimosQsos
        };

        return View(viewModel);
    }

    /// <summary>
    /// Mapea una entidad Qso a un ViewModel resumido.
    /// </summary>
    /// <param name="qso">La entidad QSO a mapear.</param>
    /// <returns>ViewModel resumido del QSO.</returns>
    private static QsoResumenViewModel MapearAResumen(Qso qso)
    {
        BandaRadio? banda = qso.Frecuencia.ObtenerBanda();

        return new QsoResumenViewModel
        {
            Id = qso.Id,
            IndicativoPropio = qso.IndicativoPropio.Valor,
            IndicativoContacto = qso.IndicativoContacto.Valor,
            FechaHora = qso.FechaHoraInicio,
            Frecuencia = qso.Frecuencia.ToString(),
            Modo = qso.Modo.ToString(),
            Banda = banda?.ObtenerNombre()
        };
    }
}
