using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Servicio.Dtos;
using RadioAficionado.Servicio.Hubs;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Tests.Servicios;

/// <summary>
/// Tests unitarios para el parser de spots DX del <see cref="ClienteDxClusterTelnet"/>.
/// Verifica que el parseo de lineas Telnet produce SpotDxDto correctos.
/// </summary>
public sealed class ClienteDxClusterTelnetTests
{
    private readonly ClienteDxClusterTelnet _cliente;

    public ClienteDxClusterTelnetTests()
    {
        Mock<IHubContext<HubEstado, IClienteHubEstado>> mockHub = new();
        Mock<IConfiguration> mockConfig = new();
        Mock<IConfigurationSection> mockSeccion = new();
        mockConfig.Setup(c => c.GetSection("DxCluster")).Returns(mockSeccion.Object);
        Mock<ILogger<ClienteDxClusterTelnet>> mockLogger = new();

        _cliente = new ClienteDxClusterTelnet(
            mockHub.Object,
            mockConfig.Object,
            mockLogger.Object);
    }

    [Fact]
    public void ParsearLineaSpot_FormatoEstandar_ExtraeCorrectamente()
    {
        // Arrange
        string linea = "DX de EA4ABC:  14076.0 JA1XYZ    FT8 -15dB          1845Z";

        // Act
        SpotDxDto? resultado = _cliente.ParsearLineaSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Spotteador.Should().Be("EA4ABC");
        resultado.Dx.Should().Be("JA1XYZ");
        resultado.FrecuenciaHz.Should().Be(14_076_000);
        resultado.Comentario.Should().Be("FT8 -15dB");
    }

    [Fact]
    public void ParsearLineaSpot_Banda40m_FrecuenciaConvertidaCorrectamente()
    {
        // Arrange
        string linea = "DX de W1AW:   7074.0 DL1ABC    FT8 +03dB 1523Hz    2130Z";

        // Act
        SpotDxDto? resultado = _cliente.ParsearLineaSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Spotteador.Should().Be("W1AW");
        resultado.Dx.Should().Be("DL1ABC");
        resultado.FrecuenciaHz.Should().Be(7_074_000);
    }

    [Fact]
    public void ParsearLineaSpot_FrecuenciaConDecimales_ConvierteAHz()
    {
        // Arrange
        string linea = "DX de K3LR:   14074.5 PY2ABC    CW 599              1200Z";

        // Act
        SpotDxDto? resultado = _cliente.ParsearLineaSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.FrecuenciaHz.Should().Be(14_074_500);
    }

    [Fact]
    public void ParsearLineaSpot_IndicativosEnMayusculas_SiempreUpperCase()
    {
        // Arrange
        string linea = "DX de ea4abc:  14076.0 ja1xyz    FT8 -15dB          1845Z";

        // Act
        SpotDxDto? resultado = _cliente.ParsearLineaSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Spotteador.Should().Be("EA4ABC");
        resultado.Dx.Should().Be("JA1XYZ");
    }

    [Fact]
    public void ParsearLineaSpot_HoraUtc_ParseaHorasYMinutos()
    {
        // Arrange
        string linea = "DX de EA4ABC:  14076.0 JA1XYZ    FT8 -15dB          0930Z";

        // Act
        SpotDxDto? resultado = _cliente.ParsearLineaSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.HoraUtc.Hour.Should().Be(9);
        resultado.HoraUtc.Minute.Should().Be(30);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bienvenido al cluster DX")]
    [InlineData("W1AW de EA4ABC: hola que tal")]
    [InlineData("*** Connected to DX Spider")]
    public void ParsearLineaSpot_LineaNoSpot_RetornaNull(string linea)
    {
        // Act
        SpotDxDto? resultado = _cliente.ParsearLineaSpot(linea);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ParsearLineaSpot_ConComentarioLargo_ExtraeComentarioCompleto()
    {
        // Arrange
        string linea = "DX de EA1AA:  28074.0 VK2ABC    FT8 -08dB CQ DX     1500Z";

        // Act
        SpotDxDto? resultado = _cliente.ParsearLineaSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Comentario.Should().Contain("FT8");
        resultado.Comentario.Should().Contain("CQ DX");
    }
}
