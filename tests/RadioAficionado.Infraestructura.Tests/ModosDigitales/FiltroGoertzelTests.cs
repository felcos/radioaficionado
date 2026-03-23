using FluentAssertions;
using RadioAficionado.Nativo.ModosDigitales.Cw;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el filtro de Goertzel.
/// Genera audio sintetico PCM de 16 bits para verificar la deteccion de tono.
/// </summary>
public class FiltroGoertzelTests
{
    private const int FrecuenciaMuestreo = 12000;

    /// <summary>
    /// Genera muestras PCM de 16 bits con un tono sinusoidal a la frecuencia y amplitud especificadas.
    /// </summary>
    private static short[] GenerarTono(double frecuenciaHz, int duracionMuestras, double amplitud = 16000.0)
    {
        short[] muestras = new short[duracionMuestras];
        for (int i = 0; i < duracionMuestras; i++)
        {
            double valor = amplitud * Math.Sin(2.0 * Math.PI * frecuenciaHz * i / FrecuenciaMuestreo);
            muestras[i] = (short)Math.Clamp(valor, short.MinValue, short.MaxValue);
        }
        return muestras;
    }

    /// <summary>
    /// Genera muestras de silencio (todas cero).
    /// </summary>
    private static short[] GenerarSilencio(int duracionMuestras)
    {
        return new short[duracionMuestras];
    }

    [Fact]
    public void CalcularMagnitud_TonoPresente_DevuelveMagnitudAlta()
    {
        // Arrange
        short[] muestras = GenerarTono(700.0, 1200);

        // Act
        double magnitud = FiltroGoertzel.CalcularMagnitud(muestras, 700.0, FrecuenciaMuestreo);

        // Assert
        magnitud.Should().BeGreaterThan(1.0, "un tono fuerte a la frecuencia objetivo debe generar magnitud alta");
    }

    [Fact]
    public void CalcularMagnitud_Silencio_DevuelveMagnitudCercaDeCero()
    {
        // Arrange
        short[] muestras = GenerarSilencio(1200);

        // Act
        double magnitud = FiltroGoertzel.CalcularMagnitud(muestras, 700.0, FrecuenciaMuestreo);

        // Assert
        magnitud.Should().BeApproximately(0.0, 0.001, "el silencio no debe generar magnitud");
    }

    [Fact]
    public void CalcularMagnitud_TonoEnFrecuenciaDiferente_DevuelveMagnitudBaja()
    {
        // Arrange: tono a 1500 Hz, buscando 700 Hz
        short[] muestras = GenerarTono(1500.0, 1200);

        // Act
        double magnitudFuera = FiltroGoertzel.CalcularMagnitud(muestras, 700.0, FrecuenciaMuestreo);
        double magnitudEnFrecuencia = FiltroGoertzel.CalcularMagnitud(muestras, 1500.0, FrecuenciaMuestreo);

        // Assert
        magnitudFuera.Should().BeLessThan(magnitudEnFrecuencia * 0.1,
            "la magnitud fuera de la frecuencia objetivo debe ser mucho menor");
    }

    [Fact]
    public void CalcularMagnitud_Tono600Hz_DetectaCorrectamente()
    {
        // Arrange
        short[] muestras = GenerarTono(600.0, 1200);

        // Act
        double magnitudEnFrecuencia = FiltroGoertzel.CalcularMagnitud(muestras, 600.0, FrecuenciaMuestreo);
        double magnitudFuera = FiltroGoertzel.CalcularMagnitud(muestras, 1000.0, FrecuenciaMuestreo);

        // Assert
        magnitudEnFrecuencia.Should().BeGreaterThan(magnitudFuera * 5.0,
            "debe detectar claramente un tono de 600 Hz");
    }

