using FluentAssertions;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el registro central de decodificadores digitales.
/// </summary>
public class RegistroDecodificadoresTests
{
    private Mock<IDecodificadorDigital> CrearMockDecodificador(ModoOperacion modo)
    {
        Mock<IDecodificadorDigital> mock = new();
        mock.Setup(d => d.Modo).Returns(modo);
        return mock;
    }

    [Fact]
    public void ObtenerTodos_SinDecodificadores_DevuelveVacio()
    {
        // Arrange
        RegistroDecodificadores registro = new();

        // Act
        IReadOnlyList<IDecodificadorDigital> resultado = registro.ObtenerTodos();

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public void Registrar_UnDecodificador_ObtenerTodosLoDevuelve()
    {
        // Arrange
        RegistroDecodificadores registro = new();
        Mock<IDecodificadorDigital> mockFt8 = CrearMockDecodificador(ModoOperacion.FT8);

        // Act
        registro.Registrar(mockFt8.Object);
        IReadOnlyList<IDecodificadorDigital> resultado = registro.ObtenerTodos();

        // Assert
        resultado.Should().HaveCount(1);
        resultado[0].Modo.Should().Be(ModoOperacion.FT8);
    }

    [Fact]
    public void Registrar_MultiplesDecodificadores_ObtenerTodosDevuelveTodos()
    {
        // Arrange
        RegistroDecodificadores registro = new();
        Mock<IDecodificadorDigital> mockFt8 = CrearMockDecodificador(ModoOperacion.FT8);
        Mock<IDecodificadorDigital> mockRtty = CrearMockDecodificador(ModoOperacion.RTTY);
        Mock<IDecodificadorDigital> mockPsk = CrearMockDecodificador(ModoOperacion.PSK);

        // Act
        registro.Registrar(mockFt8.Object);
        registro.Registrar(mockRtty.Object);
        registro.Registrar(mockPsk.Object);
        IReadOnlyList<IDecodificadorDigital> resultado = registro.ObtenerTodos();

        // Assert
        resultado.Should().HaveCount(3);
    }

    [Fact]
    public void Registrar_DecodificadorNull_LanzaExcepcion()
    {
        // Arrange
        RegistroDecodificadores registro = new();

        // Act
        Action accion = () => registro.Registrar(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ObtenerPorModo_ModoExistente_DevuelveDecodificador()
    {
        // Arrange
        RegistroDecodificadores registro = new();
        Mock<IDecodificadorDigital> mockRtty = CrearMockDecodificador(ModoOperacion.RTTY);
        registro.Registrar(mockRtty.Object);

        // Act
        IDecodificadorDigital? resultado = registro.ObtenerPorModo(ModoOperacion.RTTY);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Modo.Should().Be(ModoOperacion.RTTY);
    }

    [Fact]
    public void ObtenerPorModo_ModoInexistente_DevuelveNull()
    {
        // Arrange
        RegistroDecodificadores registro = new();
        Mock<IDecodificadorDigital> mockFt8 = CrearMockDecodificador(ModoOperacion.FT8);
        registro.Registrar(mockFt8.Object);

        // Act
        IDecodificadorDigital? resultado = registro.ObtenerPorModo(ModoOperacion.CW);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ObtenerModosDisponibles_DevuelveModosSinDuplicados()
    {
        // Arrange
        RegistroDecodificadores registro = new();
        Mock<IDecodificadorDigital> mockFt8 = CrearMockDecodificador(ModoOperacion.FT8);
        Mock<IDecodificadorDigital> mockFt4 = CrearMockDecodificador(ModoOperacion.FT4);
        Mock<IDecodificadorDigital> mockRtty = CrearMockDecodificador(ModoOperacion.RTTY);
        Mock<IDecodificadorDigital> mockPsk = CrearMockDecodificador(ModoOperacion.PSK);

        registro.Registrar(mockFt8.Object);
        registro.Registrar(mockFt4.Object);
        registro.Registrar(mockRtty.Object);
        registro.Registrar(mockPsk.Object);

        // Act
        IReadOnlyList<ModoOperacion> modos = registro.ObtenerModosDisponibles();

        // Assert
        modos.Should().HaveCount(4);
        modos.Should().Contain(ModoOperacion.FT8);
        modos.Should().Contain(ModoOperacion.FT4);
        modos.Should().Contain(ModoOperacion.RTTY);
        modos.Should().Contain(ModoOperacion.PSK);
    }

    [Fact]
    public void ObtenerModosDisponibles_SinDecodificadores_DevuelveVacio()
    {
        // Arrange
        RegistroDecodificadores registro = new();

        // Act
        IReadOnlyList<ModoOperacion> modos = registro.ObtenerModosDisponibles();

        // Assert
        modos.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ConDecodificadoresIniciales_LosRegistra()
    {
        // Arrange
        Mock<IDecodificadorDigital> mockFt8 = CrearMockDecodificador(ModoOperacion.FT8);
        Mock<IDecodificadorDigital> mockRtty = CrearMockDecodificador(ModoOperacion.RTTY);
        List<IDecodificadorDigital> iniciales = new() { mockFt8.Object, mockRtty.Object };

        // Act
        RegistroDecodificadores registro = new(iniciales);
        IReadOnlyList<IDecodificadorDigital> resultado = registro.ObtenerTodos();

        // Assert
        resultado.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_ConDecodificadoresNull_LanzaExcepcion()
    {
        // Act
        Action accion = () => new RegistroDecodificadores(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ObtenerPorModo_MultiplesDecodificadoresMismoModo_DevuelvePrimero()
    {
        // Arrange
        RegistroDecodificadores registro = new();
        Mock<IDecodificadorDigital> mock1 = CrearMockDecodificador(ModoOperacion.FT8);
        Mock<IDecodificadorDigital> mock2 = CrearMockDecodificador(ModoOperacion.FT8);
        registro.Registrar(mock1.Object);
        registro.Registrar(mock2.Object);

        // Act
        IDecodificadorDigital? resultado = registro.ObtenerPorModo(ModoOperacion.FT8);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().BeSameAs(mock1.Object);
    }
}
