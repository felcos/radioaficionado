using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel principal que coordina los paneles de la aplicación.
/// Recibe los sub-ViewModels por inyección de dependencias.
/// </summary>
public partial class VentanaPrincipalViewModel : ViewModelBase
{
    /// <summary>
    /// Panel de control del radio (frecuencia, modo, S-meter, PTT).
    /// </summary>
    public PanelRigViewModel PanelRig { get; }

    /// <summary>
    /// Panel de mensajes digitales decodificados.
    /// </summary>
    public PanelMensajesViewModel PanelMensajes { get; }

    /// <summary>
    /// Panel de registro de QSO.
    /// </summary>
    public PanelRegistroQsoViewModel PanelRegistroQso { get; }

    /// <summary>
    /// Panel del logbook (libro de guardia) con paginación, filtros e importación/exportación ADIF.
    /// </summary>
    public PanelLogbookViewModel PanelLogbook { get; }

    /// <summary>
    /// Panel de DX Cluster para spots en tiempo real.
    /// </summary>
    public PanelDxClusterViewModel PanelDxCluster { get; }

    /// <summary>
    /// Estado de conexión con el radio.
    /// </summary>
    [ObservableProperty]
    private string _estadoConexion = "Desconectado";

    /// <summary>
    /// Título de la ventana principal.
    /// </summary>
    [ObservableProperty]
    private string _titulo = "RadioAficionado v0.1";

    /// <summary>
    /// Pestaña activa: 0 = Operación, 1 = Logbook, 2 = DX Cluster.
    /// </summary>
    [ObservableProperty]
    private int _pestanaActiva;

    /// <summary>
    /// Crea el ViewModel principal inyectando los sub-ViewModels.
    /// </summary>
    /// <param name="panelRig">ViewModel del panel de control del radio.</param>
    /// <param name="panelMensajes">ViewModel del panel de mensajes digitales.</param>
    /// <param name="panelRegistroQso">ViewModel del panel de registro de QSO.</param>
    /// <param name="panelLogbook">ViewModel del panel de logbook.</param>
    /// <param name="panelDxCluster">ViewModel del panel de DX Cluster.</param>
    public VentanaPrincipalViewModel(
        PanelRigViewModel panelRig,
        PanelMensajesViewModel panelMensajes,
        PanelRegistroQsoViewModel panelRegistroQso,
        PanelLogbookViewModel panelLogbook,
        PanelDxClusterViewModel panelDxCluster)
    {
        PanelRig = panelRig ?? throw new ArgumentNullException(nameof(panelRig));
        PanelMensajes = panelMensajes ?? throw new ArgumentNullException(nameof(panelMensajes));
        PanelRegistroQso = panelRegistroQso ?? throw new ArgumentNullException(nameof(panelRegistroQso));
        PanelLogbook = panelLogbook ?? throw new ArgumentNullException(nameof(panelLogbook));
        PanelDxCluster = panelDxCluster ?? throw new ArgumentNullException(nameof(panelDxCluster));
    }
}
