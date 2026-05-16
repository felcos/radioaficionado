using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para el logbook privado del usuario autenticado.
/// Solo muestra y gestiona los QSOs cuyo IndicativoPropio coincide con el indicativo del usuario.
/// </summary>
[Authorize]
public class LogbookPrivadoController(
    IRepositorioQso repositorioQso,
    UserManager<UsuarioRadio> userManager,
    ILogger<LogbookPrivadoController> logger) : Controller
{
    private readonly IRepositorioQso _repositorioQso = repositorioQso;
    private readonly UserManager<UsuarioRadio> _userManager = userManager;
    private readonly ILogger<LogbookPrivadoController> _logger = logger;

    private const int TamanoPaginaPorDefecto = 25;

    /// <summary>
    /// Muestra el logbook privado del usuario con paginacion y filtros opcionales.
    /// </summary>
    /// <param name="pagina">Numero de pagina (base 1).</param>
    /// <param name="indicativo">Filtro parcial por indicativo contacto.</param>
    /// <param name="modo">Filtro por modo de operacion.</param>
    /// <param name="banda">Filtro por banda de radio.</param>
    /// <param name="fechaDesde">Filtro de fecha desde.</param>
    /// <param name="fechaHasta">Filtro de fecha hasta.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Vista paginada del logbook privado.</returns>
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
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null || string.IsNullOrWhiteSpace(usuario.Indicativo))
        {
            _logger.LogWarning("Usuario autenticado sin indicativo intentando acceder al logbook privado.");
            return RedirectToAction("Perfil", "Cuenta");
        }

        if (pagina < 1)
        {
            pagina = 1;
        }

        string indicativoUsuario = usuario.Indicativo.Trim().ToUpperInvariant();

        _logger.LogDebug(
            "Cargando logbook privado - Usuario: {Indicativo}, Pagina: {Pagina}, Filtro: {Filtro}, Modo: {Modo}, Banda: {Banda}",
            indicativoUsuario, pagina, indicativo, modo, banda);

        FiltroQso filtro = new(
            Indicativo: indicativoUsuario,
            Banda: banda,
            Modo: modo,
            FechaDesde: fechaDesde,
            FechaHasta: fechaHasta);

        ResultadoPaginado<Qso> resultado = await _repositorioQso.ObtenerPaginadoAsync(
            pagina, TamanoPaginaPorDefecto, filtro, ct);

        IReadOnlyList<QsoResumenViewModel> qsos = resultado.Elementos
            .Select(MapearAResumen)
            .ToList();

        LogbookPrivadoIndexViewModel viewModel = new()
        {
            Qsos = qsos,
            PaginaActual = pagina,
            TamanoPagina = TamanoPaginaPorDefecto,
            TotalElementos = resultado.TotalElementos,
            IndicativoUsuario = indicativoUsuario,
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
    /// Muestra el formulario para crear un nuevo QSO.
    /// </summary>
    /// <returns>Vista con el formulario de creacion.</returns>
    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null || string.IsNullOrWhiteSpace(usuario.Indicativo))
        {
            return RedirectToAction("Perfil", "Cuenta");
        }

        CrearQsoViewModel viewModel = new()
        {
            ModosDisponibles = ObtenerModosComunes()
        };

        return View(viewModel);
    }

    /// <summary>
    /// Procesa el formulario de creacion de un nuevo QSO.
    /// </summary>
    /// <param name="viewModel">Datos del formulario.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Redireccion al Index si es exitoso, o la vista con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearQsoViewModel viewModel, CancellationToken ct)
    {
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null || string.IsNullOrWhiteSpace(usuario.Indicativo))
        {
            return RedirectToAction("Perfil", "Cuenta");
        }

        if (!ModelState.IsValid)
        {
            viewModel.ModosDisponibles = ObtenerModosComunes();
            return View(viewModel);
        }

        try
        {
            Indicativo indicativoPropio = new(usuario.Indicativo);
            Indicativo indicativoContacto = new(viewModel.IndicativoContacto);
            Frecuencia frecuencia = Frecuencia.DesdeMHz(viewModel.FrecuenciaMHz);
            DateTimeOffset fechaInicio = new(viewModel.FechaHoraInicio, TimeSpan.Zero);

            Localizador? localizador = null;
            if (!string.IsNullOrWhiteSpace(viewModel.LocalizadorContacto))
            {
                localizador = new Localizador(viewModel.LocalizadorContacto);
            }

            Qso qso = Qso.Crear(
                indicativoPropio: indicativoPropio,
                indicativoContacto: indicativoContacto,
                fechaHoraInicio: fechaInicio,
                frecuencia: frecuencia,
                modo: viewModel.Modo,
                senalEnviada: viewModel.SenalEnviada,
                potencia: viewModel.Potencia,
                localizadorContacto: localizador,
                notas: viewModel.Notas);

            if (viewModel.FechaHoraFin.HasValue && !string.IsNullOrWhiteSpace(viewModel.SenalRecibida))
            {
                DateTimeOffset fechaFin = new(viewModel.FechaHoraFin.Value, TimeSpan.Zero);
                qso.Completar(fechaFin, viewModel.SenalRecibida);
            }

            await _repositorioQso.AgregarAsync(qso, ct);

            _logger.LogInformation(
                "QSO creado: {IndicativoPropio} -> {IndicativoContacto} en {Frecuencia} {Modo}",
                usuario.Indicativo, viewModel.IndicativoContacto, viewModel.FrecuenciaMHz, viewModel.Modo);

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validacion al crear QSO.");
            ModelState.AddModelError(string.Empty, ex.Message);
            viewModel.ModosDisponibles = ObtenerModosComunes();
            return View(viewModel);
        }
    }

    /// <summary>
    /// Muestra el formulario de edicion de un QSO existente del usuario.
    /// </summary>
    /// <param name="id">Identificador del QSO.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Vista de edicion, Forbid si no es propio, o NotFound si no existe.</returns>
    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken ct)
    {
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null || string.IsNullOrWhiteSpace(usuario.Indicativo))
        {
            return RedirectToAction("Perfil", "Cuenta");
        }

        Qso? qso = await _repositorioQso.ObtenerPorIdAsync(id, ct);

        if (qso is null)
        {
            _logger.LogWarning("QSO no encontrado para edicion: {QsoId}", id);
            return NotFound();
        }

        if (!EsQsoDelUsuario(qso, usuario.Indicativo))
        {
            _logger.LogWarning(
                "Intento de edicion de QSO ajeno: {QsoId} por usuario {Indicativo}", id, usuario.Indicativo);
            return Forbid();
        }

        EditarQsoViewModel viewModel = new()
        {
            Id = qso.Id,
            IndicativoContacto = qso.IndicativoContacto.Valor,
            FrecuenciaMHz = qso.Frecuencia.MHz,
            Modo = qso.Modo,
            FechaHoraInicio = qso.FechaHoraInicio.UtcDateTime,
            FechaHoraFin = qso.FechaHoraFin?.UtcDateTime,
            SenalEnviada = qso.SenalEnviada,
            SenalRecibida = string.IsNullOrWhiteSpace(qso.SenalRecibida) ? null : qso.SenalRecibida,
            Potencia = qso.Potencia,
            LocalizadorContacto = qso.LocalizadorContacto?.Valor,
            Notas = qso.Notas,
            ModosDisponibles = ObtenerModosComunes()
        };

        return View(viewModel);
    }

    /// <summary>
    /// Procesa el formulario de edicion de un QSO.
    /// Dado que Qso tiene setters privados, se elimina el QSO existente y se recrea con los nuevos datos.
    /// </summary>
    /// <param name="viewModel">Datos del formulario de edicion.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Redireccion al Index si es exitoso, o la vista con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarQsoViewModel viewModel, CancellationToken ct)
    {
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null || string.IsNullOrWhiteSpace(usuario.Indicativo))
        {
            return RedirectToAction("Perfil", "Cuenta");
        }

        Qso? qsoExistente = await _repositorioQso.ObtenerPorIdAsync(viewModel.Id, ct);

        if (qsoExistente is null)
        {
            return NotFound();
        }

        if (!EsQsoDelUsuario(qsoExistente, usuario.Indicativo))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            viewModel.ModosDisponibles = ObtenerModosComunes();
            return View(viewModel);
        }

        try
        {
            Indicativo indicativoPropio = new(usuario.Indicativo);
            Indicativo indicativoContacto = new(viewModel.IndicativoContacto);
            Frecuencia frecuencia = Frecuencia.DesdeMHz(viewModel.FrecuenciaMHz);
            DateTimeOffset fechaInicio = new(viewModel.FechaHoraInicio, TimeSpan.Zero);

            Localizador? localizador = null;
            if (!string.IsNullOrWhiteSpace(viewModel.LocalizadorContacto))
            {
                localizador = new Localizador(viewModel.LocalizadorContacto);
            }

            await _repositorioQso.EliminarAsync(qsoExistente, ct);

            Qso nuevoQso = Qso.Crear(
                indicativoPropio: indicativoPropio,
                indicativoContacto: indicativoContacto,
                fechaHoraInicio: fechaInicio,
                frecuencia: frecuencia,
                modo: viewModel.Modo,
                senalEnviada: viewModel.SenalEnviada,
                potencia: viewModel.Potencia,
                localizadorContacto: localizador,
                notas: viewModel.Notas);

            if (viewModel.FechaHoraFin.HasValue && !string.IsNullOrWhiteSpace(viewModel.SenalRecibida))
            {
                DateTimeOffset fechaFin = new(viewModel.FechaHoraFin.Value, TimeSpan.Zero);
                nuevoQso.Completar(fechaFin, viewModel.SenalRecibida);
            }

            await _repositorioQso.AgregarAsync(nuevoQso, ct);

            _logger.LogInformation(
                "QSO editado (recreado): {IndicativoPropio} -> {IndicativoContacto} en {Frecuencia} {Modo}",
                usuario.Indicativo, viewModel.IndicativoContacto, viewModel.FrecuenciaMHz, viewModel.Modo);

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validacion al editar QSO: {QsoId}", viewModel.Id);
            ModelState.AddModelError(string.Empty, ex.Message);
            viewModel.ModosDisponibles = ObtenerModosComunes();
            return View(viewModel);
        }
    }

    /// <summary>
    /// Muestra el detalle de un QSO propio del usuario.
    /// </summary>
    /// <param name="id">Identificador del QSO.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Vista de detalle, Forbid si no es propio, o NotFound si no existe.</returns>
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken ct)
    {
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null || string.IsNullOrWhiteSpace(usuario.Indicativo))
        {
            return RedirectToAction("Perfil", "Cuenta");
        }

        Qso? qso = await _repositorioQso.ObtenerPorIdAsync(id, ct);

        if (qso is null)
        {
            _logger.LogWarning("QSO no encontrado para detalle: {QsoId}", id);
            return NotFound();
        }

        if (!EsQsoDelUsuario(qso, usuario.Indicativo))
        {
            _logger.LogWarning(
                "Intento de ver detalle de QSO ajeno: {QsoId} por usuario {Indicativo}", id, usuario.Indicativo);
            return Forbid();
        }

        QsoDetalleViewModel viewModel = MapearADetalle(qso);

        return View(viewModel);
    }

    /// <summary>
    /// Elimina un QSO propio del usuario.
    /// </summary>
    /// <param name="id">Identificador del QSO a eliminar.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Redireccion al Index, Forbid si no es propio, o NotFound si no existe.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null || string.IsNullOrWhiteSpace(usuario.Indicativo))
        {
            return RedirectToAction("Perfil", "Cuenta");
        }

        Qso? qso = await _repositorioQso.ObtenerPorIdAsync(id, ct);

        if (qso is null)
        {
            _logger.LogWarning("QSO no encontrado para eliminacion: {QsoId}", id);
            return NotFound();
        }

        if (!EsQsoDelUsuario(qso, usuario.Indicativo))
        {
            _logger.LogWarning(
                "Intento de eliminacion de QSO ajeno: {QsoId} por usuario {Indicativo}", id, usuario.Indicativo);
            return Forbid();
        }

        await _repositorioQso.EliminarAsync(qso, ct);

        _logger.LogInformation(
            "QSO eliminado: {QsoId} por usuario {Indicativo}", id, usuario.Indicativo);

        return RedirectToAction(nameof(Index));
    }

    // ── Metodos privados ──────────────────────────────────────────

    /// <summary>
    /// Verifica si un QSO pertenece al usuario comparando IndicativoPropio.
    /// </summary>
    /// <param name="qso">El QSO a verificar.</param>
    /// <param name="indicativoUsuario">El indicativo del usuario autenticado.</param>
    /// <returns>True si el QSO pertenece al usuario.</returns>
    private static bool EsQsoDelUsuario(Qso qso, string indicativoUsuario)
    {
        return string.Equals(
            qso.IndicativoPropio.Valor,
            indicativoUsuario.Trim().ToUpperInvariant(),
            StringComparison.OrdinalIgnoreCase);
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
    /// Obtiene los modos de operacion mas comunes para el filtro desplegable.
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
    /// Obtiene las bandas de radioaficionado mas comunes para el filtro desplegable.
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
