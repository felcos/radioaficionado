using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Sstv;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el decodificador SSTV (Slow-Scan Television).
/// Verifica instanciacion, propiedades, procesamiento de audio, deteccion de sincronizacion
/// y manejo de diferentes modos SSTV.
/// </summary>
public class DecodificadorSstvTests
{
    [Fact]
    public void Modo_DebeSerSSTV()
    {
        // Arrange
        DecodificadorSstv decodificador = new();

        // Act & Assert
        decodificador.Modo.Should().Be(ModoOperacion.SSTV);

        decodificador.Dispose();
    }

    [Fact]
    public void TasaDeMuestreoRequeridaHz_DebeSer11025()
    {
        // Arrange
        DecodificadorSstv decodificador = new();

        // Act & Assert
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(11025);

        decodificador.Dispose();
    }

    [Fact]
    public async Task IniciarAsync_CambiaEstadoAActivo()
    {
        // Arrange
        DecodificadorSstv decodificador = new();

        // Act
        await decodificador.IniciarAsync();

        // Assert
        decodificador.EstaActivo.Should().BeTrue();

        decodificador.Dispose();
    }

    [Fact]
    public async Task DetenerAsync_CambiaEstadoAInactivo()
    {
        // Arrange
        DecodificadorSstv decodificador = new();
        await decodificador.IniciarAsync();

        // Act
        await decodificador.DetenerAsync();

        // Assert
        decodificador.EstaActivo.Should().BeFalse();

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_Silencio_DevuelveVacio()
    {
        // Arrange
        DecodificadorSstv decodificador = new();
        await decodificador.IniciarAsync();
        short[] datosVacios = new short[500];
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datosVacios), 11025, DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        resultado.Should().BeEmpty();

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_SinIniciar_DevuelveVacio()
    {
        // Arrange
        DecodificadorSstv decodificador = new();
        short[] datos = new short[500];
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 11025, DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        resultado.Should().BeEmpty();

        decodificador.Dispose();
    }

    [Fact]
    public void ConfiguracionSstv_ValoresPorDefecto_Correctos()
    {
        // Arrange & Act
        ConfiguracionSstv config = new();

        // Assert
        config.TasaDeMuestreo.Should().Be(11025);
        config.ModoSstv.Should().Be(ModoSstv.Scottie1);
        config.FrecuenciaSincronizacionHz.Should().Be(1200.0);
        config.FrecuenciaNegroHz.Should().Be(1500.0);
        config.FrecuenciaBlancoHz.Should().Be(2300.0);
        config.AnchoImagen.Should().Be(320);
        config.AltoImagen.Should().Be(256);
    }

    [Fact]
    public void ConfiguracionSstv_ModoRobot36_AltoImagenEs240()
    {
        // Arrange & Act
        ConfiguracionSstv config = new()
        {
            ModoSstv = ModoSstv.Robot36
        };

        // Assert
        config.AltoImagen.Should().Be(240);
        config.AnchoImagen.Should().Be(320);
    }

    [Fact]
    public async Task ProcesarAudio_SenalSincronizacion1200Hz_DetectaSincronizacion()
    {
        // Arrange
        ConfiguracionSstv config = new();
        DecodificadorSstv decodificador = new(config);
        await decodificador.IniciarAsync();

        // Generar senal de sincronizacion a 1200 Hz fuerte
        int tasaMuestreo = 11025;
        int muestrasPorVentana = (int)(tasaMuestreo * 0.010);
        // Generar suficientes muestras para llenar al menos una ventana de analisis
        int totalMuestras = muestrasPorVentana * 3;
        short[] datos = new short[totalMuestras];
        for (int i = 0; i < totalMuestras; i++)
        {
            datos[i] = (short)(20000 * Math.Sin(2.0 * Math.PI * 1200.0 * i / tasaMuestreo));
        }
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), tasaMuestreo, DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert — debe detectar sincronizacion (al menos un mensaje con info de sync)
        resultado.Should().NotBeEmpty();
        resultado[0].Texto.Should().Contain("sincronizacion");

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_RuidoAleatorio_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorSstv decodificador = new();
        await decodificador.IniciarAsync();
        Random rng = new(42);
        short[] datos = new short[5000];
        for (int i = 0; i < datos.Length; i++)
        {
            datos[i] = (short)(rng.Next(-32768, 32767));
        }
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 11025, DateTimeOffset.UtcNow);

        // Act
        Func<Task> accion = async () => await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        await accion.Should().NotThrowAsync();

        decodificador.Dispose();
    }
}
