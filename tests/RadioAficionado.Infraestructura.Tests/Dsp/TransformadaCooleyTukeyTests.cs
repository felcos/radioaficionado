using FluentAssertions;
using RadioAficionado.Nativo.Dsp;

namespace RadioAficionado.Infraestructura.Tests.Dsp;

/// <summary>
/// Tests unitarios para la FFT Cooley-Tukey managed.
/// Verifica la creacion, validacion de parametros y precision del calculo espectral.
/// </summary>
public class TransformadaCooleyTukeyTests
{
    [Fact]
    public void Constructor_TamanoPotenciaDe2_CreaCorrectamente()
    {
        // Arrange & Act
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(1024);

        // Assert
        fft.Tamano.Should().Be(1024);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(1000)]
    public void Constructor_TamanoNoPotenciaDe2_LanzaArgumentException(int tamano)
    {
        // Arrange & Act
        Action accion = () => new TransformadaCooleyTukey(tamano);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(256)]
    [InlineData(2048)]
    [InlineData(4096)]
    public void Constructor_TamanosPotenciaDe2Validos_NingunoLanzaExcepcion(int tamano)
    {
        // Arrange & Act
        Action accion = () => new TransformadaCooleyTukey(tamano);

        // Assert
        accion.Should().NotThrow();
    }

    [Fact]
    public void CalcularMagnitudDb_SenosPuros_PicoEnFrecuenciaCorrecta()
    {
        // Arrange: generar onda seno pura a 1000 Hz muestreada a 8000 Hz
        int tamano = 1024;
        int tasaMuestreo = 8000;
        double frecuenciaSeno = 1000.0;
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(tamano);

        double[] senal = new double[tamano];
        for (int i = 0; i < tamano; i++)
        {
            senal[i] = Math.Sin(2.0 * Math.PI * frecuenciaSeno * i / tasaMuestreo);
        }

        // Act
        double[] magnitudes = fft.CalcularMagnitudDb(senal);

        // Assert: el pico debe estar en bin = frecuenciaSeno * tamano / tasaMuestreo = 128
        int binEsperado = (int)(frecuenciaSeno * tamano / tasaMuestreo);
        int binPico = 0;
        double maxMagnitud = double.MinValue;
        for (int i = 0; i < magnitudes.Length; i++)
        {
            if (magnitudes[i] > maxMagnitud)
            {
                maxMagnitud = magnitudes[i];
                binPico = i;
            }
        }
        binPico.Should().Be(binEsperado);
    }

    [Fact]
    public void CalcularMagnitudDb_Silencio_TodosPorDebajoDelPisoMinimo()
    {
        // Arrange
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(512);
        double[] silencio = new double[512];

        // Act
        double[] magnitudes = fft.CalcularMagnitudDb(silencio);

        // Assert
        foreach (double mag in magnitudes)
        {
            mag.Should().BeLessThanOrEqualTo(-100.0);
        }
    }

    [Fact]
    public void CalcularMagnitudDb_DevuelveNDivididoDoseMasUnoElementos()
    {
        // Arrange
        int tamano = 256;
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(tamano);
        double[] datos = new double[tamano];

        // Act
        double[] magnitudes = fft.CalcularMagnitudDb(datos);

        // Assert
        magnitudes.Length.Should().Be(tamano / 2 + 1);
    }

    [Fact]
    public void Calcular_DevuelveParesDosVecesNDivididoDosMasUno()
    {
        // Arrange
        int tamano = 512;
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(tamano);
        double[] datos = new double[tamano];

        // Act
        double[] resultado = fft.Calcular(datos);

        // Assert: resultado es pares [re, im] para N/2+1 bins
        int binsEsperados = tamano / 2 + 1;
        resultado.Length.Should().Be(binsEsperados * 2);
    }

    [Fact]
    public void CalcularMagnitudDb_TamanoIncorrecto_LanzaArgumentException()
    {
        // Arrange
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(256);
        double[] datosIncorrectos = new double[128];

        // Act
        Action accion = () => fft.CalcularMagnitudDb(datosIncorrectos);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Calcular_TamanoIncorrecto_LanzaArgumentException()
    {
        // Arrange
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(256);
        double[] datosIncorrectos = new double[512];

        // Act
        Action accion = () => fft.Calcular(datosIncorrectos);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalcularMagnitudDb_DespuesDeDispose_LanzaObjectDisposedException()
    {
        // Arrange
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(256);
        double[] datos = new double[256];
        fft.Dispose();

        // Act
        Action accion = () => fft.CalcularMagnitudDb(datos);

        // Assert
        accion.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CalcularMagnitudDb_DosFrecuencias_DosPicosDistintos()
    {
        // Arrange: generar senal con dos tonos: 500 Hz y 2000 Hz a 8000 Hz de muestreo
        int tamano = 1024;
        int tasaMuestreo = 8000;
        TransformadaCooleyTukey fft = new TransformadaCooleyTukey(tamano);

        double[] senal = new double[tamano];
        for (int i = 0; i < tamano; i++)
        {
            senal[i] = Math.Sin(2.0 * Math.PI * 500.0 * i / tasaMuestreo)
                      + Math.Sin(2.0 * Math.PI * 2000.0 * i / tasaMuestreo);
        }

        // Act
        double[] magnitudes = fft.CalcularMagnitudDb(senal);

        // Assert: los bins correspondientes a 500 Hz y 2000 Hz deben tener magnitudes altas
        int bin500 = (int)(500.0 * tamano / tasaMuestreo);
        int bin2000 = (int)(2000.0 * tamano / tasaMuestreo);

        // Verificar que ambos bins tienen magnitudes significativamente por encima del piso
        magnitudes[bin500].Should().BeGreaterThan(-40.0);
        magnitudes[bin2000].Should().BeGreaterThan(-40.0);
    }
}
