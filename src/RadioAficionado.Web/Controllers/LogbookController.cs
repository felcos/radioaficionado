using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para el logbook público con paginación y filtros.
/// </summary>
public class LogbookController(IRepositorioQso repositorioQso, ILogger<LogbookController> logger) : Controller
{
    private readonly IRepositorioQso _repositorioQso = repositorioQso;
    private readonly ILogger<LogbookController> _logger = logger;

    private const int TamanoPaginaPorDefecto = 25;

    /// <summary>
    /// Muestra el logbook público con paginación y filtros opcionales.
    /// </summary>
    /// <param name="pagina">Número de página (base 1).</param>
    /// <param name="indicativo">Filtro parcial por indicativo.</param>
    /// <param name="modo">Filtro por modo de operación.</param>
    /// <param name="banda">Filtro por banda de radio.</param>
    /// <param name="fechaDesde">Filtro de fecha desde.</param>
    /// <param name="fechaHasta">Filtro de fecha hasta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Vista paginada del logbook.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        int pagina = 1,
        string? indicativo = null,
        ModoOperacion? modo = null,
        BandaRadio? banda = null,
        DateTimeOffset? fechaDesde = null,
        DateTimeOffset? fechaHasta = null,
        CancellationToken ct = default)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        _logger.LogDebug(
            "Cargando logbook - Página: {Pagina}, Indicativo: {Indicativo}, Modo: {Modo}, Banda: {Banda}",
            pagina, indicativo, modo, banda);

        FiltroQso? filtro = null;

        bool hayFiltros = !string.IsNullOrWhiteSpace(indicativo)
            || modo.HasValue
            || banda.HasValue
            || fechaDesde.HasValue
            || fechaHasta.HasValue;

        if (hayFiltros)
        {
            filtro = new FiltroQso(
                Indicativo: string.IsNullOrWhiteSpace(indicativo) ? null : indicativo.Trim(),
                Banda: banda,
                Modo: modo,
                FechaDesde: fechaDesde,
                FechaHasta: fechaHasta);
        }

        ResultadoPaginado<Qso> resultado = await _repositorioQso.ObtenerPaginadoAsync(
            pagina, TamanoPaginaPorDefecto, filtro, ct);

        IReadOnlyList<QsoResumenViewModel> qsos = resultado.Elementos
            .Select(MapearAResumen)
            .ToList();

        LogbookIndexViewModel viewModel = new()
        {
            Qsos = qsos,
            PaginaActual = pagina,
            TamanoPagina = TamanoPaginaPorDefecto,
            TotalElementos = resultado.TotalElementos,
            FiltroIndicativo = indicativo,
            FiltroModo = modo,
            FiltroBanda = banda,
            FiltroFechaDesde = fechaDesde,
            FiltroFechaHasta = fechaHasta,
            ModosDisponibles = ObtenerModosComunes(),
            BandasDisponibles = ObtenerBandasComunes()
        };

        return View(viewModel);
    }

    /// <summary>
    /// Muestra la vista de mapa con los contactos del logbook geolocalizados.
    /// </summary>
    /// <returns>Vista del mapa de contactos.</returns>
    [HttpGet]
    public IActionResult Mapa()
    {
        return View();
    }

    /// <summary>
    /// Endpoint JSON que retorna los datos de QSOs con localizador para mostrar en el mapa.
    /// Solo incluye contactos que tienen localizador Maidenhead asignado.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista JSON de contactos con coordenadas geográficas.</returns>
    [HttpGet]
    public async Task<IActionResult> MapaDatos(CancellationToken ct)
    {
        _logger.LogDebug("Obteniendo datos de QSOs para el mapa de contactos.");

        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);

        IReadOnlyList<MapaContactoViewModel> marcadores = todosLosQsos
            .Where(q => q.LocalizadorContacto.HasValue)
            .Select(q =>
            {
                Coordenadas coordenadas = q.LocalizadorContacto!.Value.ObtenerCoordenadas();
                BandaRadio? banda = q.Frecuencia.ObtenerBanda();

                return new MapaContactoViewModel
                {
                    Latitud = coordenadas.Latitud,
                    Longitud = coordenadas.Longitud,
                    Indicativo = q.IndicativoContacto.Valor,
                    Fecha = q.FechaHoraInicio.ToString("yyyy-MM-dd HH:mm UTC"),
                    Banda = banda?.ObtenerNombre(),
                    Modo = q.Modo.ToString(),
                    Localizador = q.LocalizadorContacto!.Value.Valor
                };
            })
            .ToList();

        _logger.LogDebug("Mapa de contactos: {Total} QSOs con localizador de {TodosTotal} totales.",
            marcadores.Count, todosLosQsos.Count);

        return Json(marcadores);
    }

    /// <summary>
    /// Muestra el detalle de un QSO específico.
    /// </summary>
    /// <param name="id">Identificador del QSO.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Vista de detalle del QSO, o NotFound si no existe.</returns>
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken ct)
    {
        _logger.LogDebug("Cargando detalle del QSO: {QsoId}", id);

        Qso? qso = await _repositorioQso.ObtenerPorIdAsync(id, ct);

        if (qso is null)
        {
            _logger.LogWarning("QSO no encontrado: {QsoId}", id);
            return NotFound();
        }

        QsoDetalleViewModel viewModel = MapearADetalle(qso);

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

    /// <summary>
    /// Mapea una entidad Qso a un ViewModel de detalle completo.
    /// </summary>
    /// <param name="qso">La entidad QSO a mapear.</param>
    /// <returns>ViewModel de detalle del QSO.</returns>
    private static QsoDetalleViewModel MapearADetalle(Qso qso)
    {
        BandaRadio? banda = qso.Frecuencia.ObtenerBanda();

        return new QsoDetalleViewModel
        {
            Id = qso.Id,
            IndicativoPropio = qso.IndicativoPropio.Valor,
            IndicativoContacto = qso.IndicativoContacto.Valor,
            FechaHoraInicio = qso.FechaHoraInicio,
            FechaHoraFin = qso.FechaHoraFin,
            Frecuencia = qso.Frecuencia.ToString(),
            Banda = banda?.ObtenerNombre(),
            Modo = qso.Modo.ToString(),
            SenalEnviada = qso.SenalEnviada,
            SenalRecibida = qso.SenalRecibida,
            Potencia = qso.Potencia,
            LocalizadorContacto = qso.LocalizadorContacto?.ToString(),
            Notas = qso.Notas,
            FechaCreacion = qso.FechaCreacion,
            FechaModificacion = qso.FechaModificacion
        };
    }

    /// <summary>
    /// Obtiene los modos de operación más comunes para el filtro desplegable.
    /// </summary>
    /// <returns>Lista de modos comunes ordenados.</returns>
    private static IReadOnlyList<ModoOperacion> ObtenerModosComunes()
    {
        return new List<ModoOperacion>
        {
            ModoOperacion.SSB,
            ModoOperacion.CW,
            ModoOperacion.FT8,
            ModoOperacion.FT4,
            ModoOperacion.FM,
            ModoOperacion.AM,
            ModoOperacion.RTTY,
            ModoOperacion.PSK,
            ModoOperacion.DIGITALVOICE,
            ModoOperacion.JT65,
            ModoOperacion.JT9,
            ModoOperacion.OLIVIA,
            ModoOperacion.MFSK,
            ModoOperacion.WSPR
        };
    }

    /// <summary>
    /// Obtiene las bandas de radioaficionado más comunes para el filtro desplegable.
    /// </summary>
    /// <returns>Lista de bandas comunes ordenadas.</returns>
    private static IReadOnlyList<BandaRadio> ObtenerBandasComunes()
    {
        return new List<BandaRadio>
        {
            BandaRadio.Banda160m,
            BandaRadio.Banda80m,
            BandaRadio.Banda40m,
            BandaRadio.Banda30m,
            BandaRadio.Banda20m,
            BandaRadio.Banda17m,
            BandaRadio.Banda15m,
            BandaRadio.Banda12m,
            BandaRadio.Banda10m,
            BandaRadio.Banda6m,
            BandaRadio.Banda2m,
            BandaRadio.Banda70cm
        };
    }
}
