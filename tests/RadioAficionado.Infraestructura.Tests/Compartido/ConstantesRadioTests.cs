using FluentAssertions;
using RadioAficionado.Compartido.Constantes;

namespace RadioAficionado.Infraestructura.Tests.Compartido;

/// <summary>
/// Tests unitarios para <see cref="ConstantesRadio"/>.
/// Verifica que las constantes físicas y de radio tengan valores correctos y coherentes.
/// </summary>
public class ConstantesRadioTests
{
    [Fact]
    public void VelocidadDeLaLuz_TieneValorCorrecto()
    {
        // Arrange & Act
        double velocidad = ConstantesRadio.VelocidadDeLaLuzMetrosPorSegundo;

        // Assert
        velocidad.Should().Be(299_792_458.0);
    }

    [Fact]
    public void RadioDeLaTierra_TieneValorPositivo()
    {
        // Arrange & Act
        double radio = ConstantesRadio.RadioDeLaTierraKm;

        // Assert
        radio.Should().BeGreaterThan(0);
        radio.Should().Be(6_371.0);
    }

    [Fact]
    public void GradosARadianes_MultiplicadoPorRadianesAGrados_EsUno()
    {
        // Arrange & Act
        double resultado = ConstantesRadio.GradosARadianes * ConstantesRadio.RadianesAGrados;

        // Assert
        resultado.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void FrecuenciaMinimaHz_EsMayorQueCero()
    {
        // Arrange & Act
        long frecuenciaMinima = ConstantesRadio.FrecuenciaMinimaHz;

        // Assert
        frecuenciaMinima.Should().BeGreaterThan(0);
        frecuenciaMinima.Should().Be(1);
    }

    [Fact]
    public void FrecuenciaMaximaHz_EsMayorQueFrecuenciaMinima()
    {
        // Arrange & Act
        long frecuenciaMaxima = ConstantesRadio.FrecuenciaMaximaHz;
        long frecuenciaMinima = ConstantesRadio.FrecuenciaMinimaHz;

        // Assert
        frecuenciaMaxima.Should().BeGreaterThan(frecuenciaMinima);
        frecuenciaMaxima.Should().Be(300_000_000_000);
    }
}
