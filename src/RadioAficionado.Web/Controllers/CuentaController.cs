using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Controllers;

/// <summary>
/// Controlador para la gestión de cuentas de usuario: registro, inicio de sesión, perfil y edición.
/// </summary>
public class CuentaController(
    UserManager<UsuarioRadio> userManager,
    SignInManager<UsuarioRadio> signInManager,
    ILogger<CuentaController> logger) : Controller
{
    private readonly UserManager<UsuarioRadio> _userManager = userManager;
    private readonly SignInManager<UsuarioRadio> _signInManager = signInManager;
    private readonly ILogger<CuentaController> _logger = logger;

    /// <summary>
    /// Muestra el formulario de registro de usuario.
    /// </summary>
    /// <returns>Vista del formulario de registro.</returns>
    [HttpGet]
    public IActionResult Registrar()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Perfil));
        }

        return View();
    }

    /// <summary>
    /// Procesa el formulario de registro de usuario.
    /// </summary>
    /// <param name="modelo">Datos del formulario de registro.</param>
    /// <returns>Redirección al inicio si el registro es exitoso, o la vista con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistrarViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        UsuarioRadio usuario = new()
        {
            UserName = modelo.Indicativo.ToUpperInvariant(),
            Email = modelo.Email,
            Indicativo = modelo.Indicativo.ToUpperInvariant(),
            Nombre = modelo.Nombre,
            Localizador = modelo.Localizador?.ToUpperInvariant(),
            RegionItu = modelo.RegionItu,
            FechaRegistro = DateTime.UtcNow
        };

        IdentityResult resultado = await _userManager.CreateAsync(usuario, modelo.Contrasena);

        if (resultado.Succeeded)
        {
            _logger.LogInformation("Usuario registrado exitosamente: {Indicativo}", usuario.Indicativo);
            await _signInManager.SignInAsync(usuario, isPersistent: false);
            return RedirectToAction("Index", "Inicio");
        }

        foreach (IdentityError error in resultado.Errors)
        {
            ModelState.AddModelError(string.Empty, TraducirErrorIdentity(error));
        }

        return View(modelo);
    }

    /// <summary>
    /// Muestra el formulario de inicio de sesión.
    /// </summary>
    /// <param name="urlRetorno">URL a la que redirigir tras el login exitoso.</param>
    /// <returns>Vista del formulario de inicio de sesión.</returns>
    [HttpGet]
    public IActionResult IniciarSesion(string? urlRetorno = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Perfil));
        }

        ViewData["UrlRetorno"] = urlRetorno;
        return View();
    }

    /// <summary>
    /// Procesa el formulario de inicio de sesión.
    /// </summary>
    /// <param name="modelo">Datos del formulario de login.</param>
    /// <param name="urlRetorno">URL a la que redirigir tras el login exitoso.</param>
    /// <returns>Redirección al inicio o URL de retorno si el login es exitoso, o la vista con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarSesion(IniciarSesionViewModel modelo, string? urlRetorno = null)
    {
        ViewData["UrlRetorno"] = urlRetorno;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        UsuarioRadio? usuario = await _userManager.FindByEmailAsync(modelo.Email);

        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Correo electrónico o contraseña incorrectos.");
            return View(modelo);
        }

        Microsoft.AspNetCore.Identity.SignInResult resultado = await _signInManager.PasswordSignInAsync(
            usuario,
            modelo.Contrasena,
            modelo.Recordarme,
            lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            _logger.LogInformation("Inicio de sesión exitoso: {Indicativo}", usuario.Indicativo);

            if (!string.IsNullOrWhiteSpace(urlRetorno) && Url.IsLocalUrl(urlRetorno))
            {
                return Redirect(urlRetorno);
            }

            return RedirectToAction("Index", "Inicio");
        }

        if (resultado.IsLockedOut)
        {
            _logger.LogWarning("Cuenta bloqueada por intentos fallidos: {Email}", modelo.Email);
            ModelState.AddModelError(string.Empty, "La cuenta ha sido bloqueada temporalmente por demasiados intentos fallidos. Intenta de nuevo en unos minutos.");
            return View(modelo);
        }

        ModelState.AddModelError(string.Empty, "Correo electrónico o contraseña incorrectos.");
        return View(modelo);
    }

    /// <summary>
    /// Cierra la sesión del usuario actual.
    /// </summary>
    /// <returns>Redirección a la página de inicio.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> CerrarSesion()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Usuario cerró sesión");
        return RedirectToAction("Index", "Inicio");
    }

    /// <summary>
    /// Muestra el perfil del usuario autenticado.
    /// </summary>
    /// <returns>Vista del perfil del usuario.</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Perfil()
    {
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null)
        {
            return RedirectToAction(nameof(IniciarSesion));
        }

        PerfilViewModel modelo = new()
        {
            Indicativo = usuario.Indicativo,
            Nombre = usuario.Nombre,
            Email = usuario.Email ?? string.Empty,
            Localizador = usuario.Localizador,
            Biografia = usuario.Biografia,
            RegionItu = usuario.RegionItu,
            FechaRegistro = usuario.FechaRegistro
        };

        return View(modelo);
    }

    /// <summary>
    /// Muestra el formulario de edición de perfil.
    /// </summary>
    /// <returns>Vista del formulario de edición.</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditarPerfil()
    {
        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null)
        {
            return RedirectToAction(nameof(IniciarSesion));
        }

        EditarPerfilViewModel modelo = new()
        {
            Indicativo = usuario.Indicativo,
            Nombre = usuario.Nombre,
            Localizador = usuario.Localizador,
            Biografia = usuario.Biografia,
            RegionItu = usuario.RegionItu
        };

        return View(modelo);
    }

    /// <summary>
    /// Procesa el formulario de edición de perfil.
    /// </summary>
    /// <param name="modelo">Datos del formulario de edición.</param>
    /// <returns>Redirección al perfil si la edición es exitosa, o la vista con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> EditarPerfil(EditarPerfilViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        UsuarioRadio? usuario = await _userManager.GetUserAsync(User);

        if (usuario is null)
        {
            return RedirectToAction(nameof(IniciarSesion));
        }

        usuario.Indicativo = modelo.Indicativo.ToUpperInvariant();
        usuario.UserName = modelo.Indicativo.ToUpperInvariant();
        usuario.Nombre = modelo.Nombre;
        usuario.Localizador = modelo.Localizador?.ToUpperInvariant();
        usuario.Biografia = modelo.Biografia;
        usuario.RegionItu = modelo.RegionItu;

        IdentityResult resultado = await _userManager.UpdateAsync(usuario);

        if (resultado.Succeeded)
        {
            _logger.LogInformation("Perfil actualizado: {Indicativo}", usuario.Indicativo);
            TempData["MensajeExito"] = "Perfil actualizado correctamente.";
            return RedirectToAction(nameof(Perfil));
        }

        foreach (IdentityError error in resultado.Errors)
        {
            ModelState.AddModelError(string.Empty, TraducirErrorIdentity(error));
        }

        return View(modelo);
    }

    /// <summary>
    /// Traduce los mensajes de error de ASP.NET Identity al español.
    /// </summary>
    /// <param name="error">Error de Identity a traducir.</param>
    /// <returns>Mensaje de error traducido al español.</returns>
    private static string TraducirErrorIdentity(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateUserName" => "Ya existe un usuario con ese indicativo.",
            "DuplicateEmail" => "Ya existe un usuario con ese correo electrónico.",
            "InvalidEmail" => "El correo electrónico no es válido.",
            "PasswordTooShort" => "La contraseña es demasiado corta (mínimo 8 caracteres).",
            "PasswordRequiresDigit" => "La contraseña debe contener al menos un número.",
            "PasswordRequiresLower" => "La contraseña debe contener al menos una letra minúscula.",
            "PasswordRequiresUpper" => "La contraseña debe contener al menos una letra mayúscula.",
            "PasswordRequiresNonAlphanumeric" => "La contraseña debe contener al menos un carácter especial.",
            "PasswordRequiresUniqueChars" => "La contraseña debe contener caracteres más variados.",
            "InvalidUserName" => "El indicativo contiene caracteres no permitidos.",
            _ => error.Description
        };
    }
}
