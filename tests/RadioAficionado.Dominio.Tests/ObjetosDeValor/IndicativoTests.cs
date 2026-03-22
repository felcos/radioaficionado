using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.ObjetosDeValor;

/// <summary>
/// Tests unitarios para el objeto de valor <see cref="Indicativo"/>.
/// Valida la construcción, conversión a mayúsculas, extracción de prefijo/sufijo,
/// operadores implícitos y ordenamiento alfabético.
/// </summary>
public class IndicativoTests
{
    [Theory]
    [InlineData("EA4ABC")]
    [InlineData("W1AW")]
    [InlineData("VK2ABC")]
    [InlineData("JA1ABC")]
    public void Constructor_IndicativoValido_CreaCorrectamente(string valor)
    {
        // Arrange & Act
        Indicativo indicativo = new Indicativo(valor);

        // Assert
        indicativo.Valor.Should().Be(valor.ToUpperInvariant());
    }

    [Theory]
    [InlineData("EA4ABC/P")]
    [InlineData("W1AW/M")]
    public void Constructor_IndicativoConModificador_CreaCorrectamente(string valor)
    {
        // Arrange & Act
        Indicativo indicativo = new Indicativo(valor);

        // Assert
        indicativo.Valor.Should().Be(valor.ToUpperInvariant());
    }

    [Fact]
    public void Constructor_IndicativoMinusculas_ConvierteAMayusculas()
    {
        // Arrange
        string valorMinusculas = "ea4abc";

        // Act
        Indicativo indicativo = new Indicativo(valorMinusculas);

        // Assert
        indicativo.Valor.Should().Be("EA4ABC");
    }

    [Fact]
    public void Constructor_IndicativoNulo_LanzaArgumentException()
    {
        // Arrange
        string? valor = null;

        // Act
        Action accion = () => new Indicativo(valor!);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_IndicativoVacio_LanzaArgumentException()
    {
        // Arrange
        string valor = "";

        // Act
        Action accion = () => new Indicativo(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_IndicativoMuyCorto_LanzaArgumentException()
    {
        // Arrange
        string valor = "AB";

        // Act
        Action accion = () => new Indicativo(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_IndicativoMuyLargo_LanzaArgumentException()
    {
        // Arrange
        string valor = "EA4ABCDEFGH";

        // Act
        Action accion = () => new Indicativo(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("ABCDEF")]
    [InlineData("EA-4ABC")]
    public void Constructor_FormatoInvalido_LanzaArgumentException(string valor)
    {
        // Arrange & Act
        Action accion = () => new Indicativo(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("EA4ABC", "EA")]
    [InlineData("W1AW", "W")]
    public void Prefijo_IndicativoEstandar_ExtraePrefijo(string valor, string prefijoEsperado)
    {
        // Arrange
        Indicativo indicativo = new Indicativo(valor);

        // Act
        string prefijo = indicativo.Prefijo;

        // Assert
        prefijo.Should().Be(prefijoEsperado);
    }

    [Theory]
    [InlineData("EA4ABC", "ABC")]
    [InlineData("W1AW", "AW")]
    public void Sufijo_IndicativoEstandar_ExtraeSufijo(string valor, string sufijoEsperado)
    {
        // Arrange
        Indicativo indicativo = new Indicativo(valor);

        // Act
        string sufijo = indicativo.Sufijo;

        // Assert
        sufijo.Should().Be(sufijoEsperado);
    }

    [Fact]
    public void OperadorImplicito_ConvierteAString_DevuelveValor()
    {
        // Arrange
        Indicativo indicativo = new Indicativo("EA4ABC");

        // Act
        string resultado = indicativo;

        // Assert
        resultado.Should().Be("EA4ABC");
    }

    [Fact]
    public void OperadorImplicito_ConvierteDesdeString_CreaIndicativo()
    {
        // Arrange
        string valor = "EA4ABC";

        // Act
        Indicativo indicativo = valor;

        // Assert
        indicativo.Valor.Should().Be("EA4ABC");
    }

    [Fact]
    public void CompareTo_DosIndicativos_OrdenaAlfabeticamente()
    {
        // Arrange
        Indicativo indicativo1 = new Indicativo("AA1AA");
        Indicativo indicativo2 = new Indicativo("ZZ9ZZ");

        // Act
        int resultado = indicativo1.CompareTo(indicativo2);

        // Assert
        resultado.Should().BeNegative();
    }
}
