using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.DxCluster;

namespace RadioAficionado.Infraestructura.Tests.DxCluster;

/// <summary>
/// Tests unitarios para el parser de spots DX del <see cref="ClienteDxCluster"/>.
/// Se prueban exclusivamente las funciones de parseo sin necesidad de conexión TCP.
/// </summary>
public class ClienteDxClusterTests
{
    [Fact]
    public void ParsearSpot_FormatoEstandar_ExtraeCorrectamente()
    {
        // Arrange
        string linea = "DX de EA4ABC:  14076.0 JA1XYZ    FT8 -15dB          1845Z";

        // Act
        SpotDx? resultado = ClienteDxCluster.ParsearSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Spotteador.Valor.Should().Be("EA4ABC");
        resultado.Dx.Valor.Should().Be("JA1XYZ");
        resultado.Frecuencia.KHz.Should().BeApproximately(14076.0, 0.1);
        resultado.Comentario.Should().Be("FT8 -15dB");
        resultado.Hora.Hour.Should().Be(18);
        resultado.Hora.Minute.Should().Be(45);
    }

    [Fact]
    public void ParsearSpot_ConComentarioFT8_ExtraeModo()
    {
        // Arrange
        string linea = "DX de W1AW:   7074.0 DL1ABC    FT8 +03dB 1523Hz    2130Z";

        // Act
        SpotDx? resultado = ClienteDxCluster.ParsearSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Spotteador.Valor.Should().Be("W1AW");
        resultado.Dx.Valor.Should().Be("DL1ABC");
        resultado.Frecuencia.KHz.Should().BeApproximately(7074.0, 0.1);
        resultado.Comentario.Should().Contain("FT8");
        resultado.Hora.Hour.Should().Be(21);
        resultado.Hora.Minute.Should().Be(30);
    }

    [Fact]
    public void ParsearSpot_SinComentario_ComentarioVacio()
    {
        // Arrange
        string linea = "DX de VK2ABC:  21074.0 ZL1XYZ                        0730Z";

        // Act
        SpotDx? resultado = ClienteDxCluster.ParsearSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Spotteador.Valor.Should().Be("VK2ABC");
        resultado.Dx.Valor.Should().Be("ZL1XYZ");
        resultado.Frecuencia.KHz.Should().BeApproximately(21074.0, 0.1);
        resultado.Comentario.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bienvenido al cluster DX")]
    [InlineData("W1AW de EA4ABC: hola que tal")]
    [InlineData("DX de : 14076.0 JA1XYZ  comentario  1845Z")]
    [InlineData(null)]
    public void ParsearSpot_LineaInvalida_RetornaNull(string? linea)
    {
        // Act
        SpotDx? resultado = ClienteDxCluster.ParsearSpot(linea!);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ParsearSpot_FrecuenciaDecimal_ParseaCorrecto()
    {
        // Arrange
        string linea = "DX de K3LR:   14074.5 PY2ABC    CW 599              1200Z";

        // Act
        SpotDx? resultado = ClienteDxCluster.ParsearSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Frecuencia.KHz.Should().BeApproximately(14074.5, 0.1);
        resultado.Frecuencia.Hz.Should().Be(14_074_500);
    }

    [Fact]
    public void ParsearSpot_FrecuenciaSinDecimales_ParseaCorrecto()
    {
        // Arrange
        string linea = "DX de EA1AAA:  28000 LU1ABC     SSB 59              0915Z";

        // Act
        SpotDx? resultado = ClienteDxCluster.ParsearSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Frecuencia.KHz.Should().BeApproximately(28000.0, 0.1);
    }

    [Fact]
    public void ParsearSpot_IndicativoConBarra_ParseaCorrecto()
    {
        // Arrange
        string linea = "DX de EA4ABC/P:  14076.0 VK2ABC/M    FT8             1845Z";

        // Act
        SpotDx? resultado = ClienteDxCluster.ParsearSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Spotteador.Valor.Should().Be("EA4ABC/P");
        resultado.Dx.Valor.Should().Be("VK2ABC/M");
    }

    [Fact]
    public void ParsearSpot_BandaDe40Metros_FrecuenciaCorrecta()
    {
        // Arrange
        string linea = "DX de G3ABC:   7025.0 UA3XYZ    CW 559 QSB          2200Z";

        // Act
        SpotDx? resultado = ClienteDxCluster.ParsearSpot(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Frecuencia.KHz.Should().BeApproximately(7025.0, 0.1);
        BandaRadio? banda = resultado.Frecuencia.ObtenerBanda();
        banda.Should().Be(BandaRadio.Banda40m);
    }

    [Fact]
    public void ConfiguracionDxCluster_ServidoresConocidos_NoEstaVacia()
    {
        // Act
        IReadOnlyList<(string Servidor, int Puerto, string Descripcion)> servidores = ConfiguracionDxCluster.ServidoresConocidos;

        // Assert
        servidores.Should().NotBeEmpty();
        servidores.Should().HaveCountGreaterThan(3);
    }

    [Fact]
    public void ConfiguracionDxCluster_ValoresPorDefecto_SonCorrectos()
    {
        // Arrange
        ConfiguracionDxCluster config = new();

        // Assert
        config.Servidor.Should().Be("dxc.ve7cc.net");
        config.Puerto.Should().Be(7300);
        config.IndicativoPropio.Should().BeEmpty();
        config.TimeoutMs.Should().Be(10_000);
        config.RetrasoReconexionMs.Should().Be(5_000);
        config.MaxIntentosReconexion.Should().Be(5);
    }

    [Fact]
    public void ConfiguracionDxCluster_ServidoresConocidos_TienenPuertosValidos()
    {
        // Act & Assert
        foreach ((string servidor, int puerto, string descripcion) in ConfiguracionDxCluster.ServidoresConocidos)
        {
            servidor.Should().NotBeNullOrWhiteSpace();
            puerto.Should().BeInRange(1, 65535);
            descripcion.Should().NotBeNullOrWhiteSpace();
        }
    }
}
