using FluentAssertions;
using RadioAficionado.Dominio.Sdr;

namespace RadioAficionado.Infraestructura.Tests.Sdr;

/// <summary>
/// Tests unitarios para el record <see cref="ConfiguracionSdr"/>.
/// </summary>
public sealed class ConfiguracionSdrTests
{
    [Fact]
    public void Crear_ConParametrosBasicos_AsignaCorrectamente()
    {
        // Arrange & Act
        ConfiguracionSdr configuracion = new(
            FrecuenciaCentralHz: 145_000_000,
            TasaDeMuestreoHz: 2_048_000,
            AnchoDeBandaHz: 200_000,
            GananciaDb: 40.0);

        // Assert
        configuracion.FrecuenciaCentralHz.Should().Be(145_000_000);
        configuracion.TasaDeMuestreoHz.Should().Be(2_048_000);
        configuracion.AnchoDeBandaHz.Should().Be(200_000);
        configuracion.GananciaDb.Should().Be(40.0);
    }

    [Fact]
    public void Crear_SinDispositivoPreferido_EsNull()
    {
        // Arrange & Act
        ConfiguracionSdr configuracion = new(
            FrecuenciaCentralHz: 145_000_000,
            TasaDeMuestreoHz: 2_048_000,
            AnchoDeBandaHz: 200_000,
            GananciaDb: 40.0);

        // Assert
        configuracion.DispositivoPreferido.Should().BeNull();
    }

    [Fact]
    public void Crear_ConDispositivoPreferido_AsignaCorrectamente()
    {
        // Arrange & Act
        ConfiguracionSdr configuracion = new(
            FrecuenciaCentralHz: 145_000_000,
            TasaDeMuestreoHz: 2_048_000,
            AnchoDeBandaHz: 200_000,
            GananciaDb: 40.0,
            DispositivoPreferido: "rtlsdr");

        // Assert
        configuracion.DispositivoPreferido.Should().Be("rtlsdr");
    }

    [Fact]
    public void TamanoBufferMuestras_PorDefecto_Es65536()
    {
        // Arrange & Act
        ConfiguracionSdr configuracion = new(
            FrecuenciaCentralHz: 145_000_000,
            TasaDeMuestreoHz: 2_048_000,
            AnchoDeBandaHz: 200_000,
            GananciaDb: 40.0);

        // Assert
        configuracion.TamanoBufferMuestras.Should().Be(65536);
    }

    [Fact]
    public void TamanoBufferMuestras_Personalizado_AsignaCorrectamente()
    {
        // Arrange & Act
        ConfiguracionSdr configuracion = new(
            FrecuenciaCentralHz: 145_000_000,
            TasaDeMuestreoHz: 2_048_000,
            AnchoDeBandaHz: 200_000,
            GananciaDb: 40.0,
            TamanoBufferMuestras: 131072);

        // Assert
        configuracion.TamanoBufferMuestras.Should().Be(131072);
    }
}
