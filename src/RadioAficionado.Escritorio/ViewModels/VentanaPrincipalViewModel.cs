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
    /// Panel de concursos de radioaficionado.
    /// </summary>
    public PanelContestViewModel PanelContest { get; }

    /// <summary>
    /// Panel de activaciones POTA/SOTA/WWFF/IOTA.
    /// </summary>
    public PanelActivacionesViewModel PanelActivaciones { get; }

    /// <summary>
    /// Panel de propagación HF con índices solares y predicciones por banda.
    /// </summary>
    public PanelPropagacionViewModel PanelPropagacion { get; }

    /// <summary>
    /// Panel de tracking DXCC con estadísticas y tabla de entidades.
    /// </summary>
    public PanelDxccViewModel PanelDxcc { get; }

    /// <summary>
    /// Panel de satélites amateur con tracking y predicción de pasos.
    /// </summary>
    public PanelSatelitesViewModel PanelSatelites { get; }

    /// <summary>
    /// Panel de APRS para recepción de paquetes en tiempo real.
    /// </summary>
    public PanelAprsViewModel PanelAprs { get; }

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
    /// Pestaña activa: 0 = Operación, 1 = Logbook, 2 = DX Cluster, 3 = Contest, 4 = Activaciones, 5 = Propagación, 6 = DXCC, 7 = Satélites, 8 = APRS.
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
    /// <param name="panelContest">ViewModel del panel de concursos.</param>
    /// <param name="panelActivaciones">ViewModel del panel de activaciones POTA/SOTA.</param>
    /// <param name="panelPropagacion">ViewModel del panel de propagación HF.</param>
    /// <param name="panelDxcc">ViewModel del panel de tracking DXCC.</param>
    /// <param name="panelSatelites">ViewModel del panel de satélites amateur.</param>
    /// <param name="panelAprs">ViewModel del panel de APRS.</param>
    public VentanaPrincipalViewModel(
        PanelRigViewModel panelRig,
        PanelMensajesViewModel panelMensajes,
        PanelRegistroQsoViewModel panelRegistroQso,
        PanelLogbookViewModel panelLogbook,
        PanelDxClusterViewModel panelDxCluster,
        PanelContestViewModel panelContest,
        PanelActivacionesViewModel panelActivaciones,
        PanelPropagacionViewModel panelPropagacion,
        PanelDxccViewModel panelDxcc,
        PanelSatelitesViewModel panelSatelites,
        PanelAprsViewModel panelAprs)
    {
        PanelRig = panelRig ?? throw new ArgumentNullException(nameof(panelRig));
        PanelMensajes = panelMensajes ?? throw new ArgumentNullException(nameof(panelMensajes));
        PanelRegistroQso = panelRegistroQso ?? throw new ArgumentNullException(nameof(panelRegistroQso));
        PanelLogbook = panelLogbook ?? throw new ArgumentNullException(nameof(panelLogbook));
        PanelDxCluster = panelDxCluster ?? throw new ArgumentNullException(nameof(panelDxCluster));
        PanelContest = panelContest ?? throw new ArgumentNullException(nameof(panelContest));
        PanelActivaciones = panelActivaciones ?? throw new ArgumentNullException(nameof(panelActivaciones));
        PanelPropagacion = panelPropagacion ?? throw new ArgumentNullException(nameof(panelPropagacion));
        PanelDxcc = panelDxcc ?? throw new ArgumentNullException(nameof(panelDxcc));
        PanelSatelites = panelSatelites ?? throw new ArgumentNullException(nameof(panelSatelites));
        PanelAprs = panelAprs ?? throw new ArgumentNullException(nameof(panelAprs));
    }
}
