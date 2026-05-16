using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Servicio.Controllers;

namespace RadioAficionado.Servicio.Tests.Controllers;

/// <summary>
/// Tests unitarios para ContestApiController.
/// Verifica estadisticas con datos reales, fallback a datos de ejemplo y respuesta sin contest.
/// </summary>
public sealed class ContestApiControllerTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly ContestApiController _controlador;

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ContestApiControllerTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _controlador = new ContestApiController(_mockRepositorio.Object);
    }

    [Fact]
    public async Task ObtenerEstadisticasAsync_SinContest_DevuelveInactivo()
    {
        // Arrange — sin parametro contest

        // Act
        IActionResult resultado = await _controlador.ObtenerEstadisticasAsync(null, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        JsonElement datos = ConvertirARespuesta(okResult.Value!);
        datos.GetProperty("activo").GetBoolean().Should().BeFalse();
        datos.GetProperty("contest").GetString().Should().Be("Ninguno activo");
        datos.GetProperty("qsos").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task ObtenerEstadisticasAsync_ConContestSinQsos_DevuelveDatosEjemplo()
    {
        // Arrange
        IReadOnlyList<Qso> listaVacia = Array.Empty<Qso>();
        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(listaVacia);

        // Act
        IActionResult resultado = await _controlador.ObtenerEstadisticasAsync("cqww-ssb", CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        JsonElement datos = ConvertirARespuesta(okResult.Value!);
        datos.GetProperty("activo").GetBoolean().Should().BeTrue();
        datos.GetProperty("contest").GetString().Should().Be("CQ WW SSB 2026");
        datos.GetProperty("qsos").GetInt32().Should().Be(347);
        datos.GetProperty("rate").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task ObtenerEstadisticasAsync_ConQsosRecientes_DevuelveDatosReales()
    {
        // Arrange — crear QSOs en la ultima hora en 20m (14.074 MHz)
        List<Qso> qsos = new()
        {
            CrearQso("EA4TEST", "K1ABC", DateTimeOffset.UtcNow.AddMinutes(-30), 14.074),
            CrearQso("EA4TEST", "JA1XYZ", DateTimeOffset.UtcNow.AddMinutes(-20), 14.074),
            CrearQso("EA4TEST", "PY2ABC", DateTimeOffset.UtcNow.AddMinutes(-10), 14.074),
        };

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos.AsReadOnly());

        // Act
        IActionResult resultado = await _controlador.ObtenerEstadisticasAsync("cqww-ssb", CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        JsonElement datos = ConvertirARespuesta(okResult.Value!);
        datos.GetProperty("activo").GetBoolean().Should().BeTrue();
        datos.GetProperty("qsos").GetInt32().Should().Be(3);
        datos.GetProperty("rate").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task ObtenerEstadisticasAsync_QsosAntiguos_DevuelveDatosEjemplo()
    {
        // Arrange — QSOs de hace 3 dias, fuera de las 48 horas
        List<Qso> qsos = new()
        {
            CrearQso("EA4TEST", "K1ABC", DateTimeOffset.UtcNow.AddHours(-72), 14.074),
        };

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos.AsReadOnly());

        // Act
        IActionResult resultado = await _controlador.ObtenerEstadisticasAsync("cqww-cw", CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        JsonElement datos = ConvertirARespuesta(okResult.Value!);
        datos.GetProperty("qsos").GetInt32().Should().Be(347);
    }

    [Fact]
    public async Task ObtenerEstadisticasAsync_MultipleBandas_AgrupaCorrectamente()
    {
        // Arrange — QSOs en 20m y 40m
        List<Qso> qsos = new()
        {
            CrearQso("EA4TEST", "K1ABC", DateTimeOffset.UtcNow.AddMinutes(-30), 14.074),
            CrearQso("EA4TEST", "W2DEF", DateTimeOffset.UtcNow.AddMinutes(-25), 14.074),
            CrearQso("EA4TEST", "DL1GHI", DateTimeOffset.UtcNow.AddMinutes(-20), 7.074),
        };

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos.AsReadOnly());

        // Act
        IActionResult resultado = await _controlador.ObtenerEstadisticasAsync("cqww-ssb", CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        JsonElement datos = ConvertirARespuesta(okResult.Value!);
        datos.GetProperty("qsos").GetInt32().Should().Be(3);
        datos.GetProperty("multiplicadores").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ObtenerEstadisticasAsync_ScoreEsPuntosPorMultiplicadores()
    {
        // Arrange — 2 QSOs de distinto continente (3 pts cada uno) en 20m con 2 DXCC distintos
        List<Qso> qsos = new()
        {
            CrearQso("EA4TEST", "K1ABC", DateTimeOffset.UtcNow.AddMinutes(-30), 14.074),
            CrearQso("EA4TEST", "JA1XYZ", DateTimeOffset.UtcNow.AddMinutes(-20), 14.074),
        };

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos.AsReadOnly());

        // Act
        IActionResult resultado = await _controlador.ObtenerEstadisticasAsync("cqww-ssb", CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        JsonElement datos = ConvertirARespuesta(okResult.Value!);

        int puntos = datos.GetProperty("puntos").GetInt32();
        int multiplicadores = datos.GetProperty("multiplicadores").GetInt32();
        long score = datos.GetProperty("score").GetInt64();

        score.Should().Be((long)puntos * multiplicadores);
    }

    [Fact]
    public void Constructor_ConRepositorioNull_LanzaArgumentNullException()
    {
        // Arrange & Act
        Action accion = () => new ContestApiController(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("repositorioQso");
    }

    [Theory]
    [InlineData("cqww-ssb", "CQ WW SSB 2026")]
    [InlineData("cqww-cw", "CQ WW CW 2026")]
    [InlineData("cqwpx-ssb", "CQ WPX SSB 2026")]
    [InlineData("arrl-dx-ssb", "ARRL DX SSB 2026")]
    [InlineData("iaru-hf", "IARU HF Championship 2026")]
    public async Task ObtenerEstadisticasAsync_NombresDeContestCorrectos(string idContest, string nombreEsperado)
    {
        // Arrange
        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Qso>());

        // Act
        IActionResult resultado = await _controlador.ObtenerEstadisticasAsync(idContest, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        JsonElement datos = ConvertirARespuesta(okResult.Value!);
        datos.GetProperty("contest").GetString().Should().Be(nombreEsperado);
    }

    [Fact]
    public async Task ObtenerEstadisticasAsync_RateCalculaUltimaHora()
    {
        // Arrange — 1 QSO en la ultima hora, 2 de hace 2 horas
        List<Qso> qsos = new()
        {
            CrearQso("EA4TEST", "K1ABC", DateTimeOffset.UtcNow.AddMinutes(-30), 14.074),
            CrearQso("EA4TEST", "W2DEF", DateTimeOffset.UtcNow.AddHours(-2), 14.074),
            CrearQso("EA4TEST", "DL1GHI", DateTimeOffset.UtcNow.AddHours(-3), 14.074),
        };

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos.AsReadOnly());

        // Act
        IActionResult resultado = await _controlador.ObtenerEstadisticasAsync("cqww-ssb", CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        JsonElement datos = ConvertirARespuesta(okResult.Value!);
        datos.GetProperty("rate").GetInt32().Should().Be(1);
        datos.GetProperty("qsos").GetInt32().Should().Be(3);
    }

    /// <summary>
    /// Convierte un objeto anonimo a JsonElement para poder inspeccionar sus propiedades
    /// desde otro assembly (los tipos anonimos son internos).
    /// </summary>
    private static JsonElement ConvertirARespuesta(object valor)
    {
        string json = JsonSerializer.Serialize(valor, OpcionesJson);
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Crea un QSO de prueba con indicativos y frecuencia especificados.
    /// </summary>
    private static Qso CrearQso(string indicativoPropio, string indicativoContacto, DateTimeOffset fecha, double frecuenciaMhz)
    {
        Indicativo indPropio = new(indicativoPropio);
        Indicativo indContacto = new(indicativoContacto);

        return Qso.Crear(
            indPropio,
            indContacto,
            fecha,
            Frecuencia.DesdeMHz(frecuenciaMhz),
            ModoOperacion.SSB,
            "59");
    }
}
