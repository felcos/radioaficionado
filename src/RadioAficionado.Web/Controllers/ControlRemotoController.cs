using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para la vista de control remoto del rig via web.
/// </summary>
[Authorize]
public class ControlRemotoController : Controller
{
    private readonly RegistroServiciosConectados _registro;
    private readonly ILogger<ControlRemotoController> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ControlRemotoController"/>.
    /// </summary>
    /// <param name="registro">Registro de servicios conectados.</param>
    /// <param name="logger">Logger de la aplicacion.</param>
    public ControlRemotoController(
        RegistroServiciosConectados registro,
        ILogger<ControlRemotoController> logger)
    {
        _registro = registro;
        _logger = logger;
    }

    /// <summary>
    /// Muestra la vista principal de control remoto del rig.
    /// </summary>
    /// <returns>Vista de control remoto.</returns>
    public IActionResult Index()
    {
        string? usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        ViewData["UsuarioId"] = usuarioId;
        ViewData["ServicioConectado"] = _registro.EstaConectado(usuarioId);

        _logger.LogInformation("Usuario {UsuarioId} accede al control remoto del rig.", usuarioId);
        return View();
    }
}
