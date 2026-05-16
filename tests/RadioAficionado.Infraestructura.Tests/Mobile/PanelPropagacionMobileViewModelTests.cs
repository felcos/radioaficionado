using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Mobile.ViewModels;

namespace RadioAficionado.Infraestructura.Tests.Mobile;

/// <summary>
/// Tests unitarios para <see cref="PanelPropagacionMobileViewModel"/>.
/// Verifica propiedades iniciales y valores por defecto de los índices solares.
/// </summary>
public class PanelPropagacionMobileViewModelTests
{
    private readonly Mock<IServicioPropagacion> _mockServicioPropagacion;
    private readonly Mock<ILogger<PanelPropagacionMobileViewModel>> _mockLogger;

    public PanelPropagacionMobileViewModelTests()
    {
        _mockServicioPropagacion = new Mock<IServicioPropagacion>();
        _mockLogger = new Mock<ILogger<PanelPropagacionMobileViewModel>>();
    }

    private PanelPropagacionMobileViewModel CrearViewModel()
    {
        return new PanelPropagacionMobileViewModel(
            _mockServicioPropagacion.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Sfi_Inicial_EsCero()
    {
        // Arrange & Act
        PanelPropagacionMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.Sfi.Should().Be(0);
    }

    [Fact]
    public void Kp_Inicial_EsCero()
    {
        // Arrange & Act
        PanelPropagacionMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.Kp.Should().Be(0);
    }

    [Fact]
    public void Predicciones_Inicial_EstaVacia()
    {
        // Arrange & Act
        PanelPropagacionMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.Predicciones.Should().BeEmpty();
    }

    [Fact]
    public void UltimaActualizacion_Inicial_EsSinDatos()
    {
        // Arrange & Act
        PanelPropagacionMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.UltimaActualizacion.Should().Be("Sin datos");
    }

    [Fact]
    public void Procesando_Inicial_EsFalso()
    {
        // Arrange & Act
        PanelPropagacionMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.Procesando.Should().BeFalse();
    }
}
