using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel principal que coordina los paneles de la aplicación.
/// </summary>
public partial class VentanaPrincipalViewModel : ViewModelBase
{
    /// <summary>
    /// Panel de control del radio (frecuencia, modo, S-meter, PTT).
    /// </summary>
    [ObservableProperty]
    private PanelRigViewModel _panelRig = new();

    /// <summary>
    /// Panel de mensajes digitales decodificados.
    /// </summary>
    [ObservableProperty]
    private PanelMensajesViewModel _panelMensajes = new();

    /// <summary>
    /// Panel de registro de QSO.
    /// </summary>
    [ObservableProperty]
    private PanelRegistroQsoViewModel _panelRegistroQso = new();

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
}
