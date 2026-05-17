using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Tests.Servicios;

public class ControladorTimeoutPttTests : IDisposable
{
    private readonly Mock<IServiceProvider> _mockProveedor;
    private readonly ILogger<ControladorTimeoutPtt> _logger;
    private readonly ControladorTimeoutPtt _controlador;

    public ControladorTimeoutPttTests()
    {
        _mockProveedor = new Mock<IServiceProvider>();
        _logger = NullLogger<ControladorTimeoutPtt>.Instance;
        _controlador = new ControladorTimeoutPtt(_mockProveedor.Object, _logger);
    }

    [Fact]
    public void RegistrarPttActivo_UsuarioValido_IncrementaCantidad()
    {
        // Arrange
        string usuarioId = "usuario-001";

        // Act
        _controlador.RegistrarPttActivo(usuarioId);

        // Assert
        _controlador.CantidadPttActivos.Should().Be(1);
    }

    [Fact]
    public void RegistrarPttInactivo_UsuarioRegistrado_DecrementaCantidad()
    {
        // Arrange
        string usuarioId = "usuario-002";
        _controlador.RegistrarPttActivo(usuarioId);

        // Act
        _controlador.RegistrarPttInactivo(usuarioId);

        // Assert
        _controlador.CantidadPttActivos.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegistrarPttActivo_UsuarioVacioONull_NoRegistra(string? usuarioId)
    {
        // Arrange — estado inicial sin registros

        // Act
        _controlador.RegistrarPttActivo(usuarioId!);

        // Assert
        _controlador.CantidadPttActivos.Should().Be(0);
    }

    [Fact]
    public void RegistrarPttInactivo_UsuarioNoRegistrado_NoHaceNada()
    {
        // Arrange
        _controlador.RegistrarPttActivo("usuario-existente");
        string usuarioNoRegistrado = "usuario-inexistente";

        // Act
        _controlador.RegistrarPttInactivo(usuarioNoRegistrado);

        // Assert
        _controlador.CantidadPttActivos.Should().Be(1);
    }

    [Fact]
    public void CantidadPttActivos_MultiplesUsuarios_RetornaCantidadCorrecta()
    {
        // Arrange & Act
        _controlador.RegistrarPttActivo("usuario-a");
        _controlador.RegistrarPttActivo("usuario-b");
        _controlador.RegistrarPttActivo("usuario-c");

        // Assert
        _controlador.CantidadPttActivos.Should().Be(3);
    }

    public void Dispose()
    {
        _controlador.Dispose();
    }
}
