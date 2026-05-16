using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Servicio.Controllers;

namespace RadioAficionado.Servicio.Tests.Controllers;

/// <summary>
/// Tests unitarios para OperacionController.
/// Verifica que cada accion retorna ViewResult y establece ViewData["Seccion"] correctamente.
/// </summary>
public sealed class OperacionControllerTests
{
    private readonly OperacionController _controlador = new();

    [Fact]
    public void Index_SinParametros_RetornaViewResultConSeccionOperacion()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Index();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["Seccion"].Should().Be("Operacion");
    }

    [Fact]
    public void Logbook_SinParametros_RetornaViewResultConSeccionLogbook()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Logbook();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["Seccion"].Should().Be("Logbook");
    }

    [Fact]
    public void DxCluster_SinParametros_RetornaViewResultConSeccionDxCluster()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.DxCluster();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["Seccion"].Should().Be("DxCluster");
    }

    [Fact]
    public void Dxcc_SinParametros_RetornaViewResultConSeccionDxcc()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Dxcc();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["Seccion"].Should().Be("Dxcc");
    }

    [Fact]
    public void Propagacion_SinParametros_RetornaViewResultConSeccionPropagacion()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Propagacion();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["Seccion"].Should().Be("Propagacion");
    }

    [Fact]
    public void Activaciones_SinParametros_RetornaViewResultConSeccionActivaciones()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Activaciones();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["Seccion"].Should().Be("Activaciones");
    }

    [Fact]
    public void Contest_SinParametros_RetornaViewResultConSeccionContest()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Contest();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["Seccion"].Should().Be("Contest");
    }

    [Fact]
    public void Satelites_SinParametros_RetornaViewResultConSeccionSatelites()
    {
        // Arrange — controlador ya instanciado

        // Act
        IActionResult resultado = _controlador.Satelites();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewData["Seccion"].Should().Be("Satelites");
    }
}
