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
    /// Crea el ViewModel principal inyectando los sub-ViewModels.
    /// </summary>
    /// <param name="panelRig">ViewModel del panel de control del radio.</param>
    /// <param name="panelMensajes">ViewModel del panel de mensajes digitales.</param>
    /// <param name="panelRegistroQso">ViewModel del panel de registro de QSO.</param>
    public VentanaPrincipalViewModel(
        PanelRigViewModel panelRig,
        PanelMensajesViewModel panelMensajes,
        PanelRegistroQsoViewModel panelRegistroQso)
    {
        PanelRig = panelRig ?? throw new ArgumentNullException(nameof(panelRig));
        PanelMensajes = panelMensajes ?? throw new ArgumentNullException(nameof(panelMensajes));
        PanelRegistroQso = panelRegistroQso ?? throw new ArgumentNullException(nameof(panelRegistroQso));
    }
}
