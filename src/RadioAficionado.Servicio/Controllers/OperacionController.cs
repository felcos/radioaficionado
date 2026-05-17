using Microsoft.AspNetCore.Mvc;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// Controlador para la vista principal de operacion y las paginas secundarias.
/// </summary>
public sealed class OperacionController : Controller
{
    /// <summary>
    /// Vista principal de operacion con waterfall, decodificaciones y panel TX.
    /// </summary>
    public IActionResult Index()
    {
        ViewData["Seccion"] = "Operacion";
        return View();
    }

    /// <summary>
    /// Pagina del logbook con historial de QSOs.
    /// </summary>
    public IActionResult Logbook()
    {
        ViewData["Seccion"] = "Logbook";
        return View();
    }

    /// <summary>
    /// Pagina del DX Cluster con spots en tiempo real.
    /// </summary>
    public IActionResult DxCluster()
    {
        ViewData["Seccion"] = "DxCluster";
        return View();
    }

    /// <summary>
    /// Pagina de seguimiento DXCC.
    /// </summary>
    public IActionResult Dxcc()
    {
        ViewData["Seccion"] = "Dxcc";
        return View();
    }

    /// <summary>
    /// Pagina de propagacion solar y condiciones de banda.
    /// </summary>
    public IActionResult Propagacion()
    {
        ViewData["Seccion"] = "Propagacion";
        return View();
    }

    /// <summary>
    /// Pagina de activaciones POTA/SOTA.
    /// </summary>
    public IActionResult Activaciones()
    {
        ViewData["Seccion"] = "Activaciones";
        return View();
    }

    /// <summary>
    /// Pagina de operacion en contest.
    /// </summary>
    public IActionResult Contest()
    {
        ViewData["Seccion"] = "Contest";
        return View();
    }

    /// <summary>
    /// Pagina de seguimiento de satelites.
    /// </summary>
    public IActionResult Satelites()
    {
        ViewData["Seccion"] = "Satelites";
        return View();
    }

    /// <summary>
    /// Mapa mundial de QSOs con great circle lines.
    /// </summary>
    public IActionResult MapaQsos()
    {
        ViewData["Seccion"] = "MapaQsos";
        return View();
    }

    /// <summary>
    /// Tabla del espectro radioelectrico con todas las bandas desde 0 Hz hasta THz.
    /// </summary>
    public IActionResult Espectro()
    {
        ViewData["Seccion"] = "Espectro";
        return View();
    }

    /// <summary>
    /// Herramientas utiles para radioaficionados: conversor potencia, distancia grids, RST, fonetico NATO.
    /// </summary>
    public IActionResult Herramientas()
    {
        ViewData["Seccion"] = "Herramientas";
        return View();
    }
}
