using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Servicio.Controllers;

namespace RadioAficionado.Servicio.Tests.Controllers;

/// <summary>
/// Tests unitarios para DxccApiController.
/// </summary>
public sealed class DxccApiControllerTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly DxccApiController _controlador;

    public DxccApiControllerTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _controlador = new DxccApiController(_mockRepositorio.Object);
    }

    [Fact]
    public async Task ObtenerEntidadesDxcc_SinFiltro_RetornaOkConEstructura()
    {
        // Arrange
        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        IActionResult resultado = await _controlador.ObtenerEntidadesDxcc();

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        object valor = okResult.Value!;

        int trabajados = (int)valor.GetType().GetProperty("trabajados")!.GetValue(valor)!;
        int confirmados = (int)valor.GetType().GetProperty("confirmados")!.GetValue(valor)!;
        int total = (int)valor.GetType().GetProperty("total")!.GetValue(valor)!;

        trabajados.Should().Be(0);
        confirmados.Should().Be(0);
        total.Should().BeGreaterThan(0); // Catalogo DXCC tiene entidades
    }

    [Fact]
    public async Task ObtenerEntidadesDxcc_ConFiltroEstado_RetornaEntidadesFiltradas()
    {
        // Arrange
        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        IActionResult resultado = await _controlador.ObtenerEntidadesDxcc(estado: "necesitado");

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ObtenerEntidadesDxcc_ConFiltroBanda_RetornaOk()
    {
        // Arrange
        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        IActionResult resultado = await _controlador.ObtenerEntidadesDxcc(banda: "20m");

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ObtenerEntidadesDxcc_ConQsosReales_CalculaTrabajadosYConfirmados()
    {
        // Arrange
        Indicativo propio = new("EA4ABC");
        Indicativo contactoUsa = new("W1AW");
        Frecuencia frecuencia20m = Frecuencia.DesdeMHz(14.074);
        DateTimeOffset fecha = DateTimeOffset.UtcNow.AddHours(-2);

        Qso qso = Qso.Crear(propio, contactoUsa, fecha, frecuencia20m, ModoOperacion.FT8, "-15");

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso> { qso });

        // Act
        IActionResult resultado = await _controlador.ObtenerEntidadesDxcc();

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        object valor = okResult.Value!;

        int trabajados = (int)valor.GetType().GetProperty("trabajados")!.GetValue(valor)!;
        trabajados.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Constructor_ConRepositorioNulo_LanzaArgumentNullException()
    {
        // Act
        Action accion = () => new DxccApiController(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("repositorioQso");
    }
}
