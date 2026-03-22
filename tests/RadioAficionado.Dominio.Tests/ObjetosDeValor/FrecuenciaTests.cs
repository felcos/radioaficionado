using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.ObjetosDeValor;

/// <summary>
/// Tests unitarios para el objeto de valor <see cref="Frecuencia"/>.
/// Valida la creación desde Hz/KHz/MHz, conversiones, detección de banda,
/// formato de texto y ordenamiento.
/// </summary>
public class FrecuenciaTests
{
    [Fact]
    public void DesdeHz_ValorPositivo_CreaFrecuencia()
    {
        // Arrange
        long hz = 14_074_000;

        // Act
        Frecuencia frecuencia = Frecuencia.DesdeHz(hz);

        // Assert
        frecuencia.Hz.Should().Be(14_074_000);
    }

    [Fact]
    public void DesdeKHz_ValorPositivo_CreaFrecuenciaConHzCorrectos()
    {
        // Arrange
        double khz = 7074.0;

        // Act
        Frecuencia frecuencia = Frecuencia.DesdeKHz(khz);

        // Assert
        frecuencia.Hz.Should().Be(7_074_000);
    }

    [Fact]
    public void DesdeMHz_ValorPositivo_CreaFrecuenciaConHzCorrectos()
    {
        // Arrange
        double mhz = 14.074;

        // Act
        Frecuencia frecuencia = Frecuencia.DesdeMHz(mhz);

        // Assert
        frecuencia.Hz.Should().Be(14_074_000);
    }

    [Fact]
    public void DesdeHz_ValorCero_LanzaArgumentException()
    {
        // Arrange & Act
        Action accion = () => Frecuencia.DesdeHz(0);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DesdeHz_ValorNegativo_LanzaArgumentException()
    {
        // Arrange & Act
        Action accion = () => Frecuencia.DesdeHz(-1);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MHz_FrecuenciaDe14074000Hz_Devuelve14074()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeHz(14_074_000);

        // Act
        double mhz = frecuencia.MHz;

        // Assert
        mhz.Should().Be(14.074);
    }

    [Fact]
    public void KHz_FrecuenciaDe7074000Hz_Devuelve7074()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeHz(7_074_000);

        // Act
        double khz = frecuencia.KHz;

        // Assert
        khz.Should().Be(7074.0);
    }

    [Fact]
    public void ObtenerBanda_Frecuencia14074MHz_DevuelveBanda20m()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.074);

        // Act
        BandaRadio? banda = frecuencia.ObtenerBanda();

        // Assert
        banda.Should().Be(BandaRadio.Banda20m);
    }

    [Fact]
    public void ObtenerBanda_Frecuencia145500MHz_DevuelveBanda2m()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(145.5);

        // Act
        BandaRadio? banda = frecuencia.ObtenerBanda();

        // Assert
        banda.Should().Be(BandaRadio.Banda2m);
    }

    [Fact]
    public void ObtenerBanda_FrecuenciaFueraDeBanda_DevuelveNull()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(100.0);

        // Act
        BandaRadio? banda = frecuencia.ObtenerBanda();

        // Assert
        banda.Should().BeNull();
    }

    [Fact]
    public void ToString_FrecuenciaEnMHz_FormatoConMHz()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.074);

        // Act
        string texto = frecuencia.ToString();

        // Assert
        texto.Should().Contain("MHz");
        texto.Should().Contain("14");
        texto.Should().Contain("074");
    }

    [Fact]
    public void ToString_FrecuenciaEnKHz_FormatoConKHz()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeKHz(137.0);

        // Act
        string texto = frecuencia.ToString();

        // Assert
        texto.Should().Contain("KHz");
        texto.Should().Contain("137");
    }

    [Fact]
    public void CompareTo_DosFrecuencias_OrdenaCorrectamente()
    {
        // Arrange
        Frecuencia frecuenciaBaja = Frecuencia.DesdeMHz(7.074);
        Frecuencia frecuenciaAlta = Frecuencia.DesdeMHz(14.074);

        // Act
        int resultado = frecuenciaBaja.CompareTo(frecuenciaAlta);

        // Assert
        resultado.Should().BeNegative();
    }
}
