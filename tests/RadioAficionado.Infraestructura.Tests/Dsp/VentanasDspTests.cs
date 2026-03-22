using FluentAssertions;
using RadioAficionado.Nativo.Dsp;

namespace RadioAficionado.Infraestructura.Tests.Dsp;

/// <summary>
/// Tests unitarios para las funciones de ventana DSP.
/// Verifica las propiedades matematicas de cada ventana (extremos, centro, simetria).
/// </summary>
public class VentanasDspTests
{
    [Fact]
    public void AplicarHann_Extremos_SonCero()
    {
        // Arrange
        double[] datos = new double[100];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarHann(datos);

        // Assert
        datos[0].Should().BeApproximately(0.0, 0.001);
        datos[99].Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void AplicarHann_Centro_EsUno()
    {
        // Arrange: tamano impar para que haya un centro exacto
        double[] datos = new double[101];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarHann(datos);

        // Assert
        datos[50].Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void AplicarHann_EsSimetrica()
    {
        // Arrange
        double[] datos = new double[128];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarHann(datos);

        // Assert: verificar simetria en varios puntos
        for (int i = 0; i < 64; i++)
        {
            datos[i].Should().BeApproximately(datos[127 - i], 1e-10);
        }
    }

    [Fact]
    public void AplicarHamming_Extremos_SonAproximadamentePuntoCincoYCuatro()
    {
        // Arrange
        double[] datos = new double[100];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarHamming(datos);

        // Assert: Hamming empieza en 0.54 - 0.46 = 0.08
        datos[0].Should().BeApproximately(0.08, 0.01);
    }

    [Fact]
    public void AplicarHamming_Centro_EsUno()
    {
        // Arrange
        double[] datos = new double[101];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarHamming(datos);

        // Assert
        datos[50].Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void AplicarHamming_EsSimetrica()
    {
        // Arrange
        double[] datos = new double[128];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarHamming(datos);

        // Assert
        for (int i = 0; i < 64; i++)
        {
            datos[i].Should().BeApproximately(datos[127 - i], 1e-10);
        }
    }

    [Fact]
    public void AplicarBlackmanHarris_Extremos_CercaDeCero()
    {
        // Arrange
        double[] datos = new double[100];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarBlackmanHarris(datos);

        // Assert: Blackman-Harris empieza en 0.35875 - 0.48829 + 0.14128 - 0.01168 = 0.00006
        datos[0].Should().BeApproximately(0.00006, 0.001);
    }

    [Fact]
    public void AplicarBlackmanHarris_Centro_EsCercaDeUno()
    {
        // Arrange
        double[] datos = new double[101];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarBlackmanHarris(datos);

        // Assert
        datos[50].Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void AplicarBlackmanHarris_EsSimetrica()
    {
        // Arrange
        double[] datos = new double[128];
        Array.Fill(datos, 1.0);

        // Act
        VentanasDsp.AplicarBlackmanHarris(datos);

        // Assert
        for (int i = 0; i < 64; i++)
        {
            datos[i].Should().BeApproximately(datos[127 - i], 1e-10);
        }
    }

    [Fact]
    public void AplicarHann_DatosVacios_NoLanzaExcepcion()
    {
        // Arrange
        double[] datos = Array.Empty<double>();

        // Act
        Action accion = () => VentanasDsp.AplicarHann(datos);

        // Assert
        accion.Should().NotThrow();
    }

    [Fact]
    public void AplicarHann_ValoresOriginales_SeMultiplican()
    {
        // Arrange: un solo valor en el centro
        double[] datos = new double[101];
        datos[50] = 5.0;

        // Act
        VentanasDsp.AplicarHann(datos);

        // Assert: el centro de Hann es 1.0, asi que 5.0 * 1.0 = 5.0
        datos[50].Should().BeApproximately(5.0, 0.001);
    }
}
