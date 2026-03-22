using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.ObjetosDeValor;

/// <summary>
/// Tests unitarios para el objeto de valor <see cref="Localizador"/>.
/// Valida la construcción con diferentes longitudes, conversión a mayúsculas,
/// validación de formato, cálculo de coordenadas y distancia entre localizadores.
/// </summary>
public class LocalizadorTests
{
    [Fact]
    public void Constructor_Localizador4Caracteres_CreaCorrectamente()
    {
        // Arrange
        string valor = "IO91";

        // Act
        Localizador localizador = new Localizador(valor);

        // Assert
        localizador.Valor.Should().Be("IO91");
    }

    [Fact]
    public void Constructor_Localizador6Caracteres_CreaCorrectamente()
    {
        // Arrange
        string valor = "IO91WM";

        // Act
        Localizador localizador = new Localizador(valor);

        // Assert
        localizador.Valor.Should().Be("IO91WM");
    }

    [Fact]
    public void Constructor_Localizador8Caracteres_CreaCorrectamente()
    {
        // Arrange
        string valor = "IO91WM35";

        // Act
        Localizador localizador = new Localizador(valor);

        // Assert
        localizador.Valor.Should().Be("IO91WM35");
    }

    [Fact]
    public void Constructor_LocalizadorMinusculas_ConvierteAMayusculas()
    {
        // Arrange
        string valor = "io91wm";

        // Act
        Localizador localizador = new Localizador(valor);

        // Assert
        localizador.Valor.Should().Be("IO91WM");
    }

    [Fact]
    public void Constructor_LocalizadorNulo_LanzaArgumentException()
    {
        // Arrange
        string? valor = null;

        // Act
        Action accion = () => new Localizador(valor!);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_Longitud5Invalida_LanzaArgumentException()
    {
        // Arrange
        string valor = "IO91W";

        // Act
        Action accion = () => new Localizador(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_CaracteresInvalidos_LanzaArgumentException()
    {
        // Arrange
        string valor = "ZZ00";

        // Act
        Action accion = () => new Localizador(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ObtenerCoordenadas_LocalizadorConocido_DevuelveCoordenadas()
    {
        // Arrange
        Localizador localizador = new Localizador("IO91WM");

        // Act
        Coordenadas coordenadas = localizador.ObtenerCoordenadas();

        // Assert
        coordenadas.Latitud.Should().BeApproximately(51.48, 0.1);
        coordenadas.Longitud.Should().BeApproximately(-0.125, 0.1);
    }

    [Fact]
    public void CalcularDistancia_DosLocalizadores_DevuelveDistanciaAproximada()
    {
        // Arrange
        Localizador londres = new Localizador("IO91WM");
        Localizador nuevaYork = new Localizador("FN30AS");

        // Act
        double distancia = londres.CalcularDistancia(nuevaYork);

        // Assert
        distancia.Should().BeGreaterThan(5000.0);
        distancia.Should().BeLessThan(6000.0);
    }
}
