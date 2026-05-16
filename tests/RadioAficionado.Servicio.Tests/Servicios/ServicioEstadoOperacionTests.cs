using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Servicio.Dtos;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Tests.Servicios;

/// <summary>
/// Tests para ServicioEstadoOperacion.
/// </summary>
public sealed class ServicioEstadoOperacionTests : IAsyncDisposable
{
    private readonly Mock<IAudioPipeline> _mockAudio;
    private readonly Mock<IServicioWaterfall> _mockWaterfall;
    private readonly Mock<ILogger<ServicioEstadoOperacion>> _mockLogger;
    private readonly ServicioEstadoOperacion _servicio;

    public ServicioEstadoOperacionTests()
    {
        _mockAudio = new Mock<IAudioPipeline>();
        _mockWaterfall = new Mock<IServicioWaterfall>();
        _mockLogger = new Mock<ILogger<ServicioEstadoOperacion>>();

        _servicio = new ServicioEstadoOperacion(
            _mockAudio.Object,
            _mockWaterfall.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_EstadoInicial_NoConectado()
    {
        // Assert
        _servicio.Conectado.Should().BeFalse();
        _servicio.FrecuenciaHz.Should().Be(14_074_000);
        _servicio.ModoActual.Should().Be("FT8");
        _servicio.BandaActual.Should().Be("20m");
        _servicio.Transmitiendo.Should().BeFalse();
        _servicio.VfoActivo.Should().Be('A');
    }

    [Fact]
    public void ObtenerEstadoActual_RetornaEstadoCorrecto()
    {
        // Act
        EstadoRigDto estado = _servicio.ObtenerEstadoActual();

        // Assert
        estado.FrecuenciaHz.Should().Be(14_074_000);
        estado.FrecuenciaDisplay.Should().Be("14.074.000");
        estado.Modo.Should().Be("FT8");
        estado.Banda.Should().Be("20m");
        estado.Transmitiendo.Should().BeFalse();
        estado.VfoActivo.Should().Be('A');
    }

    [Fact]
    public async Task CambiarFrecuenciaAsync_SinConexion_ActualizaEstadoLocal()
    {
        // Arrange
        EstadoRigDto? estadoEmitido = null;
        _servicio.EstadoCambiado += (_, dto) => estadoEmitido = dto;

        // Act
        await _servicio.CambiarFrecuenciaAsync(7_074_000);

        // Assert
        _servicio.FrecuenciaHz.Should().Be(7_074_000);
        _servicio.BandaActual.Should().Be("40m");
        estadoEmitido.Should().NotBeNull();
        estadoEmitido!.FrecuenciaHz.Should().Be(7_074_000);
    }

    [Fact]
    public async Task CambiarBandaAsync_Cambia20mA40m_ActualizaFrecuencia()
    {
        // Act
        await _servicio.CambiarBandaAsync("40m");

        // Assert
        _servicio.BandaActual.Should().Be("40m");
        _servicio.FrecuenciaHz.Should().Be(7_074_000);
    }

    [Fact]
    public async Task CambiarModoAsync_CambiaAUSB_ActualizaEstado()
    {
        // Act
        await _servicio.CambiarModoAsync("USB");

        // Assert
        _servicio.ModoActual.Should().Be("USB");
    }

    [Fact]
    public async Task CambiarVfoAsync_AlternaEntreAyB()
    {
        // Assert inicial
        _servicio.VfoActivo.Should().Be('A');

        // Act
        await _servicio.CambiarVfoAsync();

        // Assert
        _servicio.VfoActivo.Should().Be('B');

        // Act
        await _servicio.CambiarVfoAsync();

        // Assert
        _servicio.VfoActivo.Should().Be('A');
    }

    [Fact]
    public async Task CambiarFrecuenciaAsync_FrecuenciaNegativa_NoHaceNada()
    {
        // Act
        await _servicio.CambiarFrecuenciaAsync(-100);

        // Assert
        _servicio.FrecuenciaHz.Should().Be(14_074_000); // No cambia
    }

    [Fact]
    public async Task DesconectarAsync_SinConexion_NoFalla()
    {
        // Act & Assert — no debe lanzar excepcion
        await _servicio.DesconectarAsync();
        _servicio.Conectado.Should().BeFalse();
    }

    [Fact]
    public void FormatearFrecuencia_14074000_Formateado()
    {
        // Act
        string resultado = ServicioEstadoOperacion.FormatearFrecuencia(14_074_000);

        // Assert
        resultado.Should().Be("14.074.000");
    }

    [Fact]
    public void FormatearFrecuencia_7074000_Formateado()
    {
        // Act
        string resultado = ServicioEstadoOperacion.FormatearFrecuencia(7_074_000);

        // Assert
        resultado.Should().Be("7.074.000");
    }

    [Fact]
    public void FormatearFrecuencia_144174000_Formateado()
    {
        // Act
        string resultado = ServicioEstadoOperacion.FormatearFrecuencia(144_174_000);

        // Assert
        resultado.Should().Be("144.174.000");
    }

    [Fact]
    public void FormatearFrecuencia_1500_Formateado()
    {
        // Act
        string resultado = ServicioEstadoOperacion.FormatearFrecuencia(1500);

        // Assert
        resultado.Should().Be("1.500");
    }

    [Fact]
    public void FormatearFrecuencia_500_SinFormato()
    {
        // Act
        string resultado = ServicioEstadoOperacion.FormatearFrecuencia(500);

        // Assert
        resultado.Should().Be("500");
    }

    public async ValueTask DisposeAsync()
    {
        await _servicio.DisposeAsync();
    }
}
