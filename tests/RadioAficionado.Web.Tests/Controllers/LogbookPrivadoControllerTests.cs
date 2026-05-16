using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.Controllers;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Tests.Controllers;

/// <summary>
/// Tests unitarios para <see cref="LogbookPrivadoController"/>.
/// </summary>
public class LogbookPrivadoControllerTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<UserManager<UsuarioRadio>> _mockUserManager;
    private readonly Mock<ILogger<LogbookPrivadoController>> _mockLogger;
    private readonly LogbookPrivadoController _controlador;

    private const string IndicativoUsuario = "EA4ABC";
    private const string IndicativoOtro = "DL1XYZ";

    public LogbookPrivadoControllerTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockLogger = new Mock<ILogger<LogbookPrivadoController>>();

        Mock<IUserStore<UsuarioRadio>> mockUserStore = new();
        _mockUserManager = new Mock<UserManager<UsuarioRadio>>(
            mockUserStore.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<UsuarioRadio>>().Object,
            Array.Empty<IUserValidator<UsuarioRadio>>(),
            Array.Empty<IPasswordValidator<UsuarioRadio>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<UsuarioRadio>>>().Object);

        _controlador = new LogbookPrivadoController(
            _mockRepositorio.Object,
            _mockUserManager.Object,
            _mockLogger.Object);

        ConfigurarUsuarioAutenticado(IndicativoUsuario);
    }

    // ── Index ──────────────────────────────────────────────────────

    [Fact]
    public async Task Index_UsuarioConIndicativo_RetornaVistaPaginada()
    {
        // Arrange
        List<Qso> qsos = CrearListaDeQsos(3, IndicativoUsuario);
        ResultadoPaginado<Qso> resultadoPaginado = new(qsos, 3);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1,
                25,
                It.Is<FiltroQso>(f => f.Indicativo == IndicativoUsuario),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookPrivadoIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookPrivadoIndexViewModel>().Subject;
        modelo.PaginaActual.Should().Be(1);
        modelo.Qsos.Should().HaveCount(3);
        modelo.TotalElementos.Should().Be(3);
        modelo.IndicativoUsuario.Should().Be(IndicativoUsuario);
    }

    [Fact]
    public async Task Index_UsuarioSinIndicativo_RedirigeAPerfil()
    {
        // Arrange
        ConfigurarUsuarioAutenticado(indicativo: "");

        // Act
        IActionResult resultado = await _controlador.Index(ct: CancellationToken.None);

        // Assert
        RedirectToActionResult redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ActionName.Should().Be("Perfil");
        redireccion.ControllerName.Should().Be("Cuenta");
    }

    [Fact]
    public async Task Index_PaginaNegativa_CorrigeAPagina1()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1, 25,
                It.IsAny<FiltroQso>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(pagina: -5, ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookPrivadoIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookPrivadoIndexViewModel>().Subject;
        modelo.PaginaActual.Should().Be(1);
    }

    // ── Crear GET ─────────────────────────────────────────────────

    [Fact]
    public async Task Crear_Get_RetornaVistaConModosDisponibles()
    {
        // Act
        IActionResult resultado = await _controlador.Crear();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        CrearQsoViewModel modelo = vista.Model.Should().BeOfType<CrearQsoViewModel>().Subject;
        modelo.ModosDisponibles.Should().NotBeEmpty();
        modelo.ModosDisponibles.Should().Contain(ModoOperacion.FT8);
    }

    // ── Crear POST ────────────────────────────────────────────────

    [Fact]
    public async Task Crear_Post_DatosValidos_RedirigeAIndex()
    {
        // Arrange
        CrearQsoViewModel viewModel = new()
        {
            IndicativoContacto = "W1AW",
            FrecuenciaMHz = 14.074,
            Modo = ModoOperacion.FT8,
            FechaHoraInicio = DateTime.UtcNow.AddHours(-1),
            SenalEnviada = "59"
        };

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IActionResult resultado = await _controlador.Crear(viewModel, CancellationToken.None);

        // Assert
        RedirectToActionResult redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ActionName.Should().Be("Index");

        _mockRepositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Crear_Post_ModeloInvalido_RetornaVistaConErrores()
    {
        // Arrange
        CrearQsoViewModel viewModel = new()
        {
            IndicativoContacto = "",
            FrecuenciaMHz = 0,
            SenalEnviada = ""
        };

        _controlador.ModelState.AddModelError("IndicativoContacto", "Obligatorio");

        // Act
        IActionResult resultado = await _controlador.Crear(viewModel, CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        CrearQsoViewModel modelo = vista.Model.Should().BeOfType<CrearQsoViewModel>().Subject;
        modelo.ModosDisponibles.Should().NotBeEmpty();

        _mockRepositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Editar GET ────────────────────────────────────────────────

    [Fact]
    public async Task Editar_Get_QsoPropio_RetornaVistaDeEdicion()
    {
        // Arrange
        Qso qso = CrearQso(IndicativoUsuario, "W1AW");

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        // Act
        IActionResult resultado = await _controlador.Editar(qso.Id, CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        EditarQsoViewModel modelo = vista.Model.Should().BeOfType<EditarQsoViewModel>().Subject;
        modelo.Id.Should().Be(qso.Id);
        modelo.IndicativoContacto.Should().Be("W1AW");
    }

    [Fact]
    public async Task Editar_Get_QsoAjeno_RetornaForbid()
    {
        // Arrange
        Qso qso = CrearQso(IndicativoOtro, "W1AW");

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        // Act
        IActionResult resultado = await _controlador.Editar(qso.Id, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Editar_Get_QsoInexistente_RetornaNotFound()
    {
        // Arrange
        Guid idInexistente = Guid.NewGuid();

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(idInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Qso?)null);

        // Act
        IActionResult resultado = await _controlador.Editar(idInexistente, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NotFoundResult>();
    }

    // ── Editar POST ───────────────────────────────────────────────

    [Fact]
    public async Task Editar_Post_QsoPropio_DatosValidos_RedirigeAIndex()
    {
        // Arrange
        Qso qsoExistente = CrearQso(IndicativoUsuario, "W1AW");

        EditarQsoViewModel viewModel = new()
        {
            Id = qsoExistente.Id,
            IndicativoContacto = "DL1ABC",
            FrecuenciaMHz = 7.074,
            Modo = ModoOperacion.FT8,
            FechaHoraInicio = DateTime.UtcNow.AddHours(-1),
            SenalEnviada = "59"
        };

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qsoExistente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsoExistente);

        _mockRepositorio
            .Setup(r => r.EliminarAsync(qsoExistente, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IActionResult resultado = await _controlador.Editar(viewModel, CancellationToken.None);

        // Assert
        RedirectToActionResult redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ActionName.Should().Be("Index");

        _mockRepositorio.Verify(
            r => r.EliminarAsync(qsoExistente, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRepositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Editar_Post_QsoAjeno_RetornaForbid()
    {
        // Arrange
        Qso qsoAjeno = CrearQso(IndicativoOtro, "W1AW");

        EditarQsoViewModel viewModel = new()
        {
            Id = qsoAjeno.Id,
            IndicativoContacto = "W1AW",
            FrecuenciaMHz = 14.074,
            Modo = ModoOperacion.FT8,
            FechaHoraInicio = DateTime.UtcNow.AddHours(-1),
            SenalEnviada = "59"
        };

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qsoAjeno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsoAjeno);

        // Act
        IActionResult resultado = await _controlador.Editar(viewModel, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Editar_Post_QsoInexistente_RetornaNotFound()
    {
        // Arrange
        Guid idInexistente = Guid.NewGuid();

        EditarQsoViewModel viewModel = new()
        {
            Id = idInexistente,
            IndicativoContacto = "W1AW",
            FrecuenciaMHz = 14.074,
            Modo = ModoOperacion.FT8,
            FechaHoraInicio = DateTime.UtcNow.AddHours(-1),
            SenalEnviada = "59"
        };

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(idInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Qso?)null);

        // Act
        IActionResult resultado = await _controlador.Editar(viewModel, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NotFoundResult>();
    }

    // ── Detalle ───────────────────────────────────────────────────

    [Fact]
    public async Task Detalle_QsoPropio_RetornaVistaDetalle()
    {
        // Arrange
        Qso qso = CrearQso(IndicativoUsuario, "W1AW");

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        // Act
        IActionResult resultado = await _controlador.Detalle(qso.Id, CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        QsoDetalleViewModel modelo = vista.Model.Should().BeOfType<QsoDetalleViewModel>().Subject;
        modelo.Id.Should().Be(qso.Id);
        modelo.IndicativoPropio.Should().Be(IndicativoUsuario);
        modelo.IndicativoContacto.Should().Be("W1AW");
    }

    [Fact]
    public async Task Detalle_QsoAjeno_RetornaForbid()
    {
        // Arrange
        Qso qso = CrearQso(IndicativoOtro, "W1AW");

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        // Act
        IActionResult resultado = await _controlador.Detalle(qso.Id, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Detalle_QsoInexistente_RetornaNotFound()
    {
        // Arrange
        Guid idInexistente = Guid.NewGuid();

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(idInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Qso?)null);

        // Act
        IActionResult resultado = await _controlador.Detalle(idInexistente, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NotFoundResult>();
    }

    // ── Eliminar ──────────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_QsoPropio_EliminaYRedirigeAIndex()
    {
        // Arrange
        Qso qso = CrearQso(IndicativoUsuario, "W1AW");

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        _mockRepositorio
            .Setup(r => r.EliminarAsync(qso, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IActionResult resultado = await _controlador.Eliminar(qso.Id, CancellationToken.None);

        // Assert
        RedirectToActionResult redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ActionName.Should().Be("Index");

        _mockRepositorio.Verify(
            r => r.EliminarAsync(qso, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Eliminar_QsoAjeno_RetornaForbid()
    {
        // Arrange
        Qso qso = CrearQso(IndicativoOtro, "W1AW");

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        // Act
        IActionResult resultado = await _controlador.Eliminar(qso.Id, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<ForbidResult>();

        _mockRepositorio.Verify(
            r => r.EliminarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Eliminar_QsoInexistente_RetornaNotFound()
    {
        // Arrange
        Guid idInexistente = Guid.NewGuid();

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(idInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Qso?)null);

        // Act
        IActionResult resultado = await _controlador.Eliminar(idInexistente, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NotFoundResult>();

        _mockRepositorio.Verify(
            r => r.EliminarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void ConfigurarUsuarioAutenticado(string indicativo)
    {
        UsuarioRadio usuario = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = indicativo,
            Indicativo = indicativo
        };

        _mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(usuario);

        ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, indicativo),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id)
        }, "TestAuth"));

        _controlador.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private static Qso CrearQso(
        string indicativoPropio,
        string indicativoContacto,
        long frecuenciaHz = 14074000,
        ModoOperacion modo = ModoOperacion.FT8,
        int horasAtras = 1)
    {
        return Qso.Crear(
            indicativoPropio: new Indicativo(indicativoPropio),
            indicativoContacto: new Indicativo(indicativoContacto),
            fechaHoraInicio: DateTimeOffset.UtcNow.AddHours(-horasAtras),
            frecuencia: Frecuencia.DesdeHz(frecuenciaHz),
            modo: modo,
            senalEnviada: "59");
    }

    private static List<Qso> CrearListaDeQsos(int cantidad, string indicativoPropio)
    {
        string[] indicativos = { "W1AW", "DL1ABC", "F5ABC", "JA1ABC", "VK2ABC" };
        List<Qso> qsos = new();

        for (int i = 0; i < cantidad; i++)
        {
            Qso qso = CrearQso(
                indicativoPropio,
                indicativos[i % indicativos.Length],
                horasAtras: cantidad - i);
            qsos.Add(qso);
        }

        return qsos;
    }
}
