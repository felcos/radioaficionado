using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Activaciones;

/// <summary>
/// Tests unitarios para el objeto de valor <see cref="ReferenciaPota"/>.
/// Valida la construcción con formatos válidos e inválidos, extracción de país y número,
/// normalización a mayúsculas y conversión implícita a string.
/// </summary>
public class ReferenciaPotaTests
{
    [Theory]
    [InlineData("US-0001")]
    [InlineData("K-0001")]
    [InlineData("EA-0001")]
    [InlineData("VK-1234")]
    [InlineData("G-12345")]
    public void Constructor_FormatoValido_CreaCorrectamente(string valor)
    {
        // Arrange & Act
        ReferenciaPota referencia = new ReferenciaPota(valor);

        // Assert
        referencia.Valor.Should().Be(valor.ToUpperInvariant());
    }

    [Fact]
    public void Constructor_Minusculas_NormalizaAMayusculas()
    {
        // Arrange
        string valor = "us-0001";

        // Act
        ReferenciaPota referencia = new ReferenciaPota(valor);

        // Assert
        referencia.Valor.Should().Be("US-0001");
    }

    [Theory]
    [InlineData("US-0001", "US")]
    [InlineData("K-0001", "K")]
    [InlineData("EA-0001", "EA")]
    public void Pais_ReferenciaValida_ExtraePaisCorrectamente(string valor, string paisEsperado)
    {
        // Arrange
        ReferenciaPota referencia = new ReferenciaPota(valor);

        // Act
        string pais = referencia.Pais;

        // Assert
        pais.Should().Be(paisEsperado);
    }

    [Theory]
    [InlineData("US-0001", "0001")]
    [InlineData("EA-1234", "1234")]
    [InlineData("G-12345", "12345")]
    public void Numero_ReferenciaValida_ExtraeNumeroCorrectamente(string valor, string numeroEsperado)
    {
        // Arrange
        ReferenciaPota referencia = new ReferenciaPota(valor);

        // Act
        string numero = referencia.Numero;

        // Assert
        numero.Should().Be(numeroEsperado);
    }

    [Fact]
    public void Constructor_ReferenciaVacia_LanzaArgumentException()
    {
        // Arrange
        string valor = "";

        // Act
        Action accion = () => new ReferenciaPota(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ReferenciaNula_LanzaArgumentException()
    {
        // Arrange
        string? valor = null;

        // Act
        Action accion = () => new ReferenciaPota(valor!);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("US0001")]        // Sin guion
    [InlineData("USA-0001")]      // Más de 2 letras
    [InlineData("US-001")]        // Solo 3 dígitos
    [InlineData("US-123456")]     // 6 dígitos
    [InlineData("1-0001")]        // Número en lugar de letra
    [InlineData("US-ABCD")]       // Letras en lugar de dígitos
    [InlineData("-0001")]         // Sin país
    [InlineData("US-")]           // Sin número
    public void Constructor_FormatoInvalido_LanzaArgumentException(string valor)
    {
        // Arrange & Act
        Action accion = () => new ReferenciaPota(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OperadorImplicito_ConvierteAString_DevuelveValor()
    {
        // Arrange
        ReferenciaPota referencia = new ReferenciaPota("EA-0001");

        // Act
        string resultado = referencia;

        // Assert
        resultado.Should().Be("EA-0001");
    }

    [Fact]
    public void ToString_ReferenciaValida_DevuelveValor()
    {
        // Arrange
        ReferenciaPota referencia = new ReferenciaPota("US-0001");

        // Act
        string resultado = referencia.ToString();

        // Assert
        resultado.Should().Be("US-0001");
    }

    [Fact]
    public void Igualdad_MismaReferencia_SonIguales()
    {
        // Arrange
        ReferenciaPota referencia1 = new ReferenciaPota("US-0001");
        ReferenciaPota referencia2 = new ReferenciaPota("US-0001");

        // Act & Assert
        referencia1.Should().Be(referencia2);
    }

    [Fact]
    public void Igualdad_DiferenteReferencia_NoSonIguales()
    {
        // Arrange
        ReferenciaPota referencia1 = new ReferenciaPota("US-0001");
        ReferenciaPota referencia2 = new ReferenciaPota("EA-0002");

        // Act & Assert
        referencia1.Should().NotBe(referencia2);
    }
}
