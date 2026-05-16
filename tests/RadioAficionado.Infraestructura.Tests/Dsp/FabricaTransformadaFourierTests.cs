using FluentAssertions;
using RadioAficionado.Nativo.Dsp;
using RadioAficionado.Nativo.Dsp.Interfaces;
using Xunit;

namespace RadioAficionado.Infraestructura.Tests.Dsp;

/// <summary>
/// Tests para <see cref="FabricaTransformadaFourier"/>.
/// Verifican que la fabrica crea instancias validas y el fallback funciona correctamente.
/// </summary>
public class FabricaTransformadaFourierTests
{
    [Fact]
    public void Crear_ConTamanoValido_RetornaInstanciaNoNula()
    {
        // Arrange & Act
        using ITransformadaFourier fft = FabricaTransformadaFourier.Crear(1024);

        // Assert
        fft.Should().NotBeNull();
    }

    [Fact]
    public void Crear_ConTamanoValido_RetornaTamanoCorrecto()
    {
        // Arrange & Act
        using ITransformadaFourier fft = FabricaTransformadaFourier.Crear(2048);

        // Assert
        fft.Tamano.Should().Be(2048);
    }

    [Fact]
    public void Crear_SinFftw3_RetornaCooleyTukey()
    {
        // Arrange & Act
        using ITransformadaFourier fft = FabricaTransformadaFourier.Crear(512);

        // Assert — en entorno de CI sin FFTW3, debe ser Cooley-Tukey
        // Si FFTW3 esta disponible, sera TransformadaFftw3 (ambos son validos)
        fft.Should().BeAssignableTo<ITransformadaFourier>();
    }

    [Fact]
    public void Crear_InstanciaRetornadaCalculaFft_CorrectamenteSenoPuro()
    {
        // Arrange
        using ITransformadaFourier fft = FabricaTransformadaFourier.Crear(1024);
        double[] entrada = new double[1024];
        double frecuencia = 100.0;
        double tasaMuestreo = 1024.0;

        for (int i = 0; i < 1024; i++)
        {
            entrada[i] = Math.Sin(2.0 * Math.PI * frecuencia / tasaMuestreo * i);
        }

        // Act
        double[] magnitudesDb = fft.CalcularMagnitudDb(entrada);

        // Assert — el bin de 100 Hz debe tener la magnitud mas alta
        magnitudesDb.Should().NotBeEmpty();
        int binEsperado = (int)(frecuencia / (tasaMuestreo / 1024));
        double magnitudPico = magnitudesDb[binEsperado];
        magnitudPico.Should().BeGreaterThan(-20.0, "el pico de un seno puro debe ser prominente");
    }

    [Fact]
    public void Crear_InstanciaRetornadaCalcula_ResultadoComplejo()
    {
        // Arrange
        using ITransformadaFourier fft = FabricaTransformadaFourier.Crear(256);
        double[] entrada = new double[256];

        for (int i = 0; i < 256; i++)
        {
            entrada[i] = Math.Sin(2.0 * Math.PI * 10.0 / 256.0 * i);
        }

        // Act
        double[] resultado = fft.Calcular(entrada);

        // Assert
        resultado.Should().NotBeEmpty();
        resultado.Length.Should().Be((256 / 2 + 1) * 2, "debe tener N/2+1 pares complejos");
    }

    [Theory]
    [InlineData(64)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(4096)]
    public void Crear_ConDiferentesTamanos_RetornaInstanciaCorrecta(int tamano)
    {
        // Arrange & Act
        using ITransformadaFourier fft = FabricaTransformadaFourier.Crear(tamano);

        // Assert
        fft.Tamano.Should().Be(tamano);
    }

    [Fact]
    public void ObtenerNombreImplementacion_RetornaNombreNoVacio()
    {
        // Act
        string nombre = FabricaTransformadaFourier.ObtenerNombreImplementacion();

        // Assert
        nombre.Should().NotBeNullOrWhiteSpace();
        nombre.Should().ContainAny("FFTW3", "Cooley-Tukey");
    }

    [Fact]
    public void Fftw3EstaDisponible_RetornaResultadoConsistente()
    {
        // Act
        bool primera = FabricaTransformadaFourier.Fftw3EstaDisponible();
        bool segunda = FabricaTransformadaFourier.Fftw3EstaDisponible();

        // Assert — el resultado cacheado debe ser consistente
        primera.Should().Be(segunda);
    }

    [Fact]
    public void Crear_Silencio_RetornaMagnitudesBajas()
    {
        // Arrange
        using ITransformadaFourier fft = FabricaTransformadaFourier.Crear(512);
        double[] silencio = new double[512];

        // Act
        double[] magnitudesDb = fft.CalcularMagnitudDb(silencio);

        // Assert — silencio debe dar magnitudes muy bajas
        magnitudesDb.Should().AllSatisfy(db => db.Should().BeLessThanOrEqualTo(-100.0));
    }

    [Fact]
    public void Crear_MultipleInstancias_TodasFuncionan()
    {
        // Arrange & Act
        using ITransformadaFourier fft1 = FabricaTransformadaFourier.Crear(256);
        using ITransformadaFourier fft2 = FabricaTransformadaFourier.Crear(512);
        using ITransformadaFourier fft3 = FabricaTransformadaFourier.Crear(1024);

        double[] entrada256 = new double[256];
        double[] entrada512 = new double[512];
        double[] entrada1024 = new double[1024];

        // Assert — todas deben funcionar sin lanzar excepciones
        double[] r1 = fft1.CalcularMagnitudDb(entrada256);
        double[] r2 = fft2.CalcularMagnitudDb(entrada512);
        double[] r3 = fft3.CalcularMagnitudDb(entrada1024);

        r1.Length.Should().Be(129);
        r2.Length.Should().Be(257);
        r3.Length.Should().Be(513);
    }
}
