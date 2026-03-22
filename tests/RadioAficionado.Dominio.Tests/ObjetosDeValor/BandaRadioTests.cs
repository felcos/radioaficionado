using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.ObjetosDeValor;

/// <summary>
/// Tests unitarios para el enum <see cref="BandaRadio"/> y sus extensiones.
/// Valida rangos de frecuencia, nombres, categorías, exclusividad regional
/// y detección de banda desde frecuencia.
/// </summary>
public class BandaRadioTests
{
    [Fact]
    public void ObtenerRangoFrecuencia_Banda20m_DevuelveRangoCorrecto()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda20m;

        // Act
        (Frecuencia inicio, Frecuencia fin) = banda.ObtenerRangoFrecuencia();

        // Assert
        inicio.MHz.Should().Be(14.0);
        fin.MHz.Should().Be(14.35);
    }

    [Fact]
    public void ObtenerRangoFrecuencia_Banda2200m_DevuelveRangoCorrecto()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda2200m;

        // Act
        (Frecuencia inicio, Frecuencia fin) = banda.ObtenerRangoFrecuencia();

        // Assert
        inicio.KHz.Should().Be(135.7);
        fin.KHz.Should().Be(137.8);
    }

    [Fact]
    public void ObtenerNombre_Banda20m_Devuelve20Metros()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda20m;

        // Act
        string nombre = banda.ObtenerNombre();

        // Assert
        nombre.Should().Be("20 metros");
    }

    [Fact]
    public void ObtenerCategoria_Banda20m_DevuelveHf()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda20m;

        // Act
        CategoriaBanda categoria = banda.ObtenerCategoria();

        // Assert
        categoria.Should().Be(CategoriaBanda.Hf);
    }

    [Fact]
    public void ObtenerCategoria_Banda2m_DevuelveVhf()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda2m;

        // Act
        CategoriaBanda categoria = banda.ObtenerCategoria();

        // Assert
        categoria.Should().Be(CategoriaBanda.Vhf);
    }

    [Fact]
    public void ObtenerCategoria_Banda70cm_DevuelveUhf()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda70cm;

        // Act
        CategoriaBanda categoria = banda.ObtenerCategoria();

        // Assert
        categoria.Should().Be(CategoriaBanda.Uhf);
    }

    [Fact]
    public void ObtenerCategoria_Banda3cm_DevuelveMicroondas()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda3cm;

        // Act
        CategoriaBanda categoria = banda.ObtenerCategoria();

        // Assert
        categoria.Should().Be(CategoriaBanda.Microondas);
    }

    [Fact]
    public void ObtenerCategoria_Banda2200m_DevuelveLfMf()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda2200m;

        // Act
        CategoriaBanda categoria = banda.ObtenerCategoria();

        // Assert
        categoria.Should().Be(CategoriaBanda.LfMf);
    }

    [Fact]
    public void EsExclusivaDeRegion_Banda4m_DevuelveTrue()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda4m;

        // Act
        bool esExclusiva = banda.EsExclusivaDeRegion();

        // Assert
        esExclusiva.Should().BeTrue();
    }

    [Fact]
    public void EsExclusivaDeRegion_Banda20m_DevuelveFalse()
    {
        // Arrange
        BandaRadio banda = BandaRadio.Banda20m;

        // Act
        bool esExclusiva = banda.EsExclusivaDeRegion();

        // Assert
        esExclusiva.Should().BeFalse();
    }

    [Fact]
    public void DesdeFrecuencia_14074MHz_DevuelveBanda20m()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.074);

        // Act
        BandaRadio? banda = BandaRadioExtensiones.DesdeFrecuencia(frecuencia);

        // Assert
        banda.Should().Be(BandaRadio.Banda20m);
    }

    [Fact]
    public void DesdeFrecuencia_FueraDeBanda_DevuelveNull()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(100.0);

        // Act
        BandaRadio? banda = BandaRadioExtensiones.DesdeFrecuencia(frecuencia);

        // Assert
        banda.Should().BeNull();
    }
}
