using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Wspr;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el decodificador WSPR (Weak Signal Propagation Reporter).
/// Verifica instanciacion, propiedades, procesamiento de audio y manejo de errores.
/// </summary>
public class DecodificadorWsprTests
{
    [Fact]
    public void Modo_DebeSerWSPR()
    {
        // Arrange
        DecodificadorWspr decodificador = new();

        // Act & Assert
        decodificador.Modo.Should().Be(ModoOperacion.WSPR);

        decodificador.Dispose();
    }

    [Fact]
    public void TasaDeMuestreoRequeridaHz_DebeSer12000()
    {
        // Arrange
        DecodificadorWspr decodificador = new();

        // Act & Assert
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(12000);

        decodificador.Dispose();
    }

    [Fact]
    public async Task IniciarAsync_CambiaEstadoAActivo()
    {
        // Arrange
        DecodificadorWspr decodificador = new();

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
        DecodificadorWspr decodificador = new();
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
        DecodificadorWspr decodificador = new();
        await decodificador.IniciarAsync();
        short[] datosVacios = new short[1000];
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datosVacios), 12000, DateTimeOffset.UtcNow);

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
        DecodificadorWspr decodificador = new();
        short[] datos = new short[1000];
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 12000, DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        resultado.Should().BeEmpty();

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_BufferVacio_DevuelveVacio()
    {
        // Arrange
        DecodificadorWspr decodificador = new();
        await decodificador.IniciarAsync();
        short[] datosVacios = Array.Empty<short>();
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datosVacios), 12000, DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        resultado.Should().BeEmpty();

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_Cancelacion_LanzaOperationCanceledException()
    {
        // Arrange
        DecodificadorWspr decodificador = new();
        await decodificador.IniciarAsync();
        short[] datos = new short[1000];
        for (int i = 0; i < datos.Length; i++)
        {
            datos[i] = (short)(1000 * Math.Sin(2.0 * Math.PI * 1500.0 * i / 12000.0));
        }
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 12000, DateTimeOffset.UtcNow);
        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Func<Task> accion = async () => await decodificador.ProcesarAudioAsync(muestra, cts.Token);

        // Assert
        await accion.Should().ThrowAsync<OperationCanceledException>();

        decodificador.Dispose();
    }

    [Fact]
    public void ConfiguracionWspr_ValoresPorDefecto_Correctos()
    {
        // Arrange & Act
        ConfiguracionWspr config = new();

        // Assert
        config.TasaDeMuestreo.Should().Be(12000);
        config.DuracionTransmisionSegundos.Should().Be(110.6);
        config.NumeroTonos.Should().Be(4);
        config.EspaciadoTonosHz.Should().Be(1.4648);
        config.FrecuenciaBaseHz.Should().Be(1500.0);
        config.AnchoDeBandaHz.Should().Be(6.0);
        config.PotenciaMaximaDbm.Should().Be(37);
    }

    [Fact]
    public async Task ProcesarAudio_RuidoAleatorio_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorWspr decodificador = new();
        await decodificador.IniciarAsync();
        Random rng = new(42);
        short[] datos = new short[5000];
        for (int i = 0; i < datos.Length; i++)
        {
            datos[i] = (short)(rng.Next(-32768, 32767));
        }
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 12000, DateTimeOffset.UtcNow);

        // Act
        Func<Task> accion = async () => await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        await accion.Should().NotThrowAsync();

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_MultiplesLlamadas_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorWspr decodificador = new();
        await decodificador.IniciarAsync();
        short[] datos = new short[2000];
        for (int i = 0; i < datos.Length; i++)
        {
            datos[i] = (short)(500 * Math.Sin(2.0 * Math.PI * 1500.0 * i / 12000.0));
        }

        // Act
        Func<Task> accion = async () =>
        {
            for (int llamada = 0; llamada < 5; llamada++)
            {
                MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 12000, DateTimeOffset.UtcNow);
                await decodificador.ProcesarAudioAsync(muestra);
            }
        };

        // Assert
        await accion.Should().NotThrowAsync();

        decodificador.Dispose();
    }
}
