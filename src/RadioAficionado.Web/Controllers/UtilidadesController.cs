using Microsoft.AspNetCore.Mvc;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador de utilidades publicas para radioaficionados.
/// Herramientas, espectro radio, propagacion solar y satelites.
/// </summary>
public class UtilidadesController(ILogger<UtilidadesController> logger) : Controller
{
    private readonly ILogger<UtilidadesController> _logger = logger;

    /// <summary>
    /// Herramientas de calculo: conversor potencia, distancia grids, Maidenhead, plan bandas, RST, alfabeto NATO.
    /// </summary>
    public IActionResult Herramientas()
    {
        _logger.LogDebug("Accediendo a Utilidades/Herramientas");
        return View();
    }

    /// <summary>
    /// Tabla del espectro radioelectrico completo (0 Hz a THz).
    /// </summary>
    public IActionResult Espectro()
    {
        _logger.LogDebug("Accediendo a Utilidades/Espectro");
        return View();
    }

    /// <summary>
    /// Dashboard de propagacion solar con datos NOAA en tiempo real.
    /// </summary>
    public IActionResult Propagacion()
    {
        _logger.LogDebug("Accediendo a Utilidades/Propagacion");
        return View();
    }

    /// <summary>
    /// Informacion de satelites de radioaficion.
    /// </summary>
    public IActionResult Satelites()
    {
        _logger.LogDebug("Accediendo a Utilidades/Satelites");
        return View();
    }
}
