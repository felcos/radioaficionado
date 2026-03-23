using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Activaciones;

/// <summary>
/// Tests unitarios para el objeto de valor <see cref="ReferenciaSota"/>.
/// Valida la construcción con formatos válidos e inválidos, extracción de asociación,
/// región y número, normalización y conversión a string.
/// </summary>
public class ReferenciaSotaTests
{
    [Theory]
    [InlineData("G/LD-001")]
    [InlineData("EA4/MD-001")]
    [InlineData("W4C/EM-001")]
    [InlineData("VK2/CT-001")]
    public void Constructor_FormatoValido_CreaCorrectamente(string valor)
    {
        // Arrange & Act
        ReferenciaSota referencia = new ReferenciaSota(valor);

        // Assert
        referencia.Valor.Should().Be(valor.ToUpperInvariant());
    }

    [Fact]
    public void Constructor_Minusculas_NormalizaAMayusculas()
    {
        // Arrange
        string valor = "ea4/md-001";

        // Act
        ReferenciaSota referencia = new ReferenciaSota(valor);

        // Assert
        referencia.Valor.Should().Be("EA4/MD-001");
    }

    [Theory]
    [InlineData("G/LD-001", "G")]
    [InlineData("EA4/MD-001", "EA4")]
    [InlineData("W4C/EM-001", "W4C")]
    public void Asociacion_ReferenciaValida_ExtraeAsociacionCorrectamente(string valor, string asociacionEsperada)
    {
        // Arrange
        ReferenciaSota referencia = new ReferenciaSota(valor);

        // Act
        string asociacion = referencia.Asociacion;

        // Assert
        asociacion.Should().Be(asociacionEsperada);
    }

    [Theory]
    [InlineData("G/LD-001", "LD")]
    [InlineData("EA4/MD-001", "MD")]
    [InlineData("W4C/EM-001", "EM")]
    public void Region_ReferenciaValida_ExtraeRegionCorrectamente(string valor, string regionEsperada)
    {
        // Arrange
        ReferenciaSota referencia = new ReferenciaSota(valor);

        // Act
        string region = referencia.Region;

        // Assert
        region.Should().Be(regionEsperada);
    }

    [Theory]
    [InlineData("G/LD-001", "001")]
    [InlineData("EA4/MD-123", "123")]
    [InlineData("W4C/EM-999", "999")]
    public void Numero_ReferenciaValida_ExtraeNumeroCorrectamente(string valor, string numeroEsperado)
    {
        // Arrange
        ReferenciaSota referencia = new ReferenciaSota(valor);

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
        Action accion = () => new ReferenciaSota(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ReferenciaNula_LanzaArgumentException()
    {
        // Arrange
        string? valor = null;

        // Act
        Action accion = () => new ReferenciaSota(valor!);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("G/LD001")]          // Sin guion
    [InlineData("GLD-001")]          // Sin barra
    [InlineData("ABCD/LD-001")]      // Asociación de 4 caracteres
    [InlineData("G/L-001")]          // Región de 1 letra
    [InlineData("G/LDE-001")]        // Región de 3 letras
    [InlineData("G/LD-01")]          // Solo 2 dígitos
    [InlineData("G/LD-0001")]        // 4 dígitos
    [InlineData("G/12-001")]         // Región con dígitos
    [InlineData("/LD-001")]          // Sin asociación
    public void Constructor_FormatoInvalido_LanzaArgumentException(string valor)
    {
        // Arrange & Act
        Action accion = () => new ReferenciaSota(valor);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OperadorImplicito_ConvierteAString_DevuelveValor()
    {
        // Arrange
        ReferenciaSota referencia = new ReferenciaSota("EA4/MD-001");

        // Act
        string resultado = referencia;

        // Assert
        resultado.Should().Be("EA4/MD-001");
    }

    [Fact]
    public void ToString_ReferenciaValida_DevuelveValor()
    {
        // Arrange
        ReferenciaSota referencia = new ReferenciaSota("G/LD-001");

        // Act
        string resultado = referencia.ToString();

        // Assert
        resultado.Should().Be("G/LD-001");
    }

    [Fact]
    public void Igualdad_MismaReferencia_SonIguales()
    {
        // Arrange
        ReferenciaSota referencia1 = new ReferenciaSota("EA4/MD-001");
        ReferenciaSota referencia2 = new ReferenciaSota("EA4/MD-001");

        // Act & Assert
        referencia1.Should().Be(referencia2);
    }

    [Fact]
    public void Igualdad_DiferenteReferencia_NoSonIguales()
    {
        // Arrange
        ReferenciaSota referencia1 = new ReferenciaSota("EA4/MD-001");
        ReferenciaSota referencia2 = new ReferenciaSota("G/LD-002");

        // Act & Assert
        referencia1.Should().NotBe(referencia2);
    }
}
