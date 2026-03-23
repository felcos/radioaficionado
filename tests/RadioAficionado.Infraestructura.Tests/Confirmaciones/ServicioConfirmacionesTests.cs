using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Confirmaciones;

namespace RadioAficionado.Infraestructura.Tests.Confirmaciones;

/// <summary>
/// Tests unitarios para el servicio orquestador de confirmaciones.
/// </summary>
public class ServicioConfirmacionesTests
{
    private readonly Mock<IClienteLoTW> _mockLoTW;
    private readonly Mock<IClienteEQsl> _mockEQsl;
    private readonly Mock<IClienteClubLog> _mockClubLog;
    private readonly ConfiguracionLoTW _configLoTW;
    private readonly ConfiguracionEQsl _configEQsl;
    private readonly ConfiguracionClubLog _configClubLog;
    private readonly ServicioConfirmaciones _servicio;

    public ServicioConfirmacionesTests()
    {
        _mockLoTW = new Mock<IClienteLoTW>();
        _mockEQsl = new Mock<IClienteEQsl>();
        _mockClubLog = new Mock<IClienteClubLog>();

        _configLoTW = new ConfiguracionLoTW { Usuario = "EA4ABC", Password = "pass123" };
        _configEQsl = new ConfiguracionEQsl { Usuario = "EA4ABC", Password = "pass456" };
        _configClubLog = new ConfiguracionClubLog
        {
            Email = "test@test.com",
            Password = "pass789",
            Indicativo = "EA4ABC",
            ApiKey = "api-key-123"
        };

        Mock<ILogger<ServicioConfirmaciones>> mockLogger = new();

        _servicio = new ServicioConfirmaciones(
            _mockLoTW.Object,
            _mockEQsl.Object,
            _mockClubLog.Object,
            _configLoTW,
            _configEQsl,
            _configClubLog,
            mockLogger.Object);
    }

    [Fact]
    public async Task SubirQsosAsync_ConListaVacia_DebeRetornarNoExitoso()
    {
        // Arrange
        List<Qso> qsos = new();

        // Act
        ResultadoSubida resultado = await _servicio.SubirQsosAsync(qsos, ServicioExterno.LoTW);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("No hay QSOs");
    }

    [Fact]
    public async Task SubirQsosAsync_ALoTW_DebeDelegarAlClienteLoTW()
    {
        // Arrange
        Qso qso = CrearQsoDePrueba();
        List<Qso> qsos = new() { qso };

        ResultadoSubida resultadoEsperado = new(true, 1, 0, "OK", ServicioExterno.LoTW);
        _mockLoTW.Setup(c => c.SubirAdifAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);

        // Act
        ResultadoSubida resultado = await _servicio.SubirQsosAsync(qsos, ServicioExterno.LoTW);

        // Assert
        resultado.Should().Be(resultadoEsperado);
        _mockLoTW.Verify(c => c.SubirAdifAsync(It.Is<string>(s => s.Contains("<CALL:")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubirQsosAsync_AEQsl_DebeDelegarAlClienteEQsl()
    {
        // Arrange
        Qso qso = CrearQsoDePrueba();
        List<Qso> qsos = new() { qso };

        ResultadoSubida resultadoEsperado = new(true, 1, 0, "OK", ServicioExterno.EQsl);
        _mockEQsl.Setup(c => c.SubirAdifAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);

        // Act
        ResultadoSubida resultado = await _servicio.SubirQsosAsync(qsos, ServicioExterno.EQsl);

        // Assert
        resultado.Should().Be(resultadoEsperado);
        _mockEQsl.Verify(c => c.SubirAdifAsync(
            It.IsAny<string>(),
            "EA4ABC",
            "pass456",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubirQsosAsync_AClubLog_DebeDelegarAlClienteClubLog()
    {
        // Arrange
        Qso qso = CrearQsoDePrueba();
        List<Qso> qsos = new() { qso };

        ResultadoSubida resultadoEsperado = new(true, 1, 0, "OK", ServicioExterno.ClubLog);
        _mockClubLog.Setup(c => c.SubirAdifAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);

        // Act
        ResultadoSubida resultado = await _servicio.SubirQsosAsync(qsos, ServicioExterno.ClubLog);

        // Assert
        resultado.Should().Be(resultadoEsperado);
        _mockClubLog.Verify(c => c.SubirAdifAsync(
            It.IsAny<string>(),
            "test@test.com",
            "pass789",
            "EA4ABC",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObtenerEstadoAsync_ConLoTWConfigurado_DebeRetornarTrue()
    {
        // Act
        bool resultado = await _servicio.ObtenerEstadoAsync(ServicioExterno.LoTW);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerEstadoAsync_ConLoTWSinConfigurar_DebeRetornarFalse()
    {
        // Arrange
        ConfiguracionLoTW configVacia = new();
        Mock<ILogger<ServicioConfirmaciones>> mockLogger = new();

        ServicioConfirmaciones servicio = new(
            _mockLoTW.Object, _mockEQsl.Object, _mockClubLog.Object,
            configVacia, _configEQsl, _configClubLog, mockLogger.Object);

        // Act
        bool resultado = await servicio.ObtenerEstadoAsync(ServicioExterno.LoTW);

        // Assert
        resultado.Should().BeFalse();
    }

    /// <summary>
    /// Crea un QSO de prueba válido para usar en los tests.
    /// </summary>
    private static Qso CrearQsoDePrueba()
    {
        Indicativo indicativoPropio = new("EA4ABC");
        Indicativo indicativoContacto = new("W1AW");
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.250);
        DateTimeOffset fechaInicio = DateTimeOffset.UtcNow.AddHours(-1);

        return Qso.Crear(indicativoPropio, indicativoContacto, fechaInicio, frecuencia, ModoOperacion.SSB, "59");
    }
}
