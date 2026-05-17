using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Web.Controllers;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Tests.Controllers;

public class ControlRemotoControllerTests
{
    private readonly RegistroServiciosConectados _registro;
    private readonly Mock<ILogger<ControlRemotoController>> _loggerMock;

    public ControlRemotoControllerTests()
    {
        _registro = new RegistroServiciosConectados();
        _loggerMock = new Mock<ILogger<ControlRemotoController>>();
    }

    private ControlRemotoController CrearControllerConUsuario(string usuarioId)
    {
        ControlRemotoController controller = new ControlRemotoController(_registro, _loggerMock.Object);

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId)
        };
        ClaimsIdentity identidad = new ClaimsIdentity(claims, "TestAuth");
        ClaimsPrincipal principal = new ClaimsPrincipal(identidad);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    [Fact]
    public void Index_UsuarioAutenticado_RetornaVistaConUsuarioId()
    {
        // Arrange
        string usuarioId = "usuario-123";
        ControlRemotoController controller = CrearControllerConUsuario(usuarioId);

        // Act
        IActionResult resultado = controller.Index();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["UsuarioId"].Should().Be(usuarioId);
    }

    [Fact]
    public void Index_ServicioConectado_ViewDataServicioConectadoTrue()
    {
        // Arrange
        string usuarioId = "usuario-456";
        _registro.Registrar(usuarioId, "conexion-abc");
        ControlRemotoController controller = CrearControllerConUsuario(usuarioId);

        // Act
        IActionResult resultado = controller.Index();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["ServicioConectado"].Should().Be(true);
    }

    [Fact]
    public void Index_ServicioNoConectado_ViewDataServicioConectadoFalse()
    {
        // Arrange
        string usuarioId = "usuario-789";
        ControlRemotoController controller = CrearControllerConUsuario(usuarioId);

        // Act
        IActionResult resultado = controller.Index();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["ServicioConectado"].Should().Be(false);
    }
}
