using FluentAssertions;
using RadioAficionado.Dominio.Configuracion;

namespace RadioAficionado.Dominio.Tests.Configuracion;

/// <summary>
/// Tests unitarios para las propiedades de configuracion general y estacion.
/// </summary>
public class ConfiguracionGeneralTests
{
    [Fact]
    public void ConfiguracionGeneral_ValoresPorDefecto_FormatoFechaEuropeo()
    {
        // Act
        ConfiguracionGeneral config = new();

        // Assert
        config.FormatoFecha.Should().Be("dd/MM/yyyy");
    }

    [Fact]
    public void ConfiguracionGeneral_ValoresPorDefecto_BackupAutomaticoActivado()
    {
        // Act
        ConfiguracionGeneral config = new();

        // Assert
        config.BackupAutomatico.Should().BeTrue();
        config.MaxBackups.Should().Be(10);
    }

    [Theory]
    [InlineData("dd/MM/yyyy")]
    [InlineData("MM/dd/yyyy")]
    [InlineData("yyyy-MM-dd")]
    public void ConfiguracionGeneral_FormatoFecha_PuedeConfigurarseATresFormatos(string formato)
    {
        // Act
        ConfiguracionGeneral config = new() { FormatoFecha = formato };

        // Assert
        config.FormatoFecha.Should().Be(formato);
    }

    [Fact]
    public void ConfiguracionEstacion_ValoresPorDefecto_NotasEstacionVacias()
    {
        // Act
        ConfiguracionEstacion config = new();

        // Assert
        config.NotasEstacion.Should().BeEmpty();
    }

    [Fact]
    public void ConfiguracionEstacion_NotasEstacion_PuedeAsignarTexto()
    {
        // Act
        ConfiguracionEstacion config = new()
        {
            NotasEstacion = "Yaesu FT-991A, vertical 20m, QTH Madrid"
        };

        // Assert
        config.NotasEstacion.Should().Be("Yaesu FT-991A, vertical 20m, QTH Madrid");
    }

    [Fact]
    public void ConfiguracionCompleta_IncluyeNuevasPropiedades()
    {
        // Arrange & Act
        ConfiguracionCompleta completa = new();
        completa.General.FormatoFecha = "yyyy-MM-dd";
        completa.General.BackupAutomatico = false;
        completa.Estacion.NotasEstacion = "Mi estacion";

        // Assert
        completa.General.FormatoFecha.Should().Be("yyyy-MM-dd");
        completa.General.BackupAutomatico.Should().BeFalse();
        completa.Estacion.NotasEstacion.Should().Be("Mi estacion");
    }
}
