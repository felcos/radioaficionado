using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Escritorio.ViewModels;

namespace RadioAficionado.Escritorio.Tests.ViewModels;

/// <summary>
/// Tests unitarios para <see cref="PanelLogbookViewModel"/>.
/// </summary>
public class PanelLogbookViewModelTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<ILogger<PanelLogbookViewModel>> _mockLogger;
    private readonly PanelLogbookViewModel _viewModel;

    public PanelLogbookViewModelTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockLogger = new Mock<ILogger<PanelLogbookViewModel>>();
        _viewModel = new PanelLogbookViewModel(_mockRepositorio.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CargarPagina_PrimeraPagina_CargaDatos()
    {
        // Arrange
        List<Qso> qsos = CrearListaDeQsos(3);
        ResultadoPaginado<Qso> resultadoPaginado = new(qsos, 3);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        await _viewModel.CargarPaginaCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Qsos.Should().HaveCount(3);
        _viewModel.TotalQsos.Should().Be(3);
        _viewModel.PaginaActual.Should().Be(1);
        _viewModel.EstaCargando.Should().BeFalse();
    }

    [Fact]
    public async Task CargarPagina_SinQsos_MuestraListaVacia()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        await _viewModel.CargarPaginaCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Qsos.Should().BeEmpty();
        _viewModel.TotalQsos.Should().Be(0);
        _viewModel.TotalPaginas.Should().Be(1);
    }

    [Fact]
    public async Task AplicarFiltros_PorIndicativo_FiltraCorrectamente()
    {
        // Arrange
        List<Qso> qsosFiltrados = new() { CrearQso("EA4ABC", "W1AW") };
        ResultadoPaginado<Qso> resultadoPaginado = new(qsosFiltrados, 1);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                1,
                50,
                It.Is<FiltroQso>(f => f != null && f.Indicativo == "W1AW"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsosFiltrados);

        _viewModel.FiltroIndicativo = "W1AW";

        // Act
        await _viewModel.AplicarFiltrosCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Qsos.Should().HaveCount(1);
        _viewModel.PaginaActual.Should().Be(1);
    }

    [Fact]
    public async Task PaginaSiguiente_EnUltimaPagina_NoAvanza()
    {
        // Arrange
        List<Qso> qsos = CrearListaDeQsos(3);
        ResultadoPaginado<Qso> resultadoPaginado = new(qsos, 3);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Cargar primera (y unica) pagina
        await _viewModel.CargarPaginaCommand.ExecuteAsync(null);
        int paginaAntes = _viewModel.PaginaActual;

        // Act
        await _viewModel.PaginaSiguienteCommand.ExecuteAsync(null);

        // Assert
        _viewModel.PaginaActual.Should().Be(paginaAntes);
    }

    [Fact]
    public async Task PaginaAnterior_EnPrimeraPagina_NoRetrocede()
    {
        // Arrange
        List<Qso> qsos = CrearListaDeQsos(3);
        ResultadoPaginado<Qso> resultadoPaginado = new(qsos, 3);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        await _viewModel.CargarPaginaCommand.ExecuteAsync(null);

        // Act
        await _viewModel.PaginaAnteriorCommand.ExecuteAsync(null);

        // Assert
        _viewModel.PaginaActual.Should().Be(1);
    }

    [Fact]
    public async Task LimpiarFiltros_ConFiltrosActivos_LimpiaYRecarga()
    {
        // Arrange
        ResultadoPaginado<Qso> resultadoPaginado = new(new List<Qso>(), 0);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<FiltroQso?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        _viewModel.FiltroIndicativo = "W1AW";
        _viewModel.FiltroBanda = "20m";
        _viewModel.FiltroModo = "FT8";

        // Act
        await _viewModel.LimpiarFiltrosCommand.ExecuteAsync(null);

        // Assert
        _viewModel.FiltroIndicativo.Should().BeEmpty();
        _viewModel.FiltroBanda.Should().BeEmpty();
        _viewModel.FiltroModo.Should().BeEmpty();
        _viewModel.FiltroFechaDesde.Should().BeNull();
        _viewModel.FiltroFechaHasta.Should().BeNull();
        _viewModel.PaginaActual.Should().Be(1);
    }

    [Fact]
    public async Task CargarPagina_ErrorEnRepositorio_MuestraMensajeDeError()
    {
        // Arrange
        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<FiltroQso?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Error de conexion simulado"));

        // Act
        await _viewModel.CargarPaginaCommand.ExecuteAsync(null);

        // Assert
        _viewModel.MensajeEstado.Should().Contain("Error al cargar QSOs");
        _viewModel.EstaCargando.Should().BeFalse();
    }

    [Fact]
    public async Task CargarPagina_CalculaTotalPaginasCorrectamente()
    {
        // Arrange
        List<Qso> qsos = CrearListaDeQsos(5);
        ResultadoPaginado<Qso> resultadoPaginado = new(qsos, 120);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        await _viewModel.CargarPaginaCommand.ExecuteAsync(null);

        // Assert
        _viewModel.TotalPaginas.Should().Be(3); // ceil(120/50) = 3
        _viewModel.TotalQsos.Should().Be(120);
    }

    [Fact]
    public async Task CargarPagina_MapeaQsosAViewModelCorrectamente()
    {
        // Arrange
        Qso qso = CrearQso("EA4ABC", "W1AW", 14074000, ModoOperacion.FT8);
        List<Qso> qsos = new() { qso };
        ResultadoPaginado<Qso> resultadoPaginado = new(qsos, 1);

        _mockRepositorio
            .Setup(r => r.ObtenerPaginadoAsync(1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoPaginado);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(qsos);

        // Act
        await _viewModel.CargarPaginaCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Qsos.Should().HaveCount(1);
        QsoEnLogbookVm qsoVm = _viewModel.Qsos[0];
        qsoVm.Indicativo.Should().Be("W1AW");
        qsoVm.Modo.Should().Be("FT8");
        qsoVm.Banda.Should().Be("20m");
    }

    [Fact]
    public void TextoPaginacion_ValoresIniciales_MuestraPagina1De1()
    {
        // Assert
        _viewModel.TextoPaginacion.Should().Be("Página 1 de 1");
    }

    [Fact]
    public void Constructor_RepositorioNulo_LanzaExcepcion()
    {
        // Act
        Action accion = () => new PanelLogbookViewModel(null!, _mockLogger.Object);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("repositorioQso");
    }

    [Fact]
    public void Constructor_LoggerNulo_LanzaExcepcion()
    {
        // Act
        Action accion = () => new PanelLogbookViewModel(_mockRepositorio.Object, null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static Qso CrearQso(
        string indicativoPropio = "EA4ABC",
        string indicativoContacto = "W1AW",
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
                indicativoContacto: indicativos[i % indicativos.Length],
                horasAtras: cantidad - i);
            qsos.Add(qso);
        }

        return qsos;
    }
}
