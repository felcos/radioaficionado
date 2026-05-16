using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Propagacion;
using RadioAficionado.Servicio.Controllers;

namespace RadioAficionado.Servicio.Tests.Controllers;

/// <summary>
/// Tests unitarios para PropagacionApiController.
/// </summary>
public sealed class PropagacionApiControllerTests
{
    private readonly Mock<IServicioPropagacion> _mockServicio;
    private readonly PropagacionApiController _controlador;

    public PropagacionApiControllerTests()
    {
        _mockServicio = new Mock<IServicioPropagacion>();
        _controlador = new PropagacionApiController(_mockServicio.Object);
    }

    [Fact]
    public async Task ObtenerPropagacion_ConIndicesReales_RetornaOkConDatos()
    {
        // Arrange
        IndicesSolares indices = new(
            Sfi: 165,
            NumeroManchasSolares: 128,
            Ap: 8,
            Kp: 2,
            FechaActualizacion: DateTime.UtcNow);

        _mockServicio
            .Setup(s => s.ObtenerIndicesSolaresAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(indices);

        // Act
        IActionResult resultado = await _controlador.ObtenerPropagacion();

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        object valor = okResult.Value!;
        int sfi = (int)valor.GetType().GetProperty("sfi")!.GetValue(valor)!;
        sfi.Should().Be(165);
    }

    [Fact]
    public async Task ObtenerPropagacion_ServicioFalla_RetornaOkConDatosEjemplo()
    {
        // Arrange
        _mockServicio
            .Setup(s => s.ObtenerIndicesSolaresAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Sin conexion"));

        // Act
        IActionResult resultado = await _controlador.ObtenerPropagacion();

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        // Debe devolver datos de ejemplo
        object valor = okResult.Value!;
        int sfi = (int)valor.GetType().GetProperty("sfi")!.GetValue(valor)!;
        sfi.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ObtenerPropagacion_ConIndicesReales_IncluyeBandas()
    {
        // Arrange
        IndicesSolares indices = new(
            Sfi: 100,
            NumeroManchasSolares: 80,
            Ap: 15,
            Kp: 3,
            FechaActualizacion: DateTime.UtcNow);

        _mockServicio
            .Setup(s => s.ObtenerIndicesSolaresAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(indices);

        // Act
        IActionResult resultado = await _controlador.ObtenerPropagacion();

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        object valor = okResult.Value!;

        object? bandas = valor.GetType().GetProperty("bandas")!.GetValue(valor);
        bandas.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ConServicioNulo_LanzaArgumentNullException()
    {
        // Act
        Action accion = () => new PropagacionApiController(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("servicioPropagacion");
    }
}
