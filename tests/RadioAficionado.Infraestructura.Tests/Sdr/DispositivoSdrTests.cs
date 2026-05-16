using FluentAssertions;
using RadioAficionado.Dominio.Sdr;

namespace RadioAficionado.Infraestructura.Tests.Sdr;

/// <summary>
/// Tests unitarios para el record <see cref="DispositivoSdr"/>.
/// </summary>
public sealed class DispositivoSdrTests
{
    [Fact]
    public void Crear_ConParametrosCompletos_AsignaCorrectamente()
    {
        // Arrange
        Dictionary<string, string> argumentos = new()
        {
            { "driver", "rtlsdr" },
            { "tuner", "r820t" }
        };

        // Act
        DispositivoSdr dispositivo = new(
            Nombre: "RTL-SDR Blog V3",
            Controlador: "rtlsdr",
            NumeroSerie: "00000001",
            Argumentos: argumentos);

        // Assert
        dispositivo.Nombre.Should().Be("RTL-SDR Blog V3");
        dispositivo.Controlador.Should().Be("rtlsdr");
        dispositivo.NumeroSerie.Should().Be("00000001");
        dispositivo.Argumentos.Should().HaveCount(2);
    }

    [Fact]
    public void Crear_SinNumeroSerie_EsNull()
    {
        // Arrange & Act
        DispositivoSdr dispositivo = new(
            Nombre: "HackRF One",
            Controlador: "hackrf",
            NumeroSerie: null,
            Argumentos: new Dictionary<string, string>());

        // Assert
        dispositivo.NumeroSerie.Should().BeNull();
    }

    [Fact]
    public void Crear_ConArgumentosVacios_DiccionarioVacio()
    {
        // Arrange & Act
        DispositivoSdr dispositivo = new(
            Nombre: "AirSpy Mini",
            Controlador: "airspy",
            NumeroSerie: "AIRSPY-001",
            Argumentos: new Dictionary<string, string>());

        // Assert
        dispositivo.Argumentos.Should().BeEmpty();
    }

    [Fact]
    public void Igualdad_MismosValores_SonIguales()
    {
        // Arrange
        Dictionary<string, string> argumentos1 = new() { { "key", "value" } };
        Dictionary<string, string> argumentos2 = argumentos1;

        DispositivoSdr dispositivo1 = new("RTL-SDR", "rtlsdr", "001", argumentos1);
        DispositivoSdr dispositivo2 = new("RTL-SDR", "rtlsdr", "001", argumentos2);

        // Act & Assert
        dispositivo1.Should().Be(dispositivo2);
    }

    [Fact]
    public void ToString_ContieneNombre()
    {
        // Arrange
        DispositivoSdr dispositivo = new(
            Nombre: "RTL-SDR Blog V3",
            Controlador: "rtlsdr",
            NumeroSerie: "00000001",
            Argumentos: new Dictionary<string, string>());

        // Act
        string texto = dispositivo.ToString();

        // Assert
        texto.Should().Contain("RTL-SDR Blog V3");
        texto.Should().Contain("rtlsdr");
    }
}
