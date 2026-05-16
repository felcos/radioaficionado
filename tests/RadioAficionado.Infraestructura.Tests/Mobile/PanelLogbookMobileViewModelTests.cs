using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Mobile.ViewModels;

namespace RadioAficionado.Infraestructura.Tests.Mobile;

/// <summary>
/// Tests unitarios para <see cref="PanelLogbookMobileViewModel"/>.
/// Verifica propiedades iniciales, opciones de filtro y validaciones del constructor.
/// </summary>
public class PanelLogbookMobileViewModelTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<IServicioConfiguracion> _mockConfiguracion;
    private readonly Mock<ILogger<PanelLogbookMobileViewModel>> _mockLogger;

    public PanelLogbookMobileViewModelTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockConfiguracion = new Mock<IServicioConfiguracion>();
        _mockLogger = new Mock<ILogger<PanelLogbookMobileViewModel>>();
    }

    private PanelLogbookMobileViewModel CrearViewModel()
    {
        return new PanelLogbookMobileViewModel(
            _mockRepositorio.Object,
            _mockConfiguracion.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void PaginaActual_Inicial_EsUno()
    {
        // Arrange & Act
        PanelLogbookMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.PaginaActual.Should().Be(1);
    }

    [Fact]
    public void TextoBusqueda_Inicial_EsVacio()
    {
        // Arrange & Act
        PanelLogbookMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.TextoBusqueda.Should().BeEmpty();
    }

    [Fact]
    public void OpcionesBanda_ContieneBandasHf()
    {
        // Arrange & Act
        PanelLogbookMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.OpcionesBanda.Should().Contain("20m");
        viewModel.OpcionesBanda.Should().Contain("40m");
        viewModel.OpcionesBanda.Should().Contain("80m");
    }

    [Fact]
    public void OpcionesModo_ContieneModosComunes()
    {
        // Arrange & Act
        PanelLogbookMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.OpcionesModo.Should().Contain("SSB");
        viewModel.OpcionesModo.Should().Contain("FT8");
        viewModel.OpcionesModo.Should().Contain("CW");
    }

    [Fact]
    public void EstaCargando_Inicial_EsFalso()
    {
        // Arrange & Act
        PanelLogbookMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.EstaCargando.Should().BeFalse();
    }
}
