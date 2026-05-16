using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Q65;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el decodificador Q65 (65-FSK con codificacion Q-ary Repeat Accumulate).
/// Verifica instanciacion, propiedades, submodos, procesamiento de audio y manejo de errores.
/// </summary>
public class DecodificadorQ65Tests
{
    [Fact]
    public void Modo_DebeSerQ65()
    {
        // Arrange
        DecodificadorQ65 decodificador = new();

        // Act & Assert
        decodificador.Modo.Should().Be(ModoOperacion.Q65);

        decodificador.Dispose();
    }

    [Fact]
    public void TasaDeMuestreoRequeridaHz_DebeSer12000()
    {
        // Arrange
        DecodificadorQ65 decodificador = new();

        // Act & Assert
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(12000);

        decodificador.Dispose();
    }

    [Fact]
    public void SubModosSoportados_DebeContenerTodosLosSubModos()
    {
        // Arrange
        DecodificadorQ65 decodificador = new();

        // Act & Assert
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.Q65A);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.Q65B);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.Q65C);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.Q65D);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.Q65E);
        decodificador.SubModosSoportados.Should().HaveCount(5);

        decodificador.Dispose();
    }

    [Fact]
    public async Task IniciarAsync_CambiaEstadoAActivo()
    {
        // Arrange
        DecodificadorQ65 decodificador = new();

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
        DecodificadorQ65 decodificador = new();
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
        DecodificadorQ65 decodificador = new();
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
        DecodificadorQ65 decodificador = new();
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
        DecodificadorQ65 decodificador = new();
        await decodificador.IniciarAsync();
        short[] datos = new short[1000];
        for (int i = 0; i < datos.Length; i++)
        {
            datos[i] = (short)(1000 * Math.Sin(2.0 * Math.PI * 1000.0 * i / 12000.0));
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
    public void ConfiguracionQ65_ValoresPorDefecto_Correctos()
    {
        // Arrange & Act
        ConfiguracionQ65 config = new();

        // Assert
        config.TasaDeMuestreo.Should().Be(12000);
        config.SubModo.Should().Be(SubModoQ65.A);
        config.NumeroTonos.Should().Be(65);
        config.FrecuenciaBaseHz.Should().Be(1000.0);
    }

    [Theory]
    [InlineData(SubModoQ65.A, 15)]
    [InlineData(SubModoQ65.B, 30)]
    [InlineData(SubModoQ65.C, 60)]
    [InlineData(SubModoQ65.D, 120)]
    [InlineData(SubModoQ65.E, 300)]
    public void SubModoQ65_ObtenerDuracionPeriodoSegundos_DevuelveValorCorrecto(SubModoQ65 subModo, int duracionEsperada)
    {
        // Act
        int duracion = subModo.ObtenerDuracionPeriodoSegundos();

        // Assert
        duracion.Should().Be(duracionEsperada);
    }

    [Fact]
    public async Task ProcesarAudio_RuidoAleatorio_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorQ65 decodificador = new();
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
        DecodificadorQ65 decodificador = new();
        await decodificador.IniciarAsync();
        short[] datos = new short[2000];
        for (int i = 0; i < datos.Length; i++)
        {
            datos[i] = (short)(500 * Math.Sin(2.0 * Math.PI * 1000.0 * i / 12000.0));
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
