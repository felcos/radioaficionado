using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Web.Models;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para la landing page publica y paginas informativas del sitio.
/// </summary>
public class InicioController(ILogger<InicioController> logger) : Controller
{
    private readonly ILogger<InicioController> _logger = logger;

    /// <summary>
    /// Muestra la landing page publica del sitio con informacion de features y descarga.
    /// </summary>
    /// <returns>Vista de la landing page.</returns>
    [HttpGet]
    public IActionResult Index()
    {
        _logger.LogDebug("Cargando landing page publica");
        return View();
    }

    /// <summary>
    /// Muestra la página de política de privacidad del sitio.
    /// </summary>
    /// <returns>Vista de privacidad.</returns>
    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Muestra la página de error con el identificador de solicitud.
    /// Usada por UseExceptionHandler("/Inicio/Error") en Program.cs.
    /// </summary>
    /// <returns>Vista de error con el RequestId.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
