using FluentAssertions;
using RadioAficionado.Nativo.Dsp;

namespace RadioAficionado.Infraestructura.Tests.Dsp;

/// <summary>
/// Tests unitarios para el procesador de espectro (waterfall).
/// Verifica el procesamiento de muestras PCM y la generacion de lineas de espectro.
/// </summary>
public class ProcesadorEspectroTests
{
    [Fact]
    public void Procesar_MuestrasPcmConSeno_DevuelveLineaEspectroValida()
    {
        // Arrange
        ProcesadorEspectro procesador = new ProcesadorEspectro(12000, 1024);
        short[] muestras = new short[1024];
        for (int i = 0; i < muestras.Length; i++)
        {
            muestras[i] = (short)(short.MaxValue * 0.5 * Math.Sin(2.0 * Math.PI * 1000.0 * i / 12000.0));
        }

        // Act
        LineaEspectro linea = procesador.Procesar(muestras);

        // Assert
        linea.Should().NotBeNull();
        linea.MagnitudesDb.Should().NotBeEmpty();
        linea.ResolucionHz.Should().BeApproximately(12000.0 / 1024.0, 0.1);
        linea.FrecuenciaMinHz.Should().Be(0.0);
        linea.FrecuenciaMaxHz.Should().BeApproximately(6000.0, 0.1);

        procesador.Dispose();
    }

    [Fact]
    public void Procesar_CantidadMuestrasIncorrecta_LanzaArgumentException()
    {
        // Arrange
        ProcesadorEspectro procesador = new ProcesadorEspectro(12000, 1024);
        short[] muestrasCortas = new short[512];

        // Act
        Action accion = () => procesador.Procesar(muestrasCortas);

        // Assert
        accion.Should().Throw<ArgumentException>();

        procesador.Dispose();
    }

    [Fact]
    public void ProcesarBloque_ConSolapamiento_DevuelveMultiplesLineas()
    {
        // Arrange
        ProcesadorEspectro procesador = new ProcesadorEspectro(12000, 512);
        short[] muestras = new short[2048];
        for (int i = 0; i < muestras.Length; i++)
        {
            muestras[i] = (short)(1000.0 * Math.Sin(2.0 * Math.PI * 500.0 * i / 12000.0));
        }

        // Act
        IReadOnlyList<LineaEspectro> lineas = procesador.ProcesarBloque(muestras, 50);

        // Assert
        lineas.Count.Should().BeGreaterThan(1);

        procesador.Dispose();
    }

    [Fact]
    public void ProcesarBloque_SinSolapamiento_DevuelveLineasSinSuperposicion()
    {
        // Arrange
        ProcesadorEspectro procesador = new ProcesadorEspectro(8000, 512);
        short[] muestras = new short[2048];
        for (int i = 0; i < muestras.Length; i++)
        {
            muestras[i] = (short)(500.0 * Math.Sin(2.0 * Math.PI * 300.0 * i / 8000.0));
        }

        // Act
        IReadOnlyList<LineaEspectro> lineas = procesador.ProcesarBloque(muestras, 0);

        // Assert: con 2048 muestras y ventana de 512 sin solapamiento, deberia haber 4 lineas
        lineas.Count.Should().Be(4);

        procesador.Dispose();
    }

    [Fact]
    public void ProcesarBloque_MenosMuestrasQueVentana_DevuelveListaVacia()
    {
        // Arrange
        ProcesadorEspectro procesador = new ProcesadorEspectro(8000, 1024);
        short[] muestrasCortas = new short[512];

        // Act
        IReadOnlyList<LineaEspectro> lineas = procesador.ProcesarBloque(muestrasCortas, 50);

        // Assert
        lineas.Should().BeEmpty();

        procesador.Dispose();
    }

    [Fact]
    public void ProcesarBloque_SolapamientoFueraDeRango_LanzaArgumentOutOfRangeException()
    {
        // Arrange
        ProcesadorEspectro procesador = new ProcesadorEspectro(8000, 512);
        short[] muestras = new short[1024];

        // Act
        Action accion = () => procesador.ProcesarBloque(muestras, 100);

        // Assert
        accion.Should().Throw<ArgumentOutOfRangeException>();

        procesador.Dispose();
    }

    [Fact]
    public void Constructor_TasaMuestreoCero_LanzaArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action accion = () => new ProcesadorEspectro(0, 1024);

        // Assert
        accion.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_TasaMuestreoNegativa_LanzaArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action accion = () => new ProcesadorEspectro(-1, 1024);

        // Assert
        accion.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Procesar_DespuesDeDispose_LanzaObjectDisposedException()
    {
        // Arrange
        ProcesadorEspectro procesador = new ProcesadorEspectro(8000, 256);
        short[] muestras = new short[256];
        procesador.Dispose();

        // Act
        Action accion = () => procesador.Procesar(muestras);

        // Assert
        accion.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Procesar_MagnitudesDb_TieneCantidadCorrectaDeBins()
    {
        // Arrange
        int tamanoFft = 512;
        ProcesadorEspectro procesador = new ProcesadorEspectro(8000, tamanoFft);
        short[] muestras = new short[tamanoFft];

        // Act
        LineaEspectro linea = procesador.Procesar(muestras);

        // Assert: N/2 + 1 bins
        linea.MagnitudesDb.Length.Should().Be(tamanoFft / 2 + 1);

        procesador.Dispose();
    }
}