    [Fact]
    public void CalcularMagnitud_Tono800Hz_DetectaCorrectamente()
    {
        // Arrange
        short[] muestras = GenerarTono(800.0, 1200);

        // Act
        double magnitudEnFrecuencia = FiltroGoertzel.CalcularMagnitud(muestras, 800.0, FrecuenciaMuestreo);
        double magnitudFuera = FiltroGoertzel.CalcularMagnitud(muestras, 400.0, FrecuenciaMuestreo);

        // Assert
        magnitudEnFrecuencia.Should().BeGreaterThan(magnitudFuera * 5.0,
            "debe detectar claramente un tono de 800 Hz");
    }

    [Fact]
    public void CalcularMagnitud_AmplitudMayor_DevuelveMagnitudMayor()
    {
        // Arrange
        short[] muestrasDebiles = GenerarTono(700.0, 1200, amplitud: 1000.0);
        short[] muestrasFuertes = GenerarTono(700.0, 1200, amplitud: 16000.0);

        // Act
        double magnitudDebil = FiltroGoertzel.CalcularMagnitud(muestrasDebiles, 700.0, FrecuenciaMuestreo);
        double magnitudFuerte = FiltroGoertzel.CalcularMagnitud(muestrasFuertes, 700.0, FrecuenciaMuestreo);

        // Assert
        magnitudFuerte.Should().BeGreaterThan(magnitudDebil,
            "mayor amplitud debe producir mayor magnitud");
    }

    [Fact]
    public void CalcularMagnitud_MuestrasVacias_DevuelveCero()
    {
        // Arrange
        short[] muestras = Array.Empty<short>();

        // Act
        double magnitud = FiltroGoertzel.CalcularMagnitud(muestras, 700.0, FrecuenciaMuestreo);

        // Assert
        magnitud.Should().Be(0.0);
    }

    [Fact]
    public void CalcularMagnitud_FrecuenciaInvalida_LanzaExcepcion()
    {
        // Arrange
        short[] muestras = GenerarTono(700.0, 100);

        // Act
        Action accion = () => FiltroGoertzel.CalcularMagnitud(muestras, -100.0, FrecuenciaMuestreo);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalcularMagnitud_FrecuenciaMuestreoInvalida_LanzaExcepcion()
    {
        // Arrange
        short[] muestras = GenerarTono(700.0, 100);

        // Act
        Action accion = () => FiltroGoertzel.CalcularMagnitud(muestras, 700.0, 0);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalcularMagnitudDouble_TonoPresente_DevuelveMagnitudAlta()
    {
        // Arrange: generar tono en formato double normalizado
        int duracion = 1200;
        double[] muestras = new double[duracion];
        for (int i = 0; i < duracion; i++)
        {
            muestras[i] = 0.5 * Math.Sin(2.0 * Math.PI * 700.0 * i / FrecuenciaMuestreo);
        }

        // Act
        double magnitud = FiltroGoertzel.CalcularMagnitud(muestras, 700.0, FrecuenciaMuestreo);

        // Assert
        magnitud.Should().BeGreaterThan(0.01, "un tono en la frecuencia objetivo debe detectarse");
    }

    [Theory]
    [InlineData(500.0)]
    [InlineData(700.0)]
    [InlineData(900.0)]
    [InlineData(1200.0)]
    public void CalcularMagnitud_VariasFrecuencias_DetectaLaPropiaYNoOtras(double frecuencia)
    {
        // Arrange
        short[] muestras = GenerarTono(frecuencia, 2400);
        double frecuenciaErronea = frecuencia + 500.0;
        if (frecuenciaErronea >= FrecuenciaMuestreo / 2.0)
        {
            frecuenciaErronea = frecuencia - 500.0;
        }

        // Act
        double magnitudCorrecta = FiltroGoertzel.CalcularMagnitud(muestras, frecuencia, FrecuenciaMuestreo);
        double magnitudErronea = FiltroGoertzel.CalcularMagnitud(muestras, frecuenciaErronea, FrecuenciaMuestreo);

        // Assert
        magnitudCorrecta.Should().BeGreaterThan(magnitudErronea * 2.0,
            $"la magnitud a {frecuencia} Hz debe ser significativamente mayor que a {frecuenciaErronea} Hz");
    }
}
