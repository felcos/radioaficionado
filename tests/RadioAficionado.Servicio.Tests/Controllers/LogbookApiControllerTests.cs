using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Servicio.Controllers;
using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Tests.Controllers;

/// <summary>
/// Tests unitarios para LogbookApiController.
/// Verifica paginacion, filtrado y correccion de parametros invalidos.
/// </summary>
public sealed class LogbookApiControllerTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<IUnidadDeTrabajo> _mockUnidadDeTrabajo;
    private readonly Mock<ILogger<LogbookApiController>> _mockLogger;
    private readonly LogbookApiController _controlador;

    public LogbookApiControllerTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockUnidadDeTrabajo = new Mock<IUnidadDeTrabajo>();
        _mockLogger = new Mock<ILogger<LogbookApiController>>();
        _controlador = new LogbookApiController(
            _mockRepositorio.Object,
            _mockUnidadDeTrabajo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ObtenerQsos_SinFiltro_RetornaOkObjectResultConDatosPaginados()
    {
        // Arrange
        Qso qso = CrearQsoDePrueba("EA4ABC", "W1AW");
        List<Qso> elementos = [qso];
        ResultadoPaginado<Qso> resultadoPaginado = new(elementos, 1);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.ObtenerQsos();

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        // Verificar que el repositorio fue invocado con los parametros correctos
        _mockRepositorio.Verify(
            r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObtenerQsos_ConBusqueda_FiltraConIndicativoEnMayusculas()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new([], 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1,
                50,
                It.Is<FiltroQso>(f => f != null && f.Indicativo == "EA4ABC"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.ObtenerQsos(busqueda: "ea4abc");

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();

        _mockRepositorio.Verify(
            r => r.ObtenerPaginadoAsync(
                1,
                50,
                It.Is<FiltroQso>(f => f != null && f.Indicativo == "EA4ABC"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObtenerQsos_ConBusquedaConEspacios_RecortaYConvierteAMayusculas()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new([], 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1,
                50,
                It.Is<FiltroQso>(f => f != null && f.Indicativo == "W1AW"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.ObtenerQsos(busqueda: "  w1aw  ");

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();

        _mockRepositorio.Verify(
            r => r.ObtenerPaginadoAsync(
                1,
                50,
                It.Is<FiltroQso>(f => f != null && f.Indicativo == "W1AW"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObtenerQsos_PaginaMenorQueUno_SeCorrigeAUno()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new([], 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.ObtenerQsos(pagina: -5);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();

        _mockRepositorio.Verify(
            r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObtenerQsos_PorPaginaMayorQue200_SeCorrigeA50()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new([], 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.ObtenerQsos(porPagina: 500);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();

        _mockRepositorio.Verify(
            r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObtenerQsos_PorPaginaMenorQueUno_SeCorrigeA50()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new([], 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.ObtenerQsos(porPagina: 0);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();

        _mockRepositorio.Verify(
            r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObtenerQsos_ConPaginacionValida_UsaParametrosOriginales()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new([], 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(3, 25, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.ObtenerQsos(pagina: 3, porPagina: 25);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();

        _mockRepositorio.Verify(
            r => r.ObtenerPaginadoAsync(3, 25, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ObtenerQsos_ConResultados_RespuestaContieneTotalYQsos()
    {
        // Arrange
        Qso qso1 = CrearQsoDePrueba("EA4ABC", "W1AW");
        Qso qso2 = CrearQsoDePrueba("EA4ABC", "DL1ABC");
        List<Qso> elementos = [qso1, qso2];
        ResultadoPaginado<Qso> resultadoPaginado = new(elementos, 15);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.ObtenerQsos();

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        // Verificamos que el valor no es nulo y tiene la estructura esperada
        object? valor = okResult.Value;
        valor.Should().NotBeNull();

        // Usamos reflexion para verificar la estructura del anonimo
        int total = (int)valor!.GetType().GetProperty("total")!.GetValue(valor)!;
        total.Should().Be(15);
    }

    [Fact]
    public void Constructor_ConRepositorioNulo_LanzaArgumentNullException()
    {
        // Arrange
        Mock<IUnidadDeTrabajo> mockUdt = new();
        Mock<ILogger<LogbookApiController>> mockLog = new();

        // Act
        Action accion = () => new LogbookApiController(null!, mockUdt.Object, mockLog.Object);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("repositorioQso");
    }

    [Fact]
    public void Constructor_ConUnidadDeTrabajoNula_LanzaArgumentNullException()
    {
        // Arrange
        Mock<IRepositorioQso> mockRepo = new();
        Mock<ILogger<LogbookApiController>> mockLog = new();

        // Act
        Action accion = () => new LogbookApiController(mockRepo.Object, null!, mockLog.Object);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("unidadDeTrabajo");
    }

    [Fact]
    public void Constructor_ConLoggerNulo_LanzaArgumentNullException()
    {
        // Arrange
        Mock<IRepositorioQso> mockRepo = new();
        Mock<IUnidadDeTrabajo> mockUdt = new();

        // Act
        Action accion = () => new LogbookApiController(mockRepo.Object, mockUdt.Object, null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ImportarAdif_ArchivoNulo_RetornaBadRequest()
    {
        // Arrange — archivo nulo

        // Act
        IActionResult resultado = await _controlador.ImportarAdif(null!, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ImportarAdif_ExtensionInvalida_RetornaBadRequest()
    {
        // Arrange
        Mock<IFormFile> mockArchivo = CrearMockFormFile("archivo.txt", "contenido");

        // Act
        IActionResult resultado = await _controlador.ImportarAdif(mockArchivo.Object, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ImportarAdif_ArchivoAdifValido_ImportaQsosYRetornaOk()
    {
        // Arrange
        string contenidoAdif =
            "<CALL:4>W1AW <QSO_DATE:8>20250101 <TIME_ON:4>1200 <BAND:3>20m <MODE:3>SSB <RST_SENT:2>59 <RST_RCVD:2>59 <FREQ:6>14.200 <EOR>\n" +
            "<CALL:5>DL1AB <QSO_DATE:8>20250102 <TIME_ON:4>1300 <BAND:3>40m <MODE:2>CW <RST_SENT:3>599 <RST_RCVD:3>599 <FREQ:5>7.010 <EOR>";

        Mock<IFormFile> mockArchivo = CrearMockFormFile("logbook.adi", contenidoAdif);

        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(),
                It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IActionResult resultado = await _controlador.ImportarAdif(mockArchivo.Object, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        object? valor = okResult.Value;
        valor.Should().NotBeNull();

        int importados = (int)valor!.GetType().GetProperty("importados")!.GetValue(valor)!;
        importados.Should().Be(2);

        _mockRepositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _mockUnidadDeTrabajo.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportarAdif_ConDuplicados_NoInsertaDuplicadosYReportaContadores()
    {
        // Arrange
        string contenidoAdif =
            "<CALL:4>W1AW <QSO_DATE:8>20250101 <TIME_ON:4>1200 <BAND:3>20m <MODE:3>SSB <RST_SENT:2>59 <FREQ:6>14.200 <EOR>";

        Mock<IFormFile> mockArchivo = CrearMockFormFile("logbook.adi", contenidoAdif);

        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(),
                It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        IActionResult resultado = await _controlador.ImportarAdif(mockArchivo.Object, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        object? valor = okResult.Value;
        valor.Should().NotBeNull();

        int importados = (int)valor!.GetType().GetProperty("importados")!.GetValue(valor)!;
        importados.Should().Be(0);

        int erroresCount = (int)valor.GetType().GetProperty("errores")!.GetValue(valor)!;
        erroresCount.Should().Be(1); // 1 duplicado

        _mockRepositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockUnidadDeTrabajo.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportarAdif_ConRegistroInvalido_CuentaComoErrorYContinua()
    {
        // Arrange — un registro sin CALL (invalido) y uno valido
        string contenidoAdif =
            "<QSO_DATE:8>20250101 <TIME_ON:4>1200 <BAND:3>20m <MODE:3>SSB <RST_SENT:2>59 <EOR>\n" +
            "<CALL:4>W1AW <QSO_DATE:8>20250102 <TIME_ON:4>1300 <BAND:3>20m <MODE:3>SSB <RST_SENT:2>59 <FREQ:6>14.200 <EOR>";

        Mock<IFormFile> mockArchivo = CrearMockFormFile("logbook.adif", contenidoAdif);

        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(),
                It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        IActionResult resultado = await _controlador.ImportarAdif(mockArchivo.Object, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        object? valor = okResult.Value;
        valor.Should().NotBeNull();

        int importados = (int)valor!.GetType().GetProperty("importados")!.GetValue(valor)!;
        importados.Should().Be(1);

        // El registro sin CALL genera 1 error
        int erroresCount = (int)valor.GetType().GetProperty("errores")!.GetValue(valor)!;
        erroresCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ImportarAdif_ArchivoVacio_RetornaBadRequest()
    {
        // Arrange
        Mock<IFormFile> mockArchivo = new();
        mockArchivo.Setup(f => f.Length).Returns(0);
        mockArchivo.Setup(f => f.FileName).Returns("vacio.adi");

        // Act
        IActionResult resultado = await _controlador.ImportarAdif(mockArchivo.Object, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    // ================================================================
    // Tests de RegistrarQso
    // ================================================================

    [Fact]
    public async Task RegistrarQso_DatosValidos_RetornaOkConId()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "EA4ABC",
            FrecuenciaHz: 14074000,
            Modo: "FT8",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: "IN80",
            Nombre: "Juan",
            Comentario: "Buen contacto");

        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(),
                It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        OkObjectResult okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
        object? valor = okResult.Value;
        valor.Should().NotBeNull();

        Guid id = (Guid)valor!.GetType().GetProperty("id")!.GetValue(valor)!;
        id.Should().NotBeEmpty();

        _mockRepositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockUnidadDeTrabajo.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarQso_IndicativoVacio_RetornaBadRequest()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "",
            FrecuenciaHz: 14074000,
            Modo: "FT8",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: null,
            Nombre: null,
            Comentario: null);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarQso_ModoInvalido_RetornaBadRequest()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "EA4ABC",
            FrecuenciaHz: 14074000,
            Modo: "MODO_INEXISTENTE",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: null,
            Nombre: null,
            Comentario: null);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarQso_RstEnviadoVacio_RetornaBadRequest()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "EA4ABC",
            FrecuenciaHz: 14074000,
            Modo: "FT8",
            RstEnviado: "  ",
            RstRecibido: "-12",
            Grid: null,
            Nombre: null,
            Comentario: null);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarQso_RstRecibidoVacio_RetornaBadRequest()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "EA4ABC",
            FrecuenciaHz: 14074000,
            Modo: "FT8",
            RstEnviado: "-15",
            RstRecibido: "",
            Grid: null,
            Nombre: null,
            Comentario: null);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarQso_Duplicado_RetornaConflict()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "W1AW",
            FrecuenciaHz: 14074000,
            Modo: "FT8",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: null,
            Nombre: null,
            Comentario: null);

        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(),
                It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<ConflictObjectResult>();

        _mockRepositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegistrarQso_SinGrid_CreaQsoSinLocalizador()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "EA4ABC",
            FrecuenciaHz: 7074000,
            Modo: "CW",
            RstEnviado: "599",
            RstRecibido: "599",
            Grid: null,
            Nombre: null,
            Comentario: null);

        _mockRepositorio
            .Setup(r => r.ExisteDuplicadoAsync(
                It.IsAny<Indicativo>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<Frecuencia>(),
                It.IsAny<ModoOperacion>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnidadDeTrabajo
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();

        _mockRepositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarQso_DtoNulo_RetornaBadRequest()
    {
        // Act
        IActionResult resultado = await _controlador.RegistrarQso(null!, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarQso_FrecuenciaCero_RetornaBadRequest()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "EA4ABC",
            FrecuenciaHz: 0,
            Modo: "FT8",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: null,
            Nombre: null,
            Comentario: null);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarQso_ModoVacio_RetornaBadRequest()
    {
        // Arrange
        RegistroQsoDto dto = new(
            Indicativo: "EA4ABC",
            FrecuenciaHz: 14074000,
            Modo: "",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: null,
            Nombre: null,
            Comentario: null);

        // Act
        IActionResult resultado = await _controlador.RegistrarQso(dto, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Crea un mock de IFormFile con el contenido y nombre especificados.
    /// </summary>
    private static Mock<IFormFile> CrearMockFormFile(string nombreArchivo, string contenido)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(contenido);
        MemoryStream stream = new(bytes);
        Mock<IFormFile> mockArchivo = new();
        mockArchivo.Setup(f => f.FileName).Returns(nombreArchivo);
        mockArchivo.Setup(f => f.Length).Returns(bytes.Length);
        mockArchivo.Setup(f => f.OpenReadStream()).Returns(stream);
        return mockArchivo;
    }

    /// <summary>
    /// Crea un QSO de prueba con indicativos dados.
    /// </summary>
    private static Qso CrearQsoDePrueba(string indicativoPropio, string indicativoContacto)
    {
        Indicativo propio = new(indicativoPropio);
        Indicativo contacto = new(indicativoContacto);
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.074);
        DateTimeOffset fechaInicio = DateTimeOffset.UtcNow.AddHours(-1);

        return Qso.Crear(propio, contacto, fechaInicio, frecuencia, ModoOperacion.FT8, "59");
    }
}
