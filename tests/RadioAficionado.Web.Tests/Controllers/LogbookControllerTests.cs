using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.Controllers;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Tests.Controllers;

/// <summary>
/// Tests unitarios para <see cref="LogbookController"/>.
/// </summary>
public class LogbookControllerTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<ILogger<LogbookController>> _mockLogger;
    private readonly LogbookController _controlador;

    public LogbookControllerTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockLogger = new Mock<ILogger<LogbookController>>();
        _controlador = new LogbookController(_mockRepositorio.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Index_SinFiltros_RetornaPrimeraPagina()
    {
        // Arrange
        List<Qso> qsos = CrearListaDeQsos(3);
        ResultadoPaginado<Qso> resultadoPaginado = new(qsos, 3);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 25, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookIndexViewModel>().Subject;
        modelo.PaginaActual.Should().Be(1);
        modelo.Qsos.Should().HaveCount(3);
        modelo.TotalElementos.Should().Be(3);
    }

    [Fact]
    public async Task Index_ConFiltroIndicativo_FiltraCorrectamente()
    {
        // Arrange
        List<Qso> qsosFiltrados = new() { CrearQso("EA4ABC", "W1AW") };
        ResultadoPaginado<Qso> resultadoPaginado = new(qsosFiltrados, 1);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1,
                25,
                It.Is<FiltroQso>(f => f != null && f.Indicativo == "W1AW"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(indicativo: "W1AW", ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookIndexViewModel>().Subject;
        modelo.FiltroIndicativo.Should().Be("W1AW");
        modelo.Qsos.Should().HaveCount(1);
    }

    [Fact]
    public async Task Index_ConFiltroBanda_PasaFiltroAlRepositorio()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1,
                25,
                It.Is<FiltroQso>(f => f != null && f.Banda == BandaRadio.Banda20m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(banda: BandaRadio.Banda20m, ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookIndexViewModel>().Subject;
        modelo.FiltroBanda.Should().Be(BandaRadio.Banda20m);
    }

    [Fact]
    public async Task Index_ConFiltroModo_PasaFiltroAlRepositorio()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1,
                25,
                It.Is<FiltroQso>(f => f != null && f.Modo == ModoOperacion.FT8),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(modo: ModoOperacion.FT8, ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookIndexViewModel>().Subject;
        modelo.FiltroModo.Should().Be(ModoOperacion.FT8);
    }

    [Fact]
    public async Task Index_PaginaNegativa_CorrigeAPagina1()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 25, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(pagina: -5, ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookIndexViewModel>().Subject;
        modelo.PaginaActual.Should().Be(1);
    }

    [Fact]
    public async Task Index_SegundaPagina_PasaPaginaCorrectaAlRepositorio()
    {
        // Arrange
        List<Qso> qsosPagina2 = CrearListaDeQsos(2);
        ResultadoPaginado<Qso> resultadoPaginado = new(qsosPagina2, 27);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(2, 25, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(pagina: 2, ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookIndexViewModel>().Subject;
        modelo.PaginaActual.Should().Be(2);
        modelo.TotalElementos.Should().Be(27);
    }

    [Fact]
    public async Task Detalle_QsoExistente_RetornaVista()
    {
        // Arrange
        Qso qso = CrearQso("EA4ABC", "W1AW");
        Guid qsoId = qso.Id;

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qsoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        // Act
        IActionResult resultado = await _controlador.Detalle(qsoId, CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        QsoDetalleViewModel modelo = vista.Model.Should().BeOfType<QsoDetalleViewModel>().Subject;
        modelo.Id.Should().Be(qsoId);
        modelo.IndicativoPropio.Should().Be("EA4ABC");
        modelo.IndicativoContacto.Should().Be("W1AW");
    }

    [Fact]
    public async Task Detalle_QsoNoExistente_RetornaNotFound()
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

    [Fact]
    public async Task Index_SinFiltros_IncluyeModosYBandasDisponibles()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 25, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookIndexViewModel>().Subject;
        modelo.ModosDisponibles.Should().NotBeEmpty();
        modelo.BandasDisponibles.Should().NotBeEmpty();
        modelo.ModosDisponibles.Should().Contain(ModoOperacion.FT8);
        modelo.BandasDisponibles.Should().Contain(BandaRadio.Banda20m);
    }

    [Fact]
    public async Task Index_ConFiltroFechas_PasaFiltroAlRepositorio()
    {
        // Arrange
        DateTimeOffset fechaDesde = DateTimeOffset.UtcNow.AddDays(-7);
        DateTimeOffset fechaHasta = DateTimeOffset.UtcNow;
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1,
                25,
                It.Is<FiltroQso>(f => f != null && f.FechaDesde == fechaDesde && f.FechaHasta == fechaHasta),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        // Act
        IActionResult resultado = await _controlador.Index(
            fechaDesde: fechaDesde,
            fechaHasta: fechaHasta,
            ct: CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        LogbookIndexViewModel modelo = vista.Model.Should().BeOfType<LogbookIndexViewModel>().Subject;
        modelo.FiltroFechaDesde.Should().Be(fechaDesde);
        modelo.FiltroFechaHasta.Should().Be(fechaHasta);
    }

    [Fact]
    public async Task Detalle_QsoCompletado_RetornaDetalleConSenales()
    {
        // Arrange
        Qso qso = CrearQso("EA4ABC", "W1AW");
        qso.Completar(DateTimeOffset.UtcNow, "59");
        Guid qsoId = qso.Id;

        _mockRepositorio
            .Setup(r => r.ObtenerPorIdAsync(qsoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qso);

        // Act
        IActionResult resultado = await _controlador.Detalle(qsoId, CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        QsoDetalleViewModel modelo = vista.Model.Should().BeOfType<QsoDetalleViewModel>().Subject;
        modelo.SenalEnviada.Should().Be("59");
        modelo.SenalRecibida.Should().Be("59");
        modelo.FechaHoraFin.Should().NotBeNull();
    }

    // ── Helpers ──────────────────────────────────────────────────

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

    private static List<Qso> CrearListaDeQsos(int cantidad)
    {
        string[] indicativos = { "W1AW", "DL1ABC", "F5ABC", "JA1ABC", "VK2ABC" };
        List<Qso> qsos = new();

        for (int i = 0; i < cantidad; i++)
        {
            Qso qso = CrearQso(
                "EA4ABC",
                indicativos[i % indicativos.Length],
                horasAtras: cantidad - i);
            qsos.Add(qso);
        }

        return qsos;
    }
}
