using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Web.Controllers;

namespace RadioAficionado.Web.Tests.Controllers;

/// <summary>
/// Tests unitarios para <see cref="InicioController"/>.
/// El controlador ahora sirve la landing page publica sin dependencias de BD.
/// </summary>
public class InicioControllerTests
{
    private readonly InicioController _controlador;

    public InicioControllerTests()
    {
        Mock<ILogger<InicioController>> mockLogger = new();
        _controlador = new InicioController(mockLogger.Object);
    }

    [Fact]
    public void Index_SinParametros_RetornaViewResult()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Index();

        // Assert
        resultado.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Privacy_SinParametros_RetornaViewResult()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Privacy();

        // Assert
        resultado.Should().BeOfType<ViewResult>();
    }
}
