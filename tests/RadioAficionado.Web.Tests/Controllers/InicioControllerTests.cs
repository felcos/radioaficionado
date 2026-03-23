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
/// Tests unitarios para <see cref="InicioController"/>.
/// </summary>
public class InicioControllerTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<ILogger<InicioController>> _mockLogger;
    private readonly InicioController _controlador;

    public InicioControllerTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockLogger = new Mock<ILogger<InicioController>>();
        _controlador = new InicioController(_mockRepositorio.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Index_SinQsos_RetornaVistaConCeros()
    {
        // Arrange
        _mockRepositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        IActionResult resultado = await _controlador.Index(CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        InicioViewModel modelo = vista.Model.Should().BeOfType<InicioViewModel>().Subject;
        modelo.TotalQsos.Should().Be(0);
        modelo.TotalIndicativosUnicos.Should().Be(0);
        modelo.TotalBandas.Should().Be(0);
        modelo.TotalModos.Should().Be(0);
        modelo.UltimoQso.Should().BeNull();
        modelo.UltimosQsos.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_ConQsos_RetornaEstadisticasCorrectas()
    {
        // Arrange
        List<Qso> qsos = CrearListaDeQsos(3);

        _mockRepositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        IActionResult resultado = await _controlador.Index(CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        InicioViewModel modelo = vista.Model.Should().BeOfType<InicioViewModel>().Subject;
        modelo.TotalQsos.Should().Be(3);
        modelo.TotalIndicativosUnicos.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Index_ConQsos_RetornaTotalBandasCorrectas()
    {
        // Arrange
        Qso qso20m = CrearQso("EA4ABC", "W1AW", 14074000, ModoOperacion.FT8);
        Qso qso40m = CrearQso("EA4ABC", "DL1ABC", 7074000, ModoOperacion.FT8);
        List<Qso> qsos = new() { qso20m, qso40m };

        _mockRepositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        IActionResult resultado = await _controlador.Index(CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        InicioViewModel modelo = vista.Model.Should().BeOfType<InicioViewModel>().Subject;
        modelo.TotalBandas.Should().Be(2);
    }

    [Fact]
    public async Task Index_ConQsos_RetornaTotalModosCorrectos()
    {
        // Arrange
        Qso qsoFt8 = CrearQso("EA4ABC", "W1AW", 14074000, ModoOperacion.FT8);
        Qso qsoSsb = CrearQso("EA4ABC", "DL1ABC", 14200000, ModoOperacion.SSB);
        Qso qsoCw = CrearQso("EA4ABC", "F5ABC", 14050000, ModoOperacion.CW);
        List<Qso> qsos = new() { qsoFt8, qsoSsb, qsoCw };

        _mockRepositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        IActionResult resultado = await _controlador.Index(CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        InicioViewModel modelo = vista.Model.Should().BeOfType<InicioViewModel>().Subject;
        modelo.TotalModos.Should().Be(3);
    }

    [Fact]
    public async Task Index_ConQsos_RetornaFechaUltimoQso()
    {
        // Arrange
        Qso qsoAntiguo = CrearQso("EA4ABC", "W1AW", 14074000, ModoOperacion.FT8, horasAtras: 48);
        Qso qsoReciente = CrearQso("EA4ABC", "DL1ABC", 7074000, ModoOperacion.FT8, horasAtras: 1);
        List<Qso> qsos = new() { qsoAntiguo, qsoReciente };

        _mockRepositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        IActionResult resultado = await _controlador.Index(CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        InicioViewModel modelo = vista.Model.Should().BeOfType<InicioViewModel>().Subject;
        modelo.UltimoQso.Should().NotBeNull();
        modelo.UltimoQso.Should().Be(qsoReciente.FechaHoraInicio);
    }

    [Fact]
    public async Task Index_ConMasDe5Qsos_RetornaMaximo5UltimosQsos()
    {
        // Arrange
        List<Qso> qsos = CrearListaDeQsos(8);

        _mockRepositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        IActionResult resultado = await _controlador.Index(CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        InicioViewModel modelo = vista.Model.Should().BeOfType<InicioViewModel>().Subject;
        modelo.UltimosQsos.Should().HaveCount(5);
    }

    [Fact]
    public async Task Index_ConIndicativosDuplicados_CuentaUnicosCorrectamente()
    {
        // Arrange
        Qso qso1 = CrearQso("EA4ABC", "W1AW", 14074000, ModoOperacion.FT8, horasAtras: 3);
        Qso qso2 = CrearQso("EA4ABC", "W1AW", 7074000, ModoOperacion.FT8, horasAtras: 2);
        Qso qso3 = CrearQso("EA4ABC", "DL1ABC", 14074000, ModoOperacion.FT8, horasAtras: 1);
        List<Qso> qsos = new() { qso1, qso2, qso3 };

        _mockRepositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        IActionResult resultado = await _controlador.Index(CancellationToken.None);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        InicioViewModel modelo = vista.Model.Should().BeOfType<InicioViewModel>().Subject;
        modelo.TotalIndicativosUnicos.Should().Be(2);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static Qso CrearQso(
        string indicativoPropio,
        string indicativoContacto,
        long frecuenciaHz,
        ModoOperacion modo,
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
        string[] indicativos = { "W1AW", "DL1ABC", "F5ABC", "JA1ABC", "VK2ABC", "G3ABC", "I1ABC", "UA3ABC", "PY2ABC", "ZL1ABC" };
        long[] frecuencias = { 14074000, 7074000, 21074000, 28074000, 3573000 };
        ModoOperacion[] modos = { ModoOperacion.FT8, ModoOperacion.SSB, ModoOperacion.CW };

        List<Qso> qsos = new();
        for (int i = 0; i < cantidad; i++)
        {
            Qso qso = Qso.Crear(
                indicativoPropio: new Indicativo("EA4ABC"),
                indicativoContacto: new Indicativo(indicativos[i % indicativos.Length]),
                fechaHoraInicio: DateTimeOffset.UtcNow.AddHours(-(cantidad - i)),
                frecuencia: Frecuencia.DesdeHz(frecuencias[i % frecuencias.Length]),
                modo: modos[i % modos.Length],
                senalEnviada: "59");
            qsos.Add(qso);
        }

        return qsos;
    }
}
