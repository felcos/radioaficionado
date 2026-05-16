using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RadioAficionado.Mobile.ViewModels;

/// <summary>
/// ViewModel principal de la aplicación móvil.
/// Gestiona la navegación simplificada entre las pestañas:
/// Logbook, Mapa, Propagación y Configuración.
/// No incluye waterfall, control de rig ni DX Cluster (no aplican en mobile).
/// </summary>
public partial class VentanaPrincipalMobileViewModel : ViewModelBase
{
    private readonly ILogger<VentanaPrincipalMobileViewModel> _logger;

    /// <summary>
    /// Título de la aplicación mostrado en la cabecera.
    /// </summary>
    [ObservableProperty]
    private string _titulo = "RadioAficionado Mobile";

    /// <summary>
    /// Índice de la pestaña seleccionada actualmente.
    /// 0 = Logbook, 1 = Mapa, 2 = Propagación, 3 = Configuración.
    /// </summary>
    [ObservableProperty]
    private int _pestanaSeleccionada;

    /// <summary>
    /// ViewModel del panel de Logbook móvil.
    /// </summary>
    public PanelLogbookMobileViewModel PanelLogbook { get; }

    /// <summary>
    /// ViewModel del panel de Mapa móvil.
    /// </summary>
    public PanelMapaMobileViewModel PanelMapa { get; }

    /// <summary>
    /// ViewModel del panel de Propagación móvil.
    /// </summary>
    public PanelPropagacionMobileViewModel PanelPropagacion { get; }

    /// <summary>
    /// Crea el ViewModel principal móvil con las dependencias inyectadas.
    /// </summary>
    /// <param name="logger">Logger para registro de eventos.</param>
    /// <param name="panelLogbook">ViewModel del logbook móvil.</param>
    /// <param name="panelMapa">ViewModel del mapa móvil.</param>
    /// <param name="panelPropagacion">ViewModel de propagación móvil.</param>
    public VentanaPrincipalMobileViewModel(
        ILogger<VentanaPrincipalMobileViewModel> logger,
        PanelLogbookMobileViewModel panelLogbook,
        PanelMapaMobileViewModel panelMapa,
        PanelPropagacionMobileViewModel panelPropagacion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        PanelLogbook = panelLogbook ?? throw new ArgumentNullException(nameof(panelLogbook));
        PanelMapa = panelMapa ?? throw new ArgumentNullException(nameof(panelMapa));
        PanelPropagacion = panelPropagacion ?? throw new ArgumentNullException(nameof(panelPropagacion));

        _logger.LogInformation("Aplicación móvil RadioAficionado iniciada.");
    }

    /// <summary>
    /// Navega a la pestaña de Logbook.
    /// </summary>
    [RelayCommand]
    private void NavegarALogbook()
    {
        PestanaSeleccionada = 0;
    }

    /// <summary>
    /// Navega a la pestaña de Mapa.
    /// </summary>
    [RelayCommand]
    private void NavegarAMapa()
    {
        PestanaSeleccionada = 1;
    }

    /// <summary>
    /// Navega a la pestaña de Propagación.
    /// </summary>
    [RelayCommand]
    private void NavegarAPropagacion()
    {
        PestanaSeleccionada = 2;
    }

    /// <summary>
    /// Navega a la pestaña de Configuración.
    /// </summary>
    [RelayCommand]
    private void NavegarAConfiguracion()
    {
        PestanaSeleccionada = 3;
    }
}
