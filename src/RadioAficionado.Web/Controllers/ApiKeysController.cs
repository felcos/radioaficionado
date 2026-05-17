using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para la gestion de claves de API del usuario.
/// </summary>
[Authorize]
public class ApiKeysController : Controller
{
    private readonly IServicioApiKeys _servicioApiKeys;
    private readonly ILogger<ApiKeysController> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ApiKeysController"/>.
    /// </summary>
    /// <param name="servicioApiKeys">Servicio de gestion de claves de API.</param>
    /// <param name="logger">Logger de la aplicacion.</param>
    public ApiKeysController(
        IServicioApiKeys servicioApiKeys,
        ILogger<ApiKeysController> logger)
    {
        _servicioApiKeys = servicioApiKeys;
        _logger = logger;
    }

    /// <summary>
    /// Muestra la lista de claves de API del usuario.
    /// </summary>
    /// <returns>Vista con la lista de claves.</returns>
    public async Task<IActionResult> Index()
    {
        string usuarioId = ObtenerUsuarioId();
        IReadOnlyList<RadioAficionado.Dominio.Entidades.ClaveApi> claves =
            await _servicioApiKeys.ObtenerClavesUsuarioAsync(usuarioId);

        return View(claves);
    }

    /// <summary>
    /// Genera una nueva clave de API para el usuario.
    /// </summary>
    /// <param name="nombre">Nombre descriptivo de la clave.</param>
    /// <returns>Redireccion a Index con la clave generada en TempData.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            TempData["Error"] = "El nombre de la clave no puede estar vacio.";
            return RedirectToAction(nameof(Index));
        }

        string usuarioId = ObtenerUsuarioId();

        (string claveTextoPlano, RadioAficionado.Dominio.Entidades.ClaveApi _) =
            await _servicioApiKeys.GenerarClaveAsync(usuarioId, nombre);

        _logger.LogInformation(
            "Clave de API generada para el usuario {UsuarioId} con nombre '{Nombre}'.",
            usuarioId,
            nombre);

        TempData["ClaveGenerada"] = claveTextoPlano;
        TempData["NombreClave"] = nombre;

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Desactiva una clave de API del usuario.
    /// </summary>
    /// <param name="id">Identificador de la clave a desactivar.</param>
    /// <returns>Redireccion a Index.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        string usuarioId = ObtenerUsuarioId();

        await _servicioApiKeys.DesactivarClaveAsync(id, usuarioId);

        _logger.LogInformation(
            "Clave de API {ClaveId} desactivada por el usuario {UsuarioId}.",
            id,
            usuarioId);

        TempData["Exito"] = "Clave desactivada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private string ObtenerUsuarioId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("No se pudo obtener el identificador del usuario.");
    }
}
