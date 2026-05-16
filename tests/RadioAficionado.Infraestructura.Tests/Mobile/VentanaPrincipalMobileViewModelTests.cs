using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Mobile.ViewModels;

namespace RadioAficionado.Infraestructura.Tests.Mobile;

/// <summary>
/// Tests unitarios para <see cref="VentanaPrincipalMobileViewModel"/>.
/// Verifica la navegación entre pestañas y las propiedades iniciales.
/// </summary>
public class VentanaPrincipalMobileViewModelTests
{
    private VentanaPrincipalMobileViewModel CrearViewModel()
    {
        Mock<ILogger<VentanaPrincipalMobileViewModel>> mockLoggerPrincipal = new();
        Mock<ILogger<PanelLogbookMobileViewModel>> mockLoggerLogbook = new();
        Mock<ILogger<PanelMapaMobileViewModel>> mockLoggerMapa = new();
        Mock<ILogger<PanelPropagacionMobileViewModel>> mockLoggerPropagacion = new();
        Mock<IRepositorioQso> mockRepositorioQso = new();
        Mock<IServicioConfiguracion> mockConfiguracion = new();
        Mock<IServicioPropagacion> mockServicioPropagacion = new();

        PanelLogbookMobileViewModel panelLogbook = new PanelLogbookMobileViewModel(
            mockRepositorioQso.Object, mockConfiguracion.Object, mockLoggerLogbook.Object);

        PanelMapaMobileViewModel panelMapa = new PanelMapaMobileViewModel(
            mockRepositorioQso.Object, mockLoggerMapa.Object);

        PanelPropagacionMobileViewModel panelPropagacion = new PanelPropagacionMobileViewModel(
            mockServicioPropagacion.Object, mockLoggerPropagacion.Object);

        return new VentanaPrincipalMobileViewModel(
            mockLoggerPrincipal.Object,
            panelLogbook,
            panelMapa,
            panelPropagacion);
    }

    [Fact]
    public void PestanaSeleccionada_Inicial_EsCero()
    {
        // Arrange & Act
        VentanaPrincipalMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.PestanaSeleccionada.Should().Be(0);
    }

    [Fact]
    public void Titulo_Inicial_EsRadioAficionadoMobile()
    {
        // Arrange & Act
        VentanaPrincipalMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.Titulo.Should().Be("RadioAficionado Mobile");
    }

    [Fact]
    public void PanelesHijos_NoSonNull()
    {
        // Arrange & Act
        VentanaPrincipalMobileViewModel viewModel = CrearViewModel();

        // Assert
        viewModel.PanelLogbook.Should().NotBeNull();
        viewModel.PanelMapa.Should().NotBeNull();
        viewModel.PanelPropagacion.Should().NotBeNull();
    }
}
