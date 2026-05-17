using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Web.Data;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para el foro de la comunidad de radioaficionados.
/// Gestiona hilos, respuestas y paginacion.
/// Requiere autenticacion para acceder.
/// </summary>
public class ForoController(ContextoIdentidadRadioAficionado contexto, ILogger<ForoController> logger) : Controller
{
    private readonly ContextoIdentidadRadioAficionado _contexto = contexto;
    private readonly ILogger<ForoController> _logger = logger;

    private const int TamanoPaginaHilos = 20;
    private const int TamanoPaginaRespuestas = 20;

    /// <summary>
    /// Muestra la lista paginada de hilos del foro con filtro opcional por categoría.
    /// Los hilos fijados aparecen siempre primero.
    /// </summary>
    /// <param name="pagina">Número de página (base 1).</param>
    /// <param name="categoria">Filtro opcional por categoría.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Vista con la lista de hilos.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(int pagina = 1, CategoriaForo? categoria = null, CancellationToken ct = default)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        _logger.LogDebug("Cargando foro - Página: {Pagina}, Categoría: {Categoria}", pagina, categoria);

        IQueryable<HiloForo> consulta = _contexto.HilosForo
            .Include(h => h.Autor)
            .Include(h => h.Respuestas)
            .AsNoTracking();

        if (categoria.HasValue)
        {
            consulta = consulta.Where(h => h.Categoria == categoria.Value);
        }

        int totalElementos = await consulta.CountAsync(ct);

        IReadOnlyList<HiloResumenViewModel> hilos = await consulta
            .OrderByDescending(h => h.Fijado)
            .ThenByDescending(h => h.FechaUltimaRespuesta)
            .Skip((pagina - 1) * TamanoPaginaHilos)
            .Take(TamanoPaginaHilos)
            .Select(h => new HiloResumenViewModel
            {
                Id = h.Id,
                Titulo = h.Titulo,
                AutorIndicativo = h.Autor != null ? h.Autor.Indicativo : "Desconocido",
                AutorNombre = h.Autor != null ? h.Autor.Nombre : "Desconocido",
                FechaCreacion = h.FechaCreacion,
                FechaUltimaRespuesta = h.FechaUltimaRespuesta,
                Categoria = h.Categoria,
                Fijado = h.Fijado,
                Cerrado = h.Cerrado,
                NumeroRespuestas = h.Respuestas.Count
            })
            .ToListAsync(ct);

        ForoIndexViewModel viewModel = new()
        {
            Hilos = hilos,
            PaginaActual = pagina,
            TamanoPagina = TamanoPaginaHilos,
            TotalElementos = totalElementos,
            FiltroCategoria = categoria
        };

