using FluentAssertions;
using RadioAficionado.Nativo.Rotador;

namespace RadioAficionado.Infraestructura.Tests.Rotador;

/// <summary>
/// Tests unitarios para <see cref="ConfiguracionRotador"/>.
/// Verifica los valores por defecto y la modificación de propiedades.
/// </summary>
public class ConfiguracionRotadorTests
{
    [Fact]
    public void Host_PorDefecto_EsLocalhost()
    {
        // Arrange & Act
        ConfiguracionRotador configuracion = new ConfiguracionRotador();

        // Assert
        configuracion.Host.Should().Be("localhost");
    }

    [Fact]
    public void Puerto_PorDefecto_Es4533()
    {
        // Arrange & Act
        ConfiguracionRotador configuracion = new ConfiguracionRotador();

        // Assert
        configuracion.Puerto.Should().Be(4533);
    }

    [Fact]
    public void IntervaloPollingMs_PorDefecto_Es1000()
    {
        // Arrange & Act
        ConfiguracionRotador configuracion = new ConfiguracionRotador();

        // Assert
        configuracion.IntervaloPollingMs.Should().Be(1000);
    }

    [Fact]
    public void UmbralCambioGrados_PorDefecto_EsCeroPuntoCinco()
    {
        // Arrange & Act
        ConfiguracionRotador configuracion = new ConfiguracionRotador();

        // Assert
        configuracion.UmbralCambioGrados.Should().Be(0.5);
    }

    [Fact]
    public void TimeoutMs_PorDefecto_Es5000()
    {
        // Arrange & Act
        ConfiguracionRotador configuracion = new ConfiguracionRotador();

        // Assert
        configuracion.TimeoutMs.Should().Be(5000);
    }
}
