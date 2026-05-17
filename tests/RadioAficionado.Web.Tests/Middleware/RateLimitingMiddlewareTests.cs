using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Web.Middleware;

namespace RadioAficionado.Web.Tests.Middleware;

public class RateLimitingMiddlewareTests : IDisposable
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock;
    private bool _siguienteFueInvocado;
    private readonly RequestDelegate _siguiente;

    public RateLimitingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<RateLimitingMiddleware>>();
        _siguienteFueInvocado = false;
        _siguiente = (HttpContext _) =>
        {
            _siguienteFueInvocado = true;
            return Task.CompletedTask;
        };
    }

    public void Dispose()
    {
        // El middleware implementa IDisposable por el Timer interno
        GC.SuppressFinalize(this);
    }

    private DefaultHttpContext CrearHttpContext(string ruta, string? nombreUsuario = null)
    {
        DefaultHttpContext contexto = new DefaultHttpContext();
        contexto.Request.Path = ruta;

        if (nombreUsuario is not null)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nombreUsuario)
            };
            ClaimsIdentity identidad = new ClaimsIdentity(claims, "TestAuth");
            contexto.User = new ClaimsPrincipal(identidad);
        }

        return contexto;
    }

    [Fact]
    public async Task InvokeAsync_RutaNoHubs_PasaDirecto()
    {
        // Arrange
        RateLimitingMiddleware middleware = new RateLimitingMiddleware(_siguiente, _loggerMock.Object);
        DefaultHttpContext contexto = CrearHttpContext("/api/qso");

        // Act
        await middleware.InvokeAsync(contexto);

        // Assert
        _siguienteFueInvocado.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_RutaHubs_DentroDelLimite_PasaDirecto()
    {
        // Arrange
        RateLimitingMiddleware middleware = new RateLimitingMiddleware(_siguiente, _loggerMock.Object);
        DefaultHttpContext contexto = CrearHttpContext("/hubs/rig", "usuario-test");

        // Act
        await middleware.InvokeAsync(contexto);

        // Assert
        _siguienteFueInvocado.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_RutaHubs_ExcedeLimite_Retorna429()
    {
        // Arrange
        RateLimitingMiddleware middleware = new RateLimitingMiddleware(_siguiente, _loggerMock.Object);
        string nombreUsuario = "usuario-rate-limit";

        for (int i = 0; i < 20; i++)
        {
            DefaultHttpContext contextoPermitido = CrearHttpContext("/hubs/rig", nombreUsuario);
            await middleware.InvokeAsync(contextoPermitido);
        }

        DefaultHttpContext contextoExcedido = CrearHttpContext("/hubs/rig", nombreUsuario);

        // Act
        await middleware.InvokeAsync(contextoExcedido);

        // Assert
        contextoExcedido.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task InvokeAsync_RutaHubs_ExcedeLimite_TieneHeaderRetryAfter()
    {
        // Arrange
        RateLimitingMiddleware middleware = new RateLimitingMiddleware(_siguiente, _loggerMock.Object);
        string nombreUsuario = "usuario-retry-after";

        for (int i = 0; i < 20; i++)
        {
            DefaultHttpContext contextoPermitido = CrearHttpContext("/hubs/rig", nombreUsuario);
            await middleware.InvokeAsync(contextoPermitido);
        }

        DefaultHttpContext contextoExcedido = CrearHttpContext("/hubs/rig", nombreUsuario);

        // Act
        await middleware.InvokeAsync(contextoExcedido);

        // Assert
        contextoExcedido.Response.Headers["Retry-After"].ToString().Should().Be("1");
    }
}
