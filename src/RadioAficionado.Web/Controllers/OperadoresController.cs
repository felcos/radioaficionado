using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para el directorio público de operadores y sus perfiles públicos.
/// </summary>
public class OperadoresController(
    UserManager<UsuarioRadio> userManager,
    IRepositorioQso repositorioQso,
    ILogger<OperadoresController> logger) : Controller
{
    private readonly UserManager<UsuarioRadio> _userManager = userManager;
    private readonly IRepositorioQso _repositorioQso = repositorioQso;
    private readonly ILogger<OperadoresController> _logger = logger;

    private const int TamanoPaginaPorDefecto = 20;

    /// <summary>
    /// Muestra el listado paginado de operadores registrados, con búsqueda por indicativo o nombre.
    /// </summary>
    /// <param name="pagina">Número de página (base 1).</param>
    /// <param name="busqueda">Término de búsqueda por indicativo o nombre.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Vista con el listado de operadores.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(int pagina = 1, string? busqueda = null, CancellationToken ct = default)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        _logger.LogDebug("Cargando directorio de operadores - Página: {Pagina}, Búsqueda: {Busqueda}", pagina, busqueda);

        IQueryable<UsuarioRadio> consulta = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string terminoBusqueda = busqueda.Trim().ToUpperInvariant();
            consulta = consulta.Where(u =>
                u.Indicativo.ToUpper().Contains(terminoBusqueda) ||
                u.Nombre.ToUpper().Contains(terminoBusqueda));
        }

        int totalElementos = await consulta.CountAsync(ct);

        List<UsuarioRadio> usuarios = await consulta
            .OrderBy(u => u.Indicativo)
            .Skip((pagina - 1) * TamanoPaginaPorDefecto)
            .Take(TamanoPaginaPorDefecto)
            .ToListAsync(ct);

        IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(ct);

        List<OperadorResumenViewModel> operadores = usuarios.Select(u =>
        {
            int totalQsos = todosLosQsos.Count(q =>
                q.IndicativoPropio.Valor.Equals(u.Indicativo, StringComparison.OrdinalIgnoreCase));

            return new OperadorResumenViewModel
            {
                Indicativo = u.Indicativo,
                Nombre = u.Nombre,
                Localizador = u.Localizador,
                FechaRegistro = u.FechaRegistro,
                TotalQsos = totalQsos
            };
        }).ToList();

        OperadoresIndexViewModel viewModel = new()
        {
            Operadores = operadores,
            PaginaActual = pagina,
            TamanoPagina = TamanoPaginaPorDefecto,
            TotalElementos = totalElementos,
            Busqueda = busqueda
        };

        return View(viewModel);
    }

    /// <summary>
    /// Muestra el perfil público de un operador identificado por su indicativo.
    /// Incluye estadísticas de QSOs, bandas favoritas y últimos contactos.
    /// </summary>
    /// <param name="indicativo">Indicativo del operador a consultar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Vista con el perfil público del operador, o NotFound si no existe.</returns>
    [HttpGet]
    public async Task<IActionResult> Perfil(string indicativo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(indicativo))
        {
            return NotFound();
        }

        _logger.LogDebug("Cargando perfil público del operador: {Indicativo}", indicativo);

        UsuarioRadio? usuario = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Indicativo == indicativo.ToUpperInvariant(), ct);

        if (usuario is null)
        {
            _logger.LogWarning("Operador no encontrado: {Indicativo}", indicativo);
            return NotFound();
        }

        Indicativo indicativoObjeto = new(usuario.Indicativo);
        IReadOnlyList<Qso> qsosDelOperador = await _repositorioQso.BuscarPorIndicativoAsync(indicativoObjeto, ct);

        IReadOnlyList<Qso> qsosPropios = qsosDelOperador
            .Where(q => q.IndicativoPropio.Valor.Equals(usuario.Indicativo, StringComparison.OrdinalIgnoreCase))
            .ToList();

        int indicativosUnicos = qsosPropios
            .Select(q => q.IndicativoContacto.Valor)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        IReadOnlyList<BandaFavoritaViewModel> bandasFavoritas = qsosPropios
            .Select(q => q.Frecuencia.ObtenerBanda())
            .Where(b => b.HasValue)
            .GroupBy(b => b!.Value)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new BandaFavoritaViewModel
            {
                Nombre = g.Key.ObtenerNombre(),
                CantidadQsos = g.Count()
            })
            .ToList();

        IReadOnlyList<QsoResumenViewModel> ultimosQsos = qsosPropios
            .OrderByDescending(q => q.FechaHoraInicio)
            .Take(10)
            .Select(q =>
            {
                BandaRadio? banda = q.Frecuencia.ObtenerBanda();
                return new QsoResumenViewModel
                {
                    Id = q.Id,
                    IndicativoPropio = q.IndicativoPropio.Valor,
                    IndicativoContacto = q.IndicativoContacto.Valor,
                    FechaHora = q.FechaHoraInicio,
                    Frecuencia = q.Frecuencia.ToString(),
                    Modo = q.Modo.ToString(),
                    Banda = banda?.ObtenerNombre()
                };
            })
            .ToList();

        PerfilPublicoViewModel viewModel = new()
        {
            Indicativo = usuario.Indicativo,
            Nombre = usuario.Nombre,
            Localizador = usuario.Localizador,
            Biografia = usuario.Biografia,
            RegionItu = usuario.RegionItu,
            FechaRegistro = usuario.FechaRegistro,
            TotalQsos = qsosPropios.Count,
            IndicativosUnicos = indicativosUnicos,
            BandasFavoritas = bandasFavoritas,
            UltimosQsos = ultimosQsos
        };

        return View(viewModel);
    }
}
