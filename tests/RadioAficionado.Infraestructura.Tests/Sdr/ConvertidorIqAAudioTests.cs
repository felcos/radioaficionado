using FluentAssertions;
using RadioAficionado.Nativo.Sdr;

namespace RadioAficionado.Infraestructura.Tests.Sdr;

/// <summary>
/// Tests unitarios para <see cref="ConvertidorIqAAudio"/>.
/// Verifican la conversión de muestras IQ a audio mono con magnitud, ganancia y normalización.
/// </summary>
public sealed class ConvertidorIqAAudioTests
{
    private readonly ConvertidorIqAAudio _convertidor;

    public ConvertidorIqAAudioTests()
    {
        _convertidor = new ConvertidorIqAAudio();
    }

    [Fact]
    public void Convertir_MuestrasNulas_LanzaExcepcion()
    {
        // Arrange
        double[]? muestrasI = null;
        double[] muestrasQ = new double[] { 1.0 };

        // Act
        Action accion = () => _convertidor.Convertir(muestrasI!, muestrasQ);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("muestrasI");
    }

    [Fact]
    public void Convertir_MuestrasQNulas_LanzaExcepcion()
    {
        // Arrange
        double[] muestrasI = new double[] { 1.0 };
        double[]? muestrasQ = null;

        // Act
        Action accion = () => _convertidor.Convertir(muestrasI, muestrasQ!);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("muestrasQ");
    }

    [Fact]
    public void Convertir_MuestrasVacias_RetornaVacio()
    {
        // Arrange
        double[] muestrasI = Array.Empty<double>();
        double[] muestrasQ = Array.Empty<double>();

        // Act
        double[] resultado = _convertidor.Convertir(muestrasI, muestrasQ);

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public void Convertir_TamanosDiferentes_LanzaExcepcion()
    {
        // Arrange
        double[] muestrasI = new double[] { 1.0, 2.0 };
        double[] muestrasQ = new double[] { 1.0 };

        // Act
        Action accion = () => _convertidor.Convertir(muestrasI, muestrasQ);

        // Assert
        accion.Should().Throw<ArgumentException>()
            .WithParameterName("muestrasQ");
    }

    [Fact]
    public void Convertir_SilencioIq_RetornaCeros()
    {
        // Arrange
        double[] muestrasI = new double[] { 0.0, 0.0, 0.0 };
        double[] muestrasQ = new double[] { 0.0, 0.0, 0.0 };

        // Act
        double[] resultado = _convertidor.Convertir(muestrasI, muestrasQ);

        // Assert
        resultado.Should().HaveCount(3);
        resultado.Should().AllBeEquivalentTo(0.0);
    }

    [Fact]
    public void Convertir_SenoEnI_RetornaMagnitud()
    {
        // Arrange — señal solo en I, magnitud = |I|
        double[] muestrasI = new double[] { 0.5, 1.0, 0.5 };
        double[] muestrasQ = new double[] { 0.0, 0.0, 0.0 };

        // Act
        double[] resultado = _convertidor.Convertir(muestrasI, muestrasQ);

        // Assert — normalizado, el mayor (1.0) debe ser 1.0, los de 0.5 deben ser 0.5
        resultado.Should().HaveCount(3);
        resultado[1].Should().BeApproximately(1.0, 0.001);
        resultado[0].Should().BeApproximately(0.5, 0.001);
        resultado[2].Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void Convertir_GananciaDigital_AplicaCorrectamente()
    {
        // Arrange — ganancia 2.0 duplica la señal antes de normalizar
        _convertidor.GananciaDigital = 2.0;
        double[] muestrasI = new double[] { 0.3, 0.6 };
        double[] muestrasQ = new double[] { 0.0, 0.0 };

        // Act
        double[] resultado = _convertidor.Convertir(muestrasI, muestrasQ);

        // Assert — después de normalización, las proporciones se mantienen
        resultado.Should().HaveCount(2);
        resultado[1].Should().BeApproximately(1.0, 0.001);
        resultado[0].Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void Convertir_GananciaCero_RetornaCeros()
    {
        // Arrange — ganancia 0 anula toda la señal
        _convertidor.GananciaDigital = 0.0;
        double[] muestrasI = new double[] { 1.0, 0.5 };
        double[] muestrasQ = new double[] { 0.5, 1.0 };

        // Act
        double[] resultado = _convertidor.Convertir(muestrasI, muestrasQ);

        // Assert — todo debe ser cero
        resultado.Should().HaveCount(2);
        resultado.Should().AllBeEquivalentTo(0.0);
    }

    [Fact]
    public void Convertir_ValoresMaximos_NormalizaCorrectamente()
    {
        // Arrange — valores grandes que necesitan normalización
        double[] muestrasI = new double[] { 100.0, 50.0, 0.0 };
        double[] muestrasQ = new double[] { 0.0, 50.0, 100.0 };

        // Act
        double[] resultado = _convertidor.Convertir(muestrasI, muestrasQ);

        // Assert — todos los valores deben estar en [0.0, 1.0]
        resultado.Should().HaveCount(3);
        resultado.Should().OnlyContain(v => v >= 0.0 && v <= 1.0);

        // La magnitud máxima (100) debe normalizarse a 1.0
        double magnitudMaxima = Math.Max(Math.Max(resultado[0], resultado[1]), resultado[2]);
        magnitudMaxima.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void GananciaDigital_PorDefecto_EsUno()
    {
        // Arrange & Act
        ConvertidorIqAAudio nuevoConvertidor = new();

        // Assert
        nuevoConvertidor.GananciaDigital.Should().Be(1.0);
    }

    [Fact]
    public void Convertir_MuestraUnica_RetornaUnValor()
    {
        // Arrange
        double[] muestrasI = new double[] { 3.0 };
        double[] muestrasQ = new double[] { 4.0 };

        // Act
        double[] resultado = _convertidor.Convertir(muestrasI, muestrasQ);

        // Assert — magnitud sqrt(9+16) = 5, normalizado a 1.0
        resultado.Should().HaveCount(1);
        resultado[0].Should().BeApproximately(1.0, 0.001);
    }
}
