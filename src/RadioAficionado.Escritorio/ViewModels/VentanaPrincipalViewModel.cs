using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel principal que coordina los paneles de la aplicacion.
/// Recibe los sub-ViewModels por inyeccion de dependencias.
/// Incluye un reloj UTC actualizado cada segundo para la barra de estado.
/// </summary>
public partial class VentanaPrincipalViewModel : ViewModelBase, IDisposable
{
    private PeriodicTimer? _timerReloj;
    private CancellationTokenSource? _ctsReloj;
    private bool _disposed;

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
    /// Panel del logbook (libro de guardia) con paginacion, filtros e importacion/exportacion ADIF.
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
    /// Panel de propagacion HF con indices solares y predicciones por banda.
    /// </summary>
    public PanelPropagacionViewModel PanelPropagacion { get; }

    /// <summary>
    /// Panel de tracking DXCC con estadisticas y tabla de entidades.
    /// </summary>
    public PanelDxccViewModel PanelDxcc { get; }

    /// <summary>
    /// Panel de satelites amateur con tracking y prediccion de pasos.
    /// </summary>
    public PanelSatelitesViewModel PanelSatelites { get; }

    /// <summary>
    /// Panel de APRS para recepcion de paquetes en tiempo real.
    /// </summary>
    public PanelAprsViewModel PanelAprs { get; }

    /// <summary>
    /// Panel de control del receptor SDR.
    /// </summary>
    public PanelSdrViewModel PanelSdr { get; }

    /// <summary>
    /// Panel de waterfall en vivo con procesamiento FFT en tiempo real.
    /// </summary>
    public PanelWaterfallViewModel PanelWaterfall { get; }

    /// <summary>
    /// Estado de sincronizacion entre la aplicacion de escritorio y el servidor web.
    /// </summary>
    public EstadoSincronizacionViewModel EstadoSincronizacion { get; }

    /// <summary>
    /// Estado de conexion con el radio.
    /// </summary>
    [ObservableProperty]
    private string _estadoConexion = "Desconectado";

    /// <summary>
    /// Titulo de la ventana principal.
    /// </summary>
    [ObservableProperty]
    private string _titulo = "RadioAficionado v0.1";

    /// <summary>
    /// Pestana activa: 0 = QSO, 1 = Logbook, 2 = DX Cluster, 3 = Contest, 4 = Activaciones, 5 = Propagacion, 6 = DXCC, 7 = Satelites, 8 = APRS, 9 = SDR, 10 = Waterfall.
    /// </summary>
    [ObservableProperty]
    private int _pestanaActiva;

    /// <summary>
    /// Hora UTC actual formateada para la barra de estado. Se actualiza cada segundo.
    /// </summary>
    [ObservableProperty]
    private string _horaUtc = DateTime.UtcNow.ToString("HH:mm:ss") + " UTC";

    /// <summary>
    /// Crea el ViewModel principal inyectando los sub-ViewModels.
    /// Inicia el reloj UTC para la barra de estado.
    /// </summary>
    /// <param name="panelRig">ViewModel del panel de control del radio.</param>
    /// <param name="panelMensajes">ViewModel del panel de mensajes digitales.</param>
    /// <param name="panelRegistroQso">ViewModel del panel de registro de QSO.</param>
    /// <param name="panelLogbook">ViewModel del panel de logbook.</param>
    /// <param name="panelDxCluster">ViewModel del panel de DX Cluster.</param>
    /// <param name="panelContest">ViewModel del panel de concursos.</param>
    /// <param name="panelActivaciones">ViewModel del panel de activaciones POTA/SOTA.</param>
    /// <param name="panelPropagacion">ViewModel del panel de propagacion HF.</param>
    /// <param name="panelDxcc">ViewModel del panel de tracking DXCC.</param>
    /// <param name="panelSatelites">ViewModel del panel de satelites amateur.</param>
    /// <param name="panelAprs">ViewModel del panel de APRS.</param>
    /// <param name="panelSdr">ViewModel del panel de control SDR.</param>
    /// <param name="panelWaterfall">ViewModel del panel de waterfall en vivo.</param>
    /// <param name="estadoSincronizacion">ViewModel del estado de sincronizacion.</param>
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
        PanelAprsViewModel panelAprs,
        PanelSdrViewModel panelSdr,
        PanelWaterfallViewModel panelWaterfall,
        EstadoSincronizacionViewModel estadoSincronizacion)
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
        PanelSdr = panelSdr ?? throw new ArgumentNullException(nameof(panelSdr));
        PanelWaterfall = panelWaterfall ?? throw new ArgumentNullException(nameof(panelWaterfall));
        EstadoSincronizacion = estadoSincronizacion ?? throw new ArgumentNullException(nameof(estadoSincronizacion));

        IniciarRelojUtc();
    }

    /// <summary>
    /// Inicia un timer que actualiza la hora UTC cada segundo para la barra de estado.
    /// </summary>
    private void IniciarRelojUtc()
    {
        _ctsReloj = new CancellationTokenSource();
        _timerReloj = new PeriodicTimer(TimeSpan.FromSeconds(1));
        CancellationToken token = _ctsReloj.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timerReloj.WaitForNextTickAsync(token))
                {
                    HoraUtc = DateTime.UtcNow.ToString("HH:mm:ss") + " UTC";
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelacion normal.
            }
        }, token);
    }

    /// <summary>
    /// Libera los recursos del reloj UTC.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ctsReloj?.Cancel();
        _ctsReloj?.Dispose();
        _ctsReloj = null;
        _timerReloj?.Dispose();
        _timerReloj = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
