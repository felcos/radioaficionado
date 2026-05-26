using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Servicio.Remoto;

namespace RadioAficionado.Servicio.Tests.Remoto;

public class AdaptadorWebRtcAudioTests
{
    private readonly Mock<ILogger> _loggerMock = new();

    /// <summary>
    /// Verifica que Disponible devuelve true despues de inicializar el adaptador.
    /// </summary>
    [Fact]
    public void Disponible_DespuesDeInicializar_DevuelveTrue()
    {
        // Arrange & Act
        using AdaptadorWebRtcAudio adaptador = new(_loggerMock.Object);

        // Assert
        adaptador.Disponible.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que Conectado devuelve false cuando no hay conexion activa.
    /// </summary>
    [Fact]
    public void Conectado_SinConexionActiva_DevuelveFalse()
    {
        // Arrange
        using AdaptadorWebRtcAudio adaptador = new(_loggerMock.Object);

        // Act
        bool conectado = adaptador.Conectado;

        // Assert
        conectado.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que ProcesarOfertaAsync genera una respuesta SDP valida con una oferta real.
    /// </summary>
    [Fact]
    public async Task ProcesarOfertaAsync_ConOfertaValida_GeneraRespuestaSdp()
    {
        // Arrange
        using AdaptadorWebRtcAudio adaptador = new(_loggerMock.Object);
        string? respuestaSdpRecibida = null;
        adaptador.RespuestaSdpGenerada += (object? remitente, string sdp) =>
        {
            respuestaSdpRecibida = sdp;
        };

        // Oferta SDP minima valida con audio
        string ofertaSdp = GenerarOfertaSdpMinima();

        // Act
        string? respuesta = await adaptador.ProcesarOfertaAsync(ofertaSdp);

        // Assert
        respuesta.Should().NotBeNullOrWhiteSpace();
        respuesta.Should().Contain("v=0");
        respuestaSdpRecibida.Should().Be(respuesta);
    }

    /// <summary>
    /// Verifica el ciclo de vida completo: crear, procesar oferta, detener, dispose.
    /// </summary>
    [Fact]
    public async Task CicloDeVida_CrearProcesarDetenerDispose_NoLanzaExcepcion()
    {
        // Arrange
        AdaptadorWebRtcAudio adaptador = new(_loggerMock.Object);
        string ofertaSdp = GenerarOfertaSdpMinima();

        // Act & Assert - no debe lanzar excepciones
        Func<Task> accion = async () =>
        {
            await adaptador.ProcesarOfertaAsync(ofertaSdp);
            await adaptador.DetenerAsync();
            adaptador.Dispose();
        };

        await accion.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifica que dispose doble no lanza excepcion.
    /// </summary>
    [Fact]
    public void Dispose_LlamadoDobleVez_NoLanzaExcepcion()
    {
        // Arrange
        AdaptadorWebRtcAudio adaptador = new(_loggerMock.Object);

        // Act
        Action accion = () =>
        {
            adaptador.Dispose();
            adaptador.Dispose();
        };

        // Assert
        accion.Should().NotThrow();
    }

    /// <summary>
    /// Verifica que el constructor lanza excepcion con logger nulo.
    /// </summary>
    [Fact]
    public void Constructor_ConLoggerNulo_LanzaArgumentNullException()
    {
        // Act
        Action accion = () => new AdaptadorWebRtcAudio(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifica que ProcesarOfertaAsync lanza excepcion con SDP nulo.
    /// </summary>
    [Fact]
    public async Task ProcesarOfertaAsync_ConSdpNulo_LanzaArgumentNullException()
    {
        // Arrange
        using AdaptadorWebRtcAudio adaptador = new(_loggerMock.Object);

        // Act
        Func<Task> accion = () => adaptador.ProcesarOfertaAsync(null!);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Verifica que ProcesarCandidatoIceAsync no lanza excepcion sin conexion activa.
    /// </summary>
    [Fact]
    public async Task ProcesarCandidatoIceAsync_SinConexionActiva_NoLanzaExcepcion()
    {
        // Arrange
        using AdaptadorWebRtcAudio adaptador = new(_loggerMock.Object);

        // Act
        Func<Task> accion = () => adaptador.ProcesarCandidatoIceAsync(
            "candidate:123 1 udp 2130706431 192.168.1.1 5000 typ host",
            "0",
            0);

        // Assert
        await accion.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifica que EnviarAudioPcm no lanza excepcion sin conexion activa.
    /// </summary>
    [Fact]
    public void EnviarAudioPcm_SinConexionActiva_NoLanzaExcepcion()
    {
        // Arrange
        using AdaptadorWebRtcAudio adaptador = new(_loggerMock.Object);
        short[] muestras = new short[160];

        // Act
        Action accion = () => adaptador.EnviarAudioPcm(muestras, 8000);

        // Assert
        accion.Should().NotThrow();
    }

    /// <summary>
    /// Genera una oferta SDP minima valida con audio para pruebas.
    /// </summary>
    private static string GenerarOfertaSdpMinima()
    {
        return
            "v=0\r\n" +
            "o=- 123456 2 IN IP4 127.0.0.1\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "m=audio 9 UDP/TLS/RTP/SAVPF 0\r\n" +
            "c=IN IP4 0.0.0.0\r\n" +
            "a=rtpmap:0 PCMU/8000\r\n" +
            "a=sendrecv\r\n" +
            "a=ice-ufrag:test\r\n" +
            "a=ice-pwd:testpasswordtestpassword\r\n" +
            "a=fingerprint:sha-256 AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99\r\n" +
            "a=setup:actpass\r\n" +
            "a=mid:0\r\n" +
            "a=rtcp-mux\r\n";
    }
}