        return View(viewModel);
    }

    /// <summary>
    /// Muestra el detalle de un hilo con sus respuestas paginadas.
    /// </summary>
    /// <param name="id">Identificador del hilo.</param>
    /// <param name="pagina">Número de página de respuestas (base 1).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Vista de detalle del hilo, o NotFound si no existe.</returns>
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id, int pagina = 1, CancellationToken ct = default)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        _logger.LogDebug("Cargando detalle del hilo: {HiloId}, Página: {Pagina}", id, pagina);

        HiloForo? hilo = await _contexto.HilosForo
            .Include(h => h.Autor)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (hilo is null)
        {
            _logger.LogWarning("Hilo no encontrado: {HiloId}", id);
            return NotFound();
        }

        int totalRespuestas = await _contexto.RespuestasForo
            .Where(r => r.HiloId == id)
            .CountAsync(ct);

        IReadOnlyList<RespuestaViewModel> respuestas = await _contexto.RespuestasForo
            .Where(r => r.HiloId == id)
            .Include(r => r.Autor)
            .OrderBy(r => r.FechaCreacion)
            .Skip((pagina - 1) * TamanoPaginaRespuestas)
            .Take(TamanoPaginaRespuestas)
            .Select(r => new RespuestaViewModel
            {
                Id = r.Id,
                Contenido = r.Contenido,
                AutorIndicativo = r.Autor != null ? r.Autor.Indicativo : "Desconocido",
                AutorNombre = r.Autor != null ? r.Autor.Nombre : "Desconocido",
                FechaCreacion = r.FechaCreacion,
                FechaEdicion = r.FechaEdicion
            })
            .ToListAsync(ct);

        HiloDetalleViewModel viewModel = new()
        {
            Id = hilo.Id,
            Titulo = hilo.Titulo,
            Contenido = hilo.Contenido,
            AutorIndicativo = hilo.Autor?.Indicativo ?? "Desconocido",
            AutorNombre = hilo.Autor?.Nombre ?? "Desconocido",
            FechaCreacion = hilo.FechaCreacion,
            Categoria = hilo.Categoria,
            Cerrado = hilo.Cerrado,
            Respuestas = respuestas,
            PaginaActual = pagina,
            TamanoPagina = TamanoPaginaRespuestas,
            TotalRespuestas = totalRespuestas
        };

        return View(viewModel);
    }

    /// <summary>
    /// Muestra el formulario para crear un nuevo hilo.
    /// </summary>
    /// <returns>Vista con el formulario de creación.</returns>
    [HttpGet]
    [Authorize]
    public IActionResult CrearHilo()
    {
        return View(new CrearHiloViewModel());
    }

    /// <summary>
    /// Procesa la creación de un nuevo hilo en el foro.
    /// </summary>
    /// <param name="modelo">Datos del formulario de creación.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Redirección al hilo creado, o el formulario con errores.</returns>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearHilo(CrearHiloViewModel modelo, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        string? usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Forbid();
        }

        DateTime ahora = DateTime.UtcNow;

        HiloForo hilo = new()
        {
            Id = Guid.NewGuid(),
            Titulo = modelo.Titulo.Trim(),
            Contenido = modelo.Contenido.Trim(),
            AutorId = usuarioId,
            FechaCreacion = ahora,
            FechaUltimaRespuesta = ahora,
            Categoria = modelo.Categoria,
            Fijado = false,
            Cerrado = false
        };

        _contexto.HilosForo.Add(hilo);
        await _contexto.SaveChangesAsync(ct);

        _logger.LogInformation("Hilo creado: {HiloId} por usuario {UsuarioId}", hilo.Id, usuarioId);

        return RedirectToAction(nameof(Detalle), new { id = hilo.Id });
    }

    /// <summary>
    /// Procesa una nueva respuesta a un hilo del foro.
    /// </summary>
    /// <param name="modelo">Datos del formulario de respuesta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Redirección al hilo con la última página de respuestas.</returns>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Responder(ResponderHiloViewModel modelo, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Detalle), new { id = modelo.HiloId });
        }

        string? usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Forbid();
        }

        HiloForo? hilo = await _contexto.HilosForo.FindAsync(new object[] { modelo.HiloId }, ct);

        if (hilo is null)
        {
            return NotFound();
        }

        if (hilo.Cerrado)
        {
            _logger.LogWarning("Intento de responder a hilo cerrado: {HiloId} por usuario {UsuarioId}", hilo.Id, usuarioId);
            TempData["Error"] = "Este hilo está cerrado y no acepta nuevas respuestas.";
            return RedirectToAction(nameof(Detalle), new { id = modelo.HiloId });
        }

        DateTime ahora = DateTime.UtcNow;

        RespuestaForo respuesta = new()
        {
            Id = Guid.NewGuid(),
            HiloId = modelo.HiloId,
            Contenido = modelo.Contenido.Trim(),
            AutorId = usuarioId,
            FechaCreacion = ahora
        };

        hilo.FechaUltimaRespuesta = ahora;

        _contexto.RespuestasForo.Add(respuesta);
        await _contexto.SaveChangesAsync(ct);

        _logger.LogInformation("Respuesta creada en hilo {HiloId} por usuario {UsuarioId}", modelo.HiloId, usuarioId);

        // Calcular la última página para redirigir ahí
        int totalRespuestas = await _contexto.RespuestasForo
            .Where(r => r.HiloId == modelo.HiloId)
            .CountAsync(ct);
        int ultimaPagina = (int)Math.Ceiling((double)totalRespuestas / TamanoPaginaRespuestas);

        return RedirectToAction(nameof(Detalle), new { id = modelo.HiloId, pagina = ultimaPagina });
    }
}
