using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Tests.Servicios;

/// <summary>
/// Tests para ServicioOperacionDigital — maquina de estados FT8.
/// </summary>
public sealed class ServicioOperacionDigitalTests : IAsyncDisposable
{
    private readonly ServicioOperacionDigital _servicio;
    private readonly ServicioEstadoOperacion _estado;

    public ServicioOperacionDigitalTests()
    {
        Mock<IAudioPipeline> mockAudio = new();
        mockAudio.Setup(a => a.ObtenerDispositivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DispositivoAudio>());
        Mock<IServicioWaterfall> mockWaterfall = new();
        Mock<ILogger<ServicioEstadoOperacion>> mockLoggerEstado = new();
        Mock<ILogger<ServicioOperacionDigital>> mockLogger = new();

        _estado = new ServicioEstadoOperacion(
            mockAudio.Object,
            mockWaterfall.Object,
            mockLoggerEstado.Object);

        _servicio = new ServicioOperacionDigital(_estado, mockLogger.Object);
    }

    [Fact]
    public void EstadoInicial_FaseInactivo()
    {
        // Assert
        _servicio.EstadoActual.Fase.Should().Be(FaseQsoFt8.Inactivo);
        _servicio.EstadoActual.TxHabilitado.Should().BeFalse();
        _servicio.EstadoActual.AutoSecuenciaActiva.Should().BeFalse();
    }

    [Fact]
    public async Task LlamarCqAsync_TransicionaACQEnviado()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));

        // Act
        await _servicio.LlamarCqAsync();

        // Assert
        _servicio.EstadoActual.Fase.Should().Be(FaseQsoFt8.CQEnviado);
        _servicio.EstadoActual.MensajeTxActual.Should().Be("CQ EA1ABC JM28");
    }

    [Fact]
    public async Task SeleccionarEstacionAsync_TransicionaAEsperandoRespuesta()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));

        // Act
        await _servicio.SeleccionarEstacionAsync("W1AW", "FN31", -15);

        // Assert
        _servicio.EstadoActual.Fase.Should().Be(FaseQsoFt8.EsperandoRespuesta);
        _servicio.EstadoActual.IndicativoDx.Should().Be("W1AW");
        _servicio.EstadoActual.GridDx.Should().Be("FN31");
    }

    [Fact]
    public async Task ProcesarDecodificacion_CQEnviado_RecibeRespuesta_TransicionaAReporteEnviado()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));
        await _servicio.HabilitarAutoSecuenciaAsync(true);
        await _servicio.LlamarCqAsync();

        // Act — alguien responde a mi CQ: "EA1ABC W1AW -15"
        await _servicio.ProcesarDecodificacionAsync("EA1ABC W1AW -15", "W1AW", -15);

        // Assert
        _servicio.EstadoActual.Fase.Should().Be(FaseQsoFt8.ReporteEnviado);
        _servicio.EstadoActual.IndicativoDx.Should().Be("W1AW");
        _servicio.EstadoActual.MensajeTxActual.Should().Contain("W1AW");
        _servicio.EstadoActual.MensajeTxActual.Should().Contain("EA1ABC");
    }

    [Fact]
    public async Task ProcesarDecodificacion_ReporteEnviado_RecibeRReporte_TransicionaARRR()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));
        await _servicio.HabilitarAutoSecuenciaAsync(true);
        await _servicio.LlamarCqAsync();
        await _servicio.ProcesarDecodificacionAsync("EA1ABC W1AW -15", "W1AW", -15);

        // Act — recibo R+reporte: "EA1ABC W1AW R-18"
        await _servicio.ProcesarDecodificacionAsync("EA1ABC W1AW R-18", "W1AW", -18);

        // Assert
        _servicio.EstadoActual.Fase.Should().Be(FaseQsoFt8.RRREnviado);
        _servicio.EstadoActual.MensajeTxActual.Should().Contain("RRR");
    }

    [Fact]
    public async Task ProcesarDecodificacion_RRREnviado_Recibe73_TransicionaA73Enviado()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));
        await _servicio.HabilitarAutoSecuenciaAsync(true);
        await _servicio.LlamarCqAsync();
        await _servicio.ProcesarDecodificacionAsync("EA1ABC W1AW -15", "W1AW", -15);
        await _servicio.ProcesarDecodificacionAsync("EA1ABC W1AW R-18", "W1AW", -18);

        // Act — recibo 73: "EA1ABC W1AW 73"
        await _servicio.ProcesarDecodificacionAsync("EA1ABC W1AW 73", "W1AW", -15);

        // Assert
        _servicio.EstadoActual.Fase.Should().Be(FaseQsoFt8.SetentatresEnviado);
        _servicio.EstadoActual.MensajeTxActual.Should().Contain("73");
    }

    [Fact]
    public async Task ProcesarDecodificacion_SinAutoSecuencia_NoAvanza()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));
        // Auto-secuencia deshabilitada (por defecto)
        await _servicio.LlamarCqAsync();

        // Act
        await _servicio.ProcesarDecodificacionAsync("EA1ABC W1AW -15", "W1AW", -15);

        // Assert — permanece en CQEnviado porque auto-secuencia no esta activa
        _servicio.EstadoActual.Fase.Should().Be(FaseQsoFt8.CQEnviado);
    }

    [Fact]
    public async Task DetenerTxAsync_DeshabilitaTx()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));
        await _servicio.HabilitarTxAsync(true);

        // Act
        await _servicio.DetenerTxAsync();

        // Assert
        _servicio.EstadoActual.TxHabilitado.Should().BeFalse();
    }

    [Fact]
    public async Task SeleccionarMensajeTxAsync_Tx1_GeneraCQ()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));

        // Act
        await _servicio.SeleccionarMensajeTxAsync(1);

        // Assert
        _servicio.EstadoActual.MensajeTxActual.Should().Be("CQ EA1ABC JM28");
    }

    [Fact]
    public async Task SeleccionarMensajeTxAsync_Tx6_TextoLibre()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));

        // Act
        await _servicio.SeleccionarMensajeTxAsync(6, "HOLA MUNDO");

        // Assert
        _servicio.EstadoActual.MensajeTxActual.Should().Be("HOLA MUNDO");
    }

    [Fact]
    public async Task EstadoCambiado_SeDispara_AlCambiarFase()
    {
        // Arrange
        await _servicio.ConfigurarAsync(new ConfiguracionSecuencia("EA1ABC", "JM28", 1500, true));
        EstadoSecuencia? estadoRecibido = null;
        _servicio.EstadoCambiado += (_, e) => estadoRecibido = e;

        // Act
        await _servicio.LlamarCqAsync();

        // Assert
        estadoRecibido.Should().NotBeNull();
        estadoRecibido!.Fase.Should().Be(FaseQsoFt8.CQEnviado);
    }

    public async ValueTask DisposeAsync()
    {
        await _servicio.DisposeAsync();
        await _estado.DisposeAsync();
    }
}
