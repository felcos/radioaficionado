using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para el dashboard de estadisticas del logbook.
/// Proporciona datos agregados y endpoints JSON para los graficos.
/// </summary>
public class EstadisticasController(IRepositorioQso repositorioQso, ILogger<EstadisticasController> logger) : Controller
{
    private readonly IRepositorioQso _repositorioQso = repositorioQso;
    private readonly ILogger<EstadisticasController> _logger = logger;

    private static readonly Dictionary<string, string> NombresContinentes = new()
    {
        ["AF"] = "Africa",
        ["AN"] = "Antartida",
        ["AS"] = "Asia",
        ["EU"] = "Europa",
        ["NA"] = "America del Norte",
        ["OC"] = "Oceania",
        ["SA"] = "America del Sur"
    };

    /// <summary>
    /// Muestra el dashboard principal de estadisticas con tarjetas resumen.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Vista del dashboard de estadisticas.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        _logger.LogDebug("Cargando dashboard de estadisticas.");

        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);

        EstadisticasViewModel viewModel = new()
        {
            TotalQsos = todosLosQsos.Count
        };

        if (todosLosQsos.Count > 0)
        {
            EstadisticasDxcc estadisticasDxcc = new();
            HashSet<int> entidadesTrabajadas = estadisticasDxcc.EntidadesTrabajadas(todosLosQsos);

            HashSet<string> indicativosUnicos = new(
                todosLosQsos.Select(q => q.IndicativoContacto.Valor),
                StringComparer.OrdinalIgnoreCase);

            HashSet<BandaRadio> bandasUsadas = new(
                todosLosQsos
                    .Select(q => q.Frecuencia.ObtenerBanda())
                    .Where(b => b.HasValue)
                    .Select(b => b!.Value));

            HashSet<ModoOperacion> modosUsados = new(
                todosLosQsos.Select(q => q.Modo));

            viewModel.TotalEntidadesDxcc = entidadesTrabajadas.Count;
            viewModel.TotalIndicativosUnicos = indicativosUnicos.Count;
            viewModel.TotalBandas = bandasUsadas.Count;
            viewModel.TotalModos = modosUsados.Count;

            Qso primerQso = todosLosQsos.MinBy(q => q.FechaHoraInicio)!;
            Qso ultimoQso = todosLosQsos.MaxBy(q => q.FechaHoraInicio)!;
            viewModel.PrimerQso = primerQso.FechaHoraInicio;
            viewModel.UltimoQso = ultimoQso.FechaHoraInicio;

            // Promedio de QSOs por dia (solo dias con actividad)
            IGrouping<DateOnly, Qso>[] qsosPorDia = todosLosQsos
                .GroupBy(q => DateOnly.FromDateTime(q.FechaHoraInicio.UtcDateTime))
                .ToArray();

            viewModel.PromedioQsosPorDia = Math.Round(
                (double)todosLosQsos.Count / qsosPorDia.Length, 1);

            // Dia record
            IGrouping<DateOnly, Qso> diaRecord = qsosPorDia
                .OrderByDescending(g => g.Count())
                .First();

            viewModel.DiaRecord = diaRecord.Key.ToString("dd/MM/yyyy");
            viewModel.QsosEnDiaRecord = diaRecord.Count();

            // Banda mas usada
            IGrouping<BandaRadio?, Qso> bandaMasUsada = todosLosQsos
                .GroupBy(q => q.Frecuencia.ObtenerBanda())
                .Where(g => g.Key.HasValue)
                .OrderByDescending(g => g.Count())
                .First();

            viewModel.BandaMasUsada = bandaMasUsada.Key!.Value.ObtenerNombre();

            // Modo mas usado
            IGrouping<ModoOperacion, Qso> modoMasUsado = todosLosQsos
                .GroupBy(q => q.Modo)
                .OrderByDescending(g => g.Count())
                .First();

            viewModel.ModoMasUsado = modoMasUsado.Key.ToString();
        }

        return View(viewModel);
    }

    /// <summary>
    /// Endpoint JSON que retorna los QSOs agrupados por banda para el grafico de barras.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>JSON con etiquetas y cantidades por banda.</returns>
    [HttpGet]
    public async Task<IActionResult> DatosBandas(CancellationToken ct)
    {
        _logger.LogDebug("Obteniendo datos de QSOs por banda para graficos.");

        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);

        Dictionary<string, int> qsosPorBanda = todosLosQsos
            .Select(q => q.Frecuencia.ObtenerBanda())
            .Where(b => b.HasValue)
            .GroupBy(b => b!.Value)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key.ObtenerNombre(),
                g => g.Count());

        DatosBandasViewModel datos = new()
        {
            Etiquetas = qsosPorBanda.Keys.ToList(),
            Cantidades = qsosPorBanda.Values.ToList()
        };

        return Json(datos);
    }

    /// <summary>
    /// Endpoint JSON que retorna los QSOs agrupados por modo para el grafico de pastel.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>JSON con etiquetas y cantidades por modo.</returns>
    [HttpGet]
    public async Task<IActionResult> DatosModos(CancellationToken ct)
    {
        _logger.LogDebug("Obteniendo datos de QSOs por modo para graficos.");

        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);

        Dictionary<string, int> qsosPorModo = todosLosQsos
            .GroupBy(q => q.Modo)
            .OrderByDescending(g => g.Count())
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Count());

        DatosModosViewModel datos = new()
        {
            Etiquetas = qsosPorModo.Keys.ToList(),
            Cantidades = qsosPorModo.Values.ToList()
        };

        return Json(datos);
    }

    /// <summary>
    /// Endpoint JSON que retorna los QSOs agrupados por mes (ultimos 12 meses) para el grafico de lineas.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>JSON con etiquetas y cantidades por mes.</returns>
    [HttpGet]
    public async Task<IActionResult> DatosTemporales(CancellationToken ct)
    {
        _logger.LogDebug("Obteniendo datos temporales de QSOs para graficos.");

        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);

        DateTimeOffset hace12Meses = DateTimeOffset.UtcNow.AddMonths(-12);

        // Generar los ultimos 12 meses como etiquetas
        List<string> etiquetas = new();
        List<int> cantidades = new();

        string[] nombresMeses = { "Ene", "Feb", "Mar", "Abr", "May", "Jun",
                                  "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

        Dictionary<string, int> qsosPorMes = todosLosQsos
            .Where(q => q.FechaHoraInicio >= hace12Meses)
            .GroupBy(q => $"{q.FechaHoraInicio.Year}-{q.FechaHoraInicio.Month:D2}")
            .ToDictionary(g => g.Key, g => g.Count());

        for (int i = 11; i >= 0; i--)
        {
            DateTimeOffset mes = DateTimeOffset.UtcNow.AddMonths(-i);
            string clave = $"{mes.Year}-{mes.Month:D2}";
            string etiqueta = $"{nombresMeses[mes.Month - 1]} {mes.Year}";

            etiquetas.Add(etiqueta);
            cantidades.Add(qsosPorMes.GetValueOrDefault(clave, 0));
        }

        DatosTemporalesViewModel datos = new()
        {
            Etiquetas = etiquetas,
            Cantidades = cantidades
        };

        return Json(datos);
    }

    /// <summary>
    /// Endpoint JSON que retorna los QSOs agrupados por continente para el grafico de dona.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>JSON con etiquetas y cantidades por continente.</returns>
    [HttpGet]
    public async Task<IActionResult> DatosContinentes(CancellationToken ct)
    {
        _logger.LogDebug("Obteniendo datos de QSOs por continente para graficos.");

        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);

        Dictionary<string, int> qsosPorContinente = new();

        foreach (Qso qso in todosLosQsos)
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
            if (entidad is null || entidad.Eliminada)
            {
                continue;
            }

            string nombreContinente = NombresContinentes.GetValueOrDefault(
                entidad.Continente, entidad.Continente);

            if (qsosPorContinente.ContainsKey(nombreContinente))
            {
                qsosPorContinente[nombreContinente]++;
            }
            else
            {
                qsosPorContinente[nombreContinente] = 1;
            }
        }

        // Ordenar por cantidad descendente
        List<KeyValuePair<string, int>> ordenados = qsosPorContinente
            .OrderByDescending(kvp => kvp.Value)
            .ToList();

        DatosContinentesViewModel datos = new()
        {
            Etiquetas = ordenados.Select(kvp => kvp.Key).ToList(),
            Cantidades = ordenados.Select(kvp => kvp.Value).ToList()
        };

        return Json(datos);
    }
}
