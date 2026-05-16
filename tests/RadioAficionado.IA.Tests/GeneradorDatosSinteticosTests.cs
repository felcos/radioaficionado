using FluentAssertions;
using RadioAficionado.IA;

namespace RadioAficionado.IA.Tests;

/// <summary>
/// Tests para el generador de datos sinteticos de entrenamiento.
/// Verifica cantidades, rangos y caracteristicas espectrales de los datos generados.
/// </summary>
public sealed class GeneradorDatosSinteticosTests
{
    private readonly Random _rng = new(42);

    [Fact]
    public void GenerarEspectrosCw_RetornaCantidadCorrecta()
    {
        // Arrange
        int cantidad = 50;

        // Act
        List<DatoEntrenamientoSenal> resultado = GeneradorDatosSinteticos.GenerarEspectrosCw(cantidad, _rng);

        // Assert
        resultado.Should().HaveCount(cantidad);
        resultado.Should().OnlyContain(d => d.Modo == "CW");
    }

    [Fact]
    public void GenerarEspectrosSsb_RetornaCantidadCorrecta()
    {
        // Arrange
        int cantidad = 75;

        // Act
        List<DatoEntrenamientoSenal> resultado = GeneradorDatosSinteticos.GenerarEspectrosSsb(cantidad, _rng);

        // Assert
        resultado.Should().HaveCount(cantidad);
        resultado.Should().OnlyContain(d => d.Modo == "SSB");
    }

    [Fact]
    public void GenerarEspectrosFm_RetornaCantidadCorrecta()
    {
        // Arrange
        int cantidad = 60;

        // Act
        List<DatoEntrenamientoSenal> resultado = GeneradorDatosSinteticos.GenerarEspectrosFm(cantidad, _rng);

        // Assert
        resultado.Should().HaveCount(cantidad);
        resultado.Should().OnlyContain(d => d.Modo == "FM");
    }

    [Fact]
    public void GenerarEspectrosFt8_RetornaCantidadCorrecta()
    {
        // Arrange
        int cantidad = 40;

        // Act
        List<DatoEntrenamientoSenal> resultado = GeneradorDatosSinteticos.GenerarEspectrosFt8(cantidad, _rng);

        // Assert
        resultado.Should().HaveCount(cantidad);
        resultado.Should().OnlyContain(d => d.Modo == "FT8");
    }

    [Fact]
    public void GenerarEspectrosRuido_RetornaCantidadCorrecta()
    {
        // Arrange
        int cantidad = 30;

        // Act
        List<DatoEntrenamientoSenal> resultado = GeneradorDatosSinteticos.GenerarEspectrosRuido(cantidad, _rng);

        // Assert
        resultado.Should().HaveCount(cantidad);
        resultado.Should().OnlyContain(d => d.Modo == "Ruido");
    }

    [Fact]
    public void GenerarDatosPropagacion_RetornaCantidadCorrecta()
    {
        // Arrange
        int cantidad = 100;

        // Act
        List<DatoEntrenamientoPropagacion> resultado = GeneradorDatosSinteticos.GenerarDatosPropagacion(cantidad, _rng);

        // Assert
        resultado.Should().HaveCount(cantidad);
    }

    [Fact]
    public void GenerarEspectrosCw_EspectrosTienenPico()
    {
        // Arrange
        int cantidad = 20;
        Random rng = new(123);

        // Act
        List<DatoEntrenamientoSenal> resultado = GeneradorDatosSinteticos.GenerarEspectrosCw(cantidad, rng);

        // Assert
        foreach (DatoEntrenamientoSenal dato in resultado)
        {
            float valorMaximo = dato.Espectro.Max();
            valorMaximo.Should().BeGreaterThan(0.3f,
                "los espectros CW deben tener un pico notable por encima del ruido de fondo");

            // Verificar que el espectro tiene 64 bins
            dato.Espectro.Should().HaveCount(64);
        }
    }

    [Fact]
    public void GenerarDatosPropagacion_ProbabilidadesEnRango()
    {
        // Arrange
        int cantidad = 200;
        Random rng = new(456);

        // Act
        List<DatoEntrenamientoPropagacion> resultado = GeneradorDatosSinteticos.GenerarDatosPropagacion(cantidad, rng);

        // Assert
        foreach (DatoEntrenamientoPropagacion dato in resultado)
        {
            dato.ProbabilidadApertura.Should().BeInRange(0.0f, 1.0f,
                "la probabilidad de apertura debe estar entre 0.0 y 1.0");

            dato.Sfi.Should().BeGreaterThanOrEqualTo(60f,
                "el SFI debe ser al menos 60");

            dato.IndiceK.Should().BeInRange(0f, 9f,
                "el indice K debe estar entre 0 y 9");
        }
    }
}
