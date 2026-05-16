using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Olivia;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el decodificador Olivia (MFSK con Walsh-Hadamard FEC).
/// Verifica instanciacion, propiedades, procesamiento de audio, diferentes modos y manejo de errores.
/// </summary>
public class DecodificadorOliviaTests
{
    [Fact]
    public void Modo_DebeSerOLIVIA()
    {
        // Arrange
        DecodificadorOlivia decodificador = new();

        // Act & Assert
        decodificador.Modo.Should().Be(ModoOperacion.OLIVIA);

        decodificador.Dispose();
    }

    [Fact]
    public void TasaDeMuestreoRequeridaHz_DebeSer8000()
    {
        // Arrange
        DecodificadorOlivia decodificador = new();

        // Act & Assert
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(8000);

        decodificador.Dispose();
    }

    [Fact]
    public void SubModosSoportados_DebeContenerModosOlivia()
    {
        // Arrange
        DecodificadorOlivia decodificador = new();

        // Act & Assert
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.OLIVIA_8_250);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.OLIVIA_32_1000);

        decodificador.Dispose();
    }

    [Fact]
    public async Task IniciarAsync_CambiaEstadoAActivo()
    {
        // Arrange
        DecodificadorOlivia decodificador = new();

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
        DecodificadorOlivia decodificador = new();
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
        DecodificadorOlivia decodificador = new();
        await decodificador.IniciarAsync();
        short[] datosVacios = new short[1000];
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datosVacios), 8000, DateTimeOffset.UtcNow);

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
        DecodificadorOlivia decodificador = new();
        short[] datos = new short[1000];
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 8000, DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        resultado.Should().BeEmpty();

        decodificador.Dispose();
    }

    [Fact]
    public void ConfiguracionOlivia_Modo4_125_EsValida()
    {
        // Arrange & Act
        ConfiguracionOlivia config = new()
        {
            NumeroTonos = 4,
            AnchoDeBandaHz = 125
        };

        // Assert
        config.EsValida().Should().BeTrue();
        config.TiempoSimboloSegundos.Should().Be(4.0 / 125.0);
    }

    [Fact]
    public void ConfiguracionOlivia_Modo32_1000_ValoresPorDefecto()
    {
        // Arrange & Act
        ConfiguracionOlivia config = new();

        // Assert
        config.NumeroTonos.Should().Be(32);
        config.AnchoDeBandaHz.Should().Be(1000);
        config.TasaDeMuestreo.Should().Be(8000);
        config.TiempoSimboloSegundos.Should().Be(32.0 / 1000.0);
        config.EsValida().Should().BeTrue();
    }

    [Fact]
    public void ConfiguracionOlivia_Modo64_2000_EsValida()
    {
        // Arrange & Act
        ConfiguracionOlivia config = new()
        {
            NumeroTonos = 64,
            AnchoDeBandaHz = 2000
        };

        // Assert
        config.EsValida().Should().BeTrue();
        config.TiempoSimboloSegundos.Should().Be(64.0 / 2000.0);
    }

    [Fact]
    public async Task ProcesarAudio_RuidoAleatorio_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorOlivia decodificador = new();
        await decodificador.IniciarAsync();
        Random rng = new(42);
        short[] datos = new short[5000];
        for (int i = 0; i < datos.Length; i++)
        {
            datos[i] = (short)(rng.Next(-32768, 32767));
        }
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), 8000, DateTimeOffset.UtcNow);

        // Act
        Func<Task> accion = async () => await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        await accion.Should().NotThrowAsync();

        decodificador.Dispose();
    }
}
