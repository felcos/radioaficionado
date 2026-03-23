using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.Api;
using RadioAficionado.Web.Api.Dtos;

namespace RadioAficionado.Web.Tests.Api;

/// <summary>
/// Tests unitarios para el controlador API de QSOs.
/// </summary>
public sealed class QsoApiControllerTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<IUnidadDeTrabajo> _mockUnidadDeTrabajo;
    private readonly Mock<ILogger<QsoApiController>> _mockLogger;
    private readonly QsoApiController _controlador;

    public QsoApiControllerTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockUnidadDeTrabajo = new Mock<IUnidadDeTrabajo>();
        _mockLogger = new Mock<ILogger<QsoApiController>>();

        _controlador = new QsoApiController(
            _mockRepositorio.Object,
            _mockUnidadDeTrabajo.Object,
            _mockLogger.Object);
    }

    private static Qso CrearQsoDePrueba(
        string indicativoPropio = "EA4ABC",
        string indicativoContacto = "W1AW",
        double frecuenciaMHz = 14.074,
        ModoOperacion modo = ModoOperacion.FT8)
    {
        return Qso.Crear(
            new Indicativo(indicativoPropio),
            new Indicativo(indicativoContacto),
            DateTimeOffset.UtcNow.AddHours(-1),
            Frecuencia.DesdeMHz(frecuenciaMHz),
            modo,
            "-10",
            potencia: 100);
    }

    private static QsoDto CrearQsoDtoDePrueba(
        string indicativoPropio = "EA4ABC",
        string indicativoContacto = "W1AW")
    {
        return new QsoDto
        {
            IndicativoPropio = indicativoPropio,
            IndicativoContacto = indicativoContacto,
            FechaHoraInicio = DateTimeOffset.UtcNow.AddHours(-1),
            FrecuenciaMHz = 14.074,
            Modo = "FT8",
            SenalEnviada = "-10",
            Potencia = 100
        };
    }

    // ──────────────────────────────────────────────────────────
    // GET / — Lista paginada
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ObtenerTodos_SinFiltros_RetornaPaginaConQsos()
    {
        // Arrange
        List<Qso> qsos = new() { CrearQsoDePrueba(), CrearQsoDePrueba(indicativoContacto: "DL1ABC") };
        ResultadoPaginado<Qso> resultadoPaginado = new(qsos.AsReadOnly(), 2);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        FiltroQsoDto filtro = new();

        // Act
        IActionResult resultado = await _controlador.ObtenerTodos(filtro, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ObtenerTodos_ConPaginaYTamano_UsaValoresCorrectos()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>().AsReadOnly(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(2, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        FiltroQsoDto filtro = new() { Pagina = 2, Tamano = 10 };

        // Act
        IActionResult resultado = await _controlador.ObtenerTodos(filtro, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
        _mockRepositorio.Verify(r => r.ObtenerPaginadoAsync(2, 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObtenerTodos_ConTamanoExcesivo_LimitaA200()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>().AsReadOnly(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 200, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        FiltroQsoDto filtro = new() { Tamano = 999 };

        // Act
        IActionResult resultado = await _controlador.ObtenerTodos(filtro, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
        _mockRepositorio.Verify(
            r => r.ObtenerPaginadoAsync(1, 200, It.IsAny<FiltroQso?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ──────────────────────────────────────────────────────────
    // GET /{id} — QSO por ID
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ObtenerPorId_QsoExiste_RetornaOkConDto()
    {
        // Arrange
        Qso qso = CrearQsoDePrueba();
        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        // Act
        IActionResult resultado = await _controlador.ObtenerPorId(qso.Id, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        QsoDto dto = okResult.Value.Should().BeOfType<QsoDto>().Subject;
        dto.IndicativoContacto.Should().Be("W1AW");
    }

    [Fact]
    public async Task ObtenerPorId_QsoNoExiste_Retorna404()
    {
        // Arrange
        Guid idInexistente = Guid.NewGuid();
        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(idInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Qso?)null);

        // Act
        IActionResult resultado = await _controlador.ObtenerPorId(idInexistente, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    // ──────────────────────────────────────────────────────────
    // POST / — Crear QSO
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Crear_DtoValido_RetornaCreatedConQso()
    {
        // Arrange
        QsoDto dto = CrearQsoDtoDePrueba();

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        IActionResult resultado = await _controlador.Crear(dto, CancellationToken.None);

        // Assert
        CreatedAtActionResult createdResult = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        _mockRepositorio.Verify(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnidadDeTrabajo.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_DtoConIndicativoInvalido_Retorna400()
    {
        // Arrange
        QsoDto dto = CrearQsoDtoDePrueba();
        dto.IndicativoContacto = "INVALIDO!!!";

        // Act
        IActionResult resultado = await _controlador.Crear(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    // ──────────────────────────────────────────────────────────
    // POST /sincronizar — Sincronización batch
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Sincronizar_QsosNuevos_InsertaTodosYRetornaContadores()
    {
        // Arrange
        List<QsoDto> dtos = new()
        {
            CrearQsoDtoDePrueba(indicativoContacto: "W1AW"),
            CrearQsoDtoDePrueba(indicativoContacto: "DL1ABC")
        };

        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(), It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IActionResult resultado = await _controlador.Sincronizar(dtos, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ResultadoSincronizacionDto sincResult = okResult.Value.Should().BeOfType<ResultadoSincronizacionDto>().Subject;
        sincResult.QsosRecibidos.Should().Be(2);
        sincResult.QsosNuevos.Should().Be(2);
        sincResult.QsosDuplicados.Should().Be(0);
        sincResult.Errores.Should().BeEmpty();
    }

    [Fact]
    public async Task Sincronizar_ConDuplicados_DetectaYReportaDuplicados()
    {
        // Arrange
        List<QsoDto> dtos = new()
        {
            CrearQsoDtoDePrueba(indicativoContacto: "W1AW"),
            CrearQsoDtoDePrueba(indicativoContacto: "DL1ABC")
        };

        int llamada = 0;
        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(), It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                llamada++;
                return llamada == 1; // Primer QSO es duplicado
            });

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        IActionResult resultado = await _controlador.Sincronizar(dtos, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ResultadoSincronizacionDto sincResult = okResult.Value.Should().BeOfType<ResultadoSincronizacionDto>().Subject;
        sincResult.QsosRecibidos.Should().Be(2);
        sincResult.QsosDuplicados.Should().Be(1);
        sincResult.QsosNuevos.Should().Be(1);
    }

    [Fact]
    public async Task Sincronizar_ListaVacia_Retorna400()
    {
        // Arrange
        List<QsoDto> dtos = new();

        // Act
        IActionResult resultado = await _controlador.Sincronizar(dtos, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Sincronizar_ConQsoInvalido_RegistraErrorSinDetenerProceso()
    {
        // Arrange
        QsoDto dtoValido = CrearQsoDtoDePrueba(indicativoContacto: "W1AW");
        QsoDto dtoInvalido = CrearQsoDtoDePrueba();
        dtoInvalido.IndicativoContacto = "!!INVALIDO!!";

        List<QsoDto> dtos = new() { dtoInvalido, dtoValido };

        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(), It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        IActionResult resultado = await _controlador.Sincronizar(dtos, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ResultadoSincronizacionDto sincResult = okResult.Value.Should().BeOfType<ResultadoSincronizacionDto>().Subject;
        sincResult.QsosRecibidos.Should().Be(2);
        sincResult.QsosNuevos.Should().Be(1);
        sincResult.Errores.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────
    // DELETE /{id} — Eliminar QSO
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_QsoExiste_RetornaNoContent()
    {
        // Arrange
        Qso qso = CrearQsoDePrueba();
        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        _mockRepositorio
            .Setup(r => r.EliminarAsync(qso, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        IActionResult resultado = await _controlador.Eliminar(qso.Id, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _mockRepositorio.Verify(r => r.EliminarAsync(qso, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnidadDeTrabajo.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Eliminar_QsoNoExiste_Retorna404()
    {
        // Arrange
        Guid idInexistente = Guid.NewGuid();
        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(idInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Qso?)null);

        // Act
        IActionResult resultado = await _controlador.Eliminar(idInexistente, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        _mockRepositorio.Verify(r => r.EliminarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
