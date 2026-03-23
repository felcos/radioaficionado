using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Sincronizacion;

namespace RadioAficionado.Infraestructura.Tests.Sincronizacion;

/// <summary>
/// Tests unitarios para el servicio de sincronización de QSOs.
/// Verifica formato de requests, manejo de respuestas, conflictos y errores.
/// </summary>
public class ServicioSincronizacionTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<ILogger<ServicioSincronizacion>> _mockLogger;
    private readonly ServicioSincronizacion _servicio;

    /// <summary>
    /// Configuración de prueba compartida por todos los tests.
    /// </summary>
    private static readonly ConfiguracionSincronizacion ConfiguracionPrueba = new(
        UrlServidor: "https://test.radioaficionado.com",
        Token: "token-de-prueba-123",
        IndicativoPropio: "EA4TEST",
        SincronizacionAutomatica: false,
        IntervaloMinutos: 15);

    public ServicioSincronizacionTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object);
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockLogger = new Mock<ILogger<ServicioSincronizacion>>();
        _servicio = new ServicioSincronizacion(_httpClient, _mockRepositorio.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SincronizarAsync_SinConfigurar_DebeRetornarErrorDeConfiguracion()
    {
        // Arrange — servicio sin configurar

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.QsosEnviados.Should().Be(0);
        resultado.QsosRecibidos.Should().Be(0);
        resultado.Errores.Should().ContainSingle()
            .Which.Should().Contain("no está configurado");
    }

    [Fact]
    public async Task SincronizarAsync_ConServidorExitoso_DebeRetornarConteosCorrrectos()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        RespuestaSincronizacion respuestaServidor = new()
        {
            QsosParaCliente = [],
            IdsAceptados = [],
            IdsDuplicados = [],
            Errores = []
        };

        ConfigurarRespuestaHttp(HttpStatusCode.OK, respuestaServidor);

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.QsosEnviados.Should().Be(0);
        resultado.QsosRecibidos.Should().Be(0);
        resultado.QsosDuplicados.Should().Be(0);
        resultado.Errores.Should().BeEmpty();
        resultado.FechaSincronizacion.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SincronizarAsync_ConQsosAceptados_DebeContarEnviados()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();

        RespuestaSincronizacion respuestaServidor = new()
        {
            QsosParaCliente = [],
            IdsAceptados = [id1, id2],
            IdsDuplicados = [],
            Errores = []
        };

        ConfigurarRespuestaHttp(HttpStatusCode.OK, respuestaServidor);

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.QsosEnviados.Should().Be(2);
    }

    [Fact]
    public async Task SincronizarAsync_ConDuplicados_DebeContarDuplicados()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        RespuestaSincronizacion respuestaServidor = new()
        {
            QsosParaCliente = [],
            IdsAceptados = [],
            IdsDuplicados = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
            Errores = []
        };

        ConfigurarRespuestaHttp(HttpStatusCode.OK, respuestaServidor);

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.QsosDuplicados.Should().Be(3);
    }

    [Fact]
    public async Task SincronizarAsync_ConErrorHttp_DebeRetornarError()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        ConfigurarRespuestaHttpConTexto(HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.QsosEnviados.Should().Be(0);
        resultado.Errores.Should().ContainSingle()
            .Which.Should().Contain("500");
    }

    [Fact]
    public async Task SincronizarAsync_ConErrorDeConexion_DebeRetornarError()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("No se puede conectar al servidor"));

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.QsosEnviados.Should().Be(0);
        resultado.Errores.Should().ContainSingle()
            .Which.Should().Contain("conexión");
    }

    [Fact]
    public async Task SincronizarAsync_ConQsosRecibidosDelServidor_DebeContarRecibidos()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        _mockRepositorio.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Qso?)null);

        RespuestaSincronizacion respuestaServidor = new()
        {
            QsosParaCliente =
            [
                new QsoSincronizacionDto
                {
                    Id = Guid.NewGuid(),
                    IndicativoPropio = "EA4TEST",
                    IndicativoContacto = "DL1ABC",
                    FechaHoraInicio = DateTimeOffset.UtcNow.AddHours(-2),
                    FrecuenciaMhz = 14.074,
                    Modo = "FT8",
                    SenalEnviada = "-10",
                    SenalRecibida = "-15",
                    FechaCreacion = DateTimeOffset.UtcNow.AddHours(-2)
                },
                new QsoSincronizacionDto
                {
                    Id = Guid.NewGuid(),
                    IndicativoPropio = "EA4TEST",
                    IndicativoContacto = "G3XYZ",
                    FechaHoraInicio = DateTimeOffset.UtcNow.AddHours(-1),
                    FrecuenciaMhz = 7.074,
                    Modo = "FT8",
                    SenalEnviada = "-08",
                    SenalRecibida = "-12",
                    FechaCreacion = DateTimeOffset.UtcNow.AddHours(-1)
                }
            ],
            IdsAceptados = [],
            IdsDuplicados = [],
            Errores = []
        };

        ConfigurarRespuestaHttp(HttpStatusCode.OK, respuestaServidor);

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.QsosRecibidos.Should().Be(2);
    }

    [Fact]
    public async Task SincronizarAsync_ConErroresParciales_DebeIncluirErroresEnResultado()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        RespuestaSincronizacion respuestaServidor = new()
        {
            QsosParaCliente = [],
            IdsAceptados = [Guid.NewGuid()],
            IdsDuplicados = [],
            Errores = ["QSO con ID inválido", "Frecuencia fuera de rango"]
        };

        ConfigurarRespuestaHttp(HttpStatusCode.OK, respuestaServidor);

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.QsosEnviados.Should().Be(1);
        resultado.Errores.Should().HaveCount(2);
        resultado.Errores.Should().Contain("QSO con ID inválido");
        resultado.Errores.Should().Contain("Frecuencia fuera de rango");
    }

    [Fact]
    public async Task ConfigurarAsync_ConUrlVacia_DebeLanzarExcepcion()
    {
        // Arrange
        ConfiguracionSincronizacion configuracionInvalida = new(
            UrlServidor: "",
            Token: "token",
            IndicativoPropio: "EA4TEST");

        // Act
        Func<Task> accion = () => _servicio.ConfigurarAsync(configuracionInvalida);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*URL*");
    }

    [Fact]
    public async Task ConfigurarAsync_ConTokenVacio_DebeLanzarExcepcion()
    {
        // Arrange
        ConfiguracionSincronizacion configuracionInvalida = new(
            UrlServidor: "https://test.com",
            Token: "",
            IndicativoPropio: "EA4TEST");

        // Act
        Func<Task> accion = () => _servicio.ConfigurarAsync(configuracionInvalida);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*token*");
    }

    [Fact]
    public async Task ConfigurarAsync_ConIntervaloMenorAUno_DebeLanzarExcepcion()
    {
        // Arrange
        ConfiguracionSincronizacion configuracionInvalida = new(
            UrlServidor: "https://test.com",
            Token: "token",
            IndicativoPropio: "EA4TEST",
            IntervaloMinutos: 0);

        // Act
        Func<Task> accion = () => _servicio.ConfigurarAsync(configuracionInvalida);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*intervalo*");
    }

    [Fact]
    public async Task ObtenerEstadoAsync_SinConfigurar_DebeRetornarEstadoVacio()
    {
        // Arrange — servicio sin configurar

        // Act
        EstadoSincronizacion estado = await _servicio.ObtenerEstadoAsync();

        // Assert
        estado.UltimaSincronizacion.Should().BeNull();
        estado.ConexionActiva.Should().BeFalse();
        estado.QsosPendientesSincronizar.Should().Be(0);
    }

    [Fact]
    public async Task SincronizarAsync_ConRespuestaNoDeserializable_DebeRetornarError()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        ConfigurarRespuestaHttpConTexto(HttpStatusCode.OK, "esto no es JSON válido {{{");

        // Act
        ResultadoSincronizacion resultado = await _servicio.SincronizarAsync();

        // Assert
        resultado.Errores.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SincronizarAsync_DebeEnviarTokenDeAutorizacion()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        HttpRequestMessage? peticionCapturada = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => peticionCapturada = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RespuestaSincronizacion())
            });

        // Act
        await _servicio.SincronizarAsync();

        // Assert
        peticionCapturada.Should().NotBeNull();
        peticionCapturada!.Headers.Authorization.Should().NotBeNull();
        peticionCapturada.Headers.Authorization!.Scheme.Should().Be("Bearer");
        peticionCapturada.Headers.Authorization.Parameter.Should().Be("token-de-prueba-123");
    }

    [Fact]
    public async Task SincronizarAsync_DebeEnviarAlEndpointCorrecto()
    {
        // Arrange
        await _servicio.ConfigurarAsync(ConfiguracionPrueba);

        _mockRepositorio.Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        HttpRequestMessage? peticionCapturada = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => peticionCapturada = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RespuestaSincronizacion())
            });

        // Act
        await _servicio.SincronizarAsync();

        // Assert
        peticionCapturada.Should().NotBeNull();
        peticionCapturada!.RequestUri.Should().NotBeNull();
        peticionCapturada.RequestUri!.ToString().Should().Be("https://test.radioaficionado.com/api/qsos/sincronizar");
        peticionCapturada.Method.Should().Be(HttpMethod.Post);
    }

    /// <summary>
    /// Configura el mock del HttpMessageHandler para devolver una respuesta JSON serializada.
    /// </summary>
    private void ConfigurarRespuestaHttp<T>(HttpStatusCode codigoEstado, T contenido)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(codigoEstado)
            {
                Content = JsonContent.Create(contenido)
            });
    }

    /// <summary>
    /// Configura el mock del HttpMessageHandler para devolver una respuesta con texto plano.
    /// </summary>
    private void ConfigurarRespuestaHttpConTexto(HttpStatusCode codigoEstado, string contenido)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(codigoEstado)
            {
                Content = new StringContent(contenido)
            });
    }

    /// <summary>
    /// Libera recursos del servicio y del HttpClient.
    /// </summary>
    public void Dispose()
    {
        _servicio.Dispose();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
