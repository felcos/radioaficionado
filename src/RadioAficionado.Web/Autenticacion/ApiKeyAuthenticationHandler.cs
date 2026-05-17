using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Autenticacion;

/// <summary>
/// Handler de autenticacion personalizado que valida claves de API
/// enviadas en el header "X-Api-Key". Utilizado por el servicio local
/// (RadioAficionado.Servicio) para autenticarse contra los hubs de SignalR.
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> opciones,
    ILoggerFactory fabricaLogger,
    UrlEncoder codificadorUrl,
    IServicioApiKeys servicioApiKeys)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, fabricaLogger, codificadorUrl)
{
    /// <summary>
    /// Nombre del esquema de autenticacion por clave de API.
    /// </summary>
    public const string NombreEsquema = "ApiKey";

    private const string NombreHeader = "X-Api-Key";

    /// <summary>
    /// Intenta autenticar la peticion usando la clave de API del header.
    /// </summary>
    /// <returns>Resultado de la autenticacion.</returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(NombreHeader, out Microsoft.Extensions.Primitives.StringValues valorHeader))
        {
            return AuthenticateResult.NoResult();
        }

        string? claveTextoPlano = valorHeader.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(claveTextoPlano))
        {
            return AuthenticateResult.NoResult();
        }

        ClaveApi? claveApi = await servicioApiKeys.ValidarClaveAsync(claveTextoPlano);

        if (claveApi is null)
        {
            Logger.LogWarning("Clave de API invalida recibida");
            return AuthenticateResult.Fail("Clave de API invalida o expirada.");
        }

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, claveApi.UsuarioId),
            new Claim(ClaimTypes.Name, claveApi.Usuario?.Indicativo ?? string.Empty),
            new Claim("ApiKeyId", claveApi.Id.ToString()),
            new Claim("ApiKeyPrefijo", claveApi.Prefijo)
        ];

        ClaimsIdentity identidad = new(claims, NombreEsquema);
        ClaimsPrincipal principal = new(identidad);
        AuthenticationTicket ticket = new(principal, NombreEsquema);

        return AuthenticateResult.Success(ticket);
    }
}
