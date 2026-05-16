using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Servicio.Dtos;
using RadioAficionado.Servicio.Hubs;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Tests.Hubs;

/// <summary>
/// Tests para HubRig.
/// </summary>
public sealed class HubRigTests : IAsyncDisposable
{
    private readonly ServicioEstadoOperacion _estado;
    private readonly HubRig _hub;

    public HubRigTests()
    {
        Mock<IAudioPipeline> mockAudio = new();
        mockAudio.Setup(a => a.ObtenerDispositivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DispositivoAudio>());
        Mock<IServicioWaterfall> mockWaterfall = new();
        Mock<ILogger<ServicioEstadoOperacion>> mockLogger = new();

        _estado = new ServicioEstadoOperacion(
            mockAudio.Object,
            mockWaterfall.Object,
            mockLogger.Object);

        _hub = new HubRig(_estado);
    }

    [Fact]
    public void ObtenerEstado_RetornaEstadoActual()
    {
        // Act
        EstadoRigDto estado = _hub.ObtenerEstado();

        // Assert
        estado.Should().NotBeNull();
        estado.FrecuenciaHz.Should().Be(14_074_000);
        estado.Modo.Should().Be("FT8");
        estado.Banda.Should().Be("20m");
    }

    [Fact]
    public void ObtenerPuertos_RetornaListaVaciaOReal()
    {
        // Act
        IReadOnlyList<string> puertos = _hub.ObtenerPuertos();

        // Assert
        puertos.Should().NotBeNull();
    }

    [Fact]
    public async Task ObtenerDispositivosAudio_RetornaLista()
    {
        // Act
        IReadOnlyList<DispositivoAudioDto> dispositivos = await _hub.ObtenerDispositivosAudio();

        // Assert
        dispositivos.Should().NotBeNull();
    }

    public async ValueTask DisposeAsync()
    {
        await _estado.DisposeAsync();
    }
}
