using FluentAssertions;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Tests.Hubs;

/// <summary>
/// Tests para la logica del HubWaterfall (conversion y throttle).
/// </summary>
public sealed class HubWaterfallTests
{
    [Fact]
    public void ConvertirMagnitudesABytes_RangoCompleto_MapeoLinealCorrecto()
    {
        // Arrange — rango completo de -120 a 0 dB
        double[] magnitudes = new double[256];
        for (int i = 0; i < 256; i++)
        {
            magnitudes[i] = -120.0 + (120.0 * i / 255.0);
        }

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado.Should().HaveCount(256);
        resultado[0].Should().Be(0);
        resultado[255].Should().Be(255);

        // Verificar que es monotonamente creciente
        for (int i = 1; i < resultado.Length; i++)
        {
            resultado[i].Should().BeGreaterThanOrEqualTo(resultado[i - 1]);
        }
    }

    [Fact]
    public void ConvertirMagnitudesABytes_TodosSilencio_TodosCero()
    {
        // Arrange
        double[] magnitudes = new double[1024];
        Array.Fill(magnitudes, -120.0);

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado.Should().HaveCount(1024);
        resultado.Should().AllSatisfy(b => b.Should().Be(0));
    }

    [Fact]
    public void ConvertirMagnitudesABytes_TodosMaximo_Todos255()
    {
        // Arrange
        double[] magnitudes = new double[512];
        Array.Fill(magnitudes, 0.0);

        // Act
        byte[] resultado = ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);

        // Assert
        resultado.Should().HaveCount(512);
        resultado.Should().AllSatisfy(b => b.Should().Be(255));
    }

    [Fact]
    public void ConvertirMagnitudesABytes_Tamano2048_RendimientoAceptable()
    {
        // Arrange — tamano tipico de FFT
        double[] magnitudes = new double[2048];
        Random rng = new(42);
        for (int i = 0; i < magnitudes.Length; i++)
        {
            magnitudes[i] = -120.0 + (rng.NextDouble() * 120.0);
        }

        // Act & Assert — debe completar en menos de 10ms
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        for (int j = 0; j < 100; j++)
        {
            ServicioEstadoOperacion.ConvertirMagnitudesABytes(magnitudes);
        }
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(100, "100 conversiones de 2048 samples deben ser rapidas");
    }
}
