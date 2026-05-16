using FluentAssertions;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Nativo.Dsp;
using Xunit;

namespace RadioAficionado.Infraestructura.Tests.Dsp;

/// <summary>
/// Tests para <see cref="ServicioWaterfall"/>.
/// Verifican la coordinacion entre pipeline de audio y procesador de espectro.
/// </summary>
public class ServicioWaterfallTests
{
    private readonly Mock<IAudioPipeline> _mockPipeline;

    public ServicioWaterfallTests()
    {
        _mockPipeline = new Mock<IAudioPipeline>();
    }

    [Fact]
    public void Constructor_ConPipelineValido_NoLanzaExcepcion()
    {
        // Act
        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);

        // Assert
        servicio.Should().NotBeNull();
        servicio.EstaActivo.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ConPipelineNulo_LanzaArgumentNullException()
    {
        // Act
        Action accion = () => new ServicioWaterfall(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task IniciarAsync_ConPipelineActivo_ActivaServicio()
    {
        // Arrange
        _mockPipeline.Setup(p => p.EstaActivo).Returns(true);
        _mockPipeline.Setup(p => p.TasaDeMuestreoHz).Returns(12000);
        _mockPipeline.Setup(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>())).Returns(Guid.NewGuid());

        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);

        // Act
        await servicio.IniciarAsync(2048);

        // Assert
        servicio.EstaActivo.Should().BeTrue();
        servicio.TamanoFft.Should().Be(2048);
        servicio.TasaDeMuestreoHz.Should().Be(12000);
    }

    [Fact]
    public async Task IniciarAsync_ConPipelineInactivo_LanzaExcepcion()
    {
        // Arrange
        _mockPipeline.Setup(p => p.EstaActivo).Returns(false);
        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);

        // Act
        Func<Task> accion = () => servicio.IniciarAsync();

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DetenerAsync_DespuesDeIniciar_DesactivaServicio()
    {
        // Arrange
        Guid suscripcionId = Guid.NewGuid();
        _mockPipeline.Setup(p => p.EstaActivo).Returns(true);
        _mockPipeline.Setup(p => p.TasaDeMuestreoHz).Returns(12000);
        _mockPipeline.Setup(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>())).Returns(suscripcionId);

        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);
        await servicio.IniciarAsync();

        // Act
        await servicio.DetenerAsync();

        // Assert
        servicio.EstaActivo.Should().BeFalse();
        _mockPipeline.Verify(p => p.Desuscribir(suscripcionId), Times.Once);
    }

    [Fact]
    public async Task DetenerAsync_SinIniciar_NoLanzaExcepcion()
    {
        // Arrange
        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);

        // Act
        Func<Task> accion = () => servicio.DetenerAsync();

        // Assert
        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task IniciarAsync_YaActivo_NoHaceNada()
    {
        // Arrange
        _mockPipeline.Setup(p => p.EstaActivo).Returns(true);
        _mockPipeline.Setup(p => p.TasaDeMuestreoHz).Returns(12000);
        _mockPipeline.Setup(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>())).Returns(Guid.NewGuid());

        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);
        await servicio.IniciarAsync();

        // Act
        await servicio.IniciarAsync();

        // Assert — solo una suscripcion
        _mockPipeline.Verify(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>()), Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_SeSuscribeAlPipeline()
    {
        // Arrange
        _mockPipeline.Setup(p => p.EstaActivo).Returns(true);
        _mockPipeline.Setup(p => p.TasaDeMuestreoHz).Returns(12000);
        _mockPipeline.Setup(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>())).Returns(Guid.NewGuid());

        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);

        // Act
        await servicio.IniciarAsync(1024);

        // Assert
        _mockPipeline.Verify(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>()), Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_ConTamanoFftPersonalizado_UsaElTamanoEspecificado()
    {
        // Arrange
        _mockPipeline.Setup(p => p.EstaActivo).Returns(true);
        _mockPipeline.Setup(p => p.TasaDeMuestreoHz).Returns(48000);
        _mockPipeline.Setup(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>())).Returns(Guid.NewGuid());

        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);

        // Act
        await servicio.IniciarAsync(4096);

        // Assert
        servicio.TamanoFft.Should().Be(4096);
        servicio.TasaDeMuestreoHz.Should().Be(48000);
    }

    [Fact]
    public async Task LineaEspectroGenerada_AlRecibirMuestraCompleta_DisparaEvento()
    {
        // Arrange
        Action<MuestraAudio>? consumidorCapturado = null;
        _mockPipeline.Setup(p => p.EstaActivo).Returns(true);
        _mockPipeline.Setup(p => p.TasaDeMuestreoHz).Returns(12000);
        _mockPipeline.Setup(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>()))
            .Callback<Action<MuestraAudio>>(c => consumidorCapturado = c)
            .Returns(Guid.NewGuid());

        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);
        await servicio.IniciarAsync(512);

        bool eventoRecibido = false;
        servicio.LineaEspectroGenerada += (_, _) => eventoRecibido = true;

        // Act — enviar suficientes muestras para completar un buffer FFT
        short[] muestras = new short[512];
        for (int i = 0; i < 512; i++)
        {
            muestras[i] = (short)(Math.Sin(2.0 * Math.PI * 1000.0 / 12000.0 * i) * 16000);
        }

        MuestraAudio muestra = new MuestraAudio(muestras, 12000, DateTimeOffset.UtcNow);
        consumidorCapturado!.Invoke(muestra);

        // Assert
        eventoRecibido.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_LiberaRecursos()
    {
        // Arrange
        Guid suscripcionId = Guid.NewGuid();
        _mockPipeline.Setup(p => p.EstaActivo).Returns(true);
        _mockPipeline.Setup(p => p.TasaDeMuestreoHz).Returns(12000);
        _mockPipeline.Setup(p => p.Suscribir(It.IsAny<Action<MuestraAudio>>())).Returns(suscripcionId);

        ServicioWaterfall servicio = new ServicioWaterfall(_mockPipeline.Object);
        await servicio.IniciarAsync();

        // Act
        await servicio.DisposeAsync();

        // Assert
        servicio.EstaActivo.Should().BeFalse();
        _mockPipeline.Verify(p => p.Desuscribir(suscripcionId), Times.Once);
    }
}
