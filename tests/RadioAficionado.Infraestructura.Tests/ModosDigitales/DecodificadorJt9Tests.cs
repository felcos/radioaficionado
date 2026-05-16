using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Jt9;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el decodificador JT9 (9-FSK para senal debil HF).
/// Verifica instanciacion, propiedades, procesamiento de audio y manejo de errores.
/// </summary>
public class DecodificadorJt9Tests
{
    [Fact]
    public void Modo_DebeSerJT9()
    {
        // Arrange
        DecodificadorJt9 decodificador = new();

        // Act & Assert
        decodificador.Modo.Should().Be(ModoOperacion.JT9);

        decodificador.Dispose();
    }

    [Fact]
    public void TasaDeMuestreoRequeridaHz_DebeSer12000()
    {
        // Arrange
        DecodificadorJt9 decodificador = new();

        // Act & Assert
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(12000);

        decodificador.Dispose();
    }

    [Fact]
    public void SubModosSoportados_DebeContenerNinguno()
    {
        // Arrange
        DecodificadorJt9 decodificador = new();

        // Act & Assert
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.Ninguno);

        decodificador.Dispose();
    }

    [Fact]
    public async Task IniciarAsync_CambiaEstadoAActivo()
    {
        // Arrange
        DecodificadorJt9 decodificador = new();

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
        DecodificadorJt9 decodificador = new();
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
        DecodificadorJt9 decodificador = new();
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
    public async Task ProcesarAudio_BufferVacio_DevuelveVacio()
    {
        // Arrange
        DecodificadorJt9 decodificador = new();
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
    public async Task ProcesarAudio_SinIniciar_DevuelveVacio()
    {
        // Arrange
        DecodificadorJt9 decodificador = new();
        short[] datos = new short[1000];
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 12000, DateTimeOffset.UtcNow);

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
        DecodificadorJt9 decodificador = new();
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
    public void ConfiguracionJt9_ValoresPorDefecto_Correctos()
    {
        // Arrange & Act
        ConfiguracionJt9 config = new();

        // Assert
        config.TasaDeMuestreo.Should().Be(12000);
        config.DuracionSimboloSegundos.Should().Be(0.576);
        config.NumeroTonos.Should().Be(9);
        config.EspaciadoTonosHz.Should().Be(1.7361);
        config.FrecuenciaBaseHz.Should().Be(1500.0);
        config.TiempoTransmisionSegundos.Should().Be(49.0);
    }

    [Fact]
    public async Task ProcesarAudio_RuidoAleatorio_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorJt9 decodificador = new();
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
        DecodificadorJt9 decodificador = new();
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
