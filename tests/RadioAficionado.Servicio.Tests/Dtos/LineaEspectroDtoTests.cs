using FluentAssertions;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Tests.Dtos;

/// <summary>
/// Tests para la conversion de magnitudes double[] a byte[].
/// </summary>
public sealed class LineaEspectroDtoTests
{
    [Fact]
    public void ConvertirMagnitudesABytes_ValorMaximo0dB_Retorna255()
    {
        // Arrange
        double[] magnitudes = [0.0];

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado[0].Should().Be(255);
    }

    [Fact]
    public void ConvertirMagnitudesABytes_ValorMinimo120dB_Retorna0()
    {
        // Arrange
        double[] magnitudes = [-120.0];

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado[0].Should().Be(0);
    }

    [Fact]
    public void ConvertirMagnitudesABytes_ValorMedio60dB_RetornaMitad()
    {
        // Arrange
        double[] magnitudes = [-60.0];

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado[0].Should().BeInRange(126, 128); // ~127.5
    }

    [Fact]
    public void ConvertirMagnitudesABytes_ValorPorDebajoDeRango_ClampA0()
    {
        // Arrange
        double[] magnitudes = [-200.0];

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado[0].Should().Be(0);
    }

    [Fact]
    public void ConvertirMagnitudesABytes_ValorPorEncimaDeRango_ClampA255()
    {
        // Arrange
        double[] magnitudes = [20.0];

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado[0].Should().Be(255);
    }

    [Fact]
    public void ConvertirMagnitudesABytes_ArrayMultiple_PreservaLongitud()
    {
        // Arrange
        double[] magnitudes = [-120.0, -90.0, -60.0, -30.0, 0.0];

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado.Should().HaveCount(5);
        resultado[0].Should().BeLessThan(resultado[1]);
        resultado[1].Should().BeLessThan(resultado[2]);
        resultado[2].Should().BeLessThan(resultado[3]);
        resultado[3].Should().BeLessThan(resultado[4]);
    }

    [Fact]
    public void ConvertirMagnitudesABytes_ArrayVacio_RetornaArrayVacio()
    {
        // Arrange
        double[] magnitudes = [];

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado.Should().BeEmpty();
    }
}
