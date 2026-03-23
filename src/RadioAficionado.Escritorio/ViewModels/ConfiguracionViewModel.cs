using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RadioAficionado.Dominio.Configuracion;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para la ventana de configuración de la aplicación.
/// Expone todas las propiedades editables organizadas por pestaña:
/// Estación, Radio, Rotador, Audio, DX Cluster y General.
/// </summary>
public partial class ConfiguracionViewModel : ViewModelBase
{
    private readonly IServicioConfiguracion _servicioConfiguracion;

    // --- Pestaña activa ---

    /// <summary>Índice de la pestaña activa en el TabControl.</summary>
    [ObservableProperty]
    private int _pestanaActiva;

    // --- Estación ---

    /// <summary>Indicativo propio del operador.</summary>
    [ObservableProperty]
    private string _indicativoPropio = string.Empty;

    /// <summary>Localizador Maidenhead de la estación.</summary>
    [ObservableProperty]
    private string _localizador = string.Empty;

    /// <summary>Región ITU seleccionada.</summary>
    [ObservableProperty]
    private RegionItu _regionItu = RegionItu.Region2;

    /// <summary>Nivel de licencia del operador.</summary>
    [ObservableProperty]
    private NivelLicencia _nivelLicencia = NivelLicencia.Basico;

    /// <summary>Potencia máxima en vatios de la licencia.</summary>
    [ObservableProperty]
    private int _potenciaMaximaVatios = 100;

    /// <summary>Nombre del operador.</summary>
    [ObservableProperty]
    private string _nombreOperador = string.Empty;

    // --- Radio (Rig) ---

    /// <summary>Host del demonio rigctld.</summary>
    [ObservableProperty]
    private string _rigHost = "localhost";

    /// <summary>Puerto TCP de rigctld.</summary>
    [ObservableProperty]
    private int _rigPuerto = 4532;

    /// <summary>Intervalo de polling del radio en ms.</summary>
    [ObservableProperty]
    private int _rigIntervaloPollingMs = 500;

    /// <summary>Potencia máxima del radio en vatios.</summary>
    [ObservableProperty]
    private double _rigPotenciaMaximaVatios = 100.0;

    /// <summary>Timeout de conexión al radio en ms.</summary>
    [ObservableProperty]
    private int _rigTimeoutMs = 5000;

    // --- Rotador ---

    /// <summary>Host del demonio rotctld.</summary>
    [ObservableProperty]
    private string _rotadorHost = "localhost";

    /// <summary>Puerto TCP de rotctld.</summary>
    [ObservableProperty]
    private int _rotadorPuerto = 4533;

    /// <summary>Intervalo de polling del rotador en ms.</summary>
    [ObservableProperty]
    private int _rotadorIntervaloPollingMs = 1000;

    /// <summary>Umbral de cambio en grados del rotador.</summary>
    [ObservableProperty]
    private double _rotadorUmbralCambioGrados = 0.5;

    /// <summary>Timeout de conexión al rotador en ms.</summary>
    [ObservableProperty]
    private int _rotadorTimeoutMs = 5000;

    // --- Audio ---

    /// <summary>Dispositivo de entrada de audio.</summary>
    [ObservableProperty]
    private string _dispositivoEntrada = string.Empty;

    /// <summary>Dispositivo de salida de audio.</summary>
    [ObservableProperty]
    private string _dispositivoSalida = string.Empty;

    /// <summary>Frecuencia de muestreo en Hz.</summary>
    [ObservableProperty]
    private int _frecuenciaMuestreo = 48_000;

    // --- DX Cluster ---

    /// <summary>Servidor del DX Cluster.</summary>
    [ObservableProperty]
    private string _dxClusterServidor = "dxc.ve7cc.net";

    /// <summary>Puerto del servidor DX Cluster.</summary>
    [ObservableProperty]
    private int _dxClusterPuerto = 7300;

    /// <summary>Indicativo para autenticarse en el DX Cluster.</summary>
    [ObservableProperty]
    private string _dxClusterIndicativo = string.Empty;

    /// <summary>Timeout de conexión al DX Cluster en ms.</summary>
    [ObservableProperty]
    private int _dxClusterTimeoutMs = 10_000;

    /// <summary>Retraso de reconexión del DX Cluster en ms.</summary>
    [ObservableProperty]
    private int _dxClusterRetrasoReconexionMs = 5_000;

    /// <summary>Máximo de intentos de reconexión del DX Cluster.</summary>
    [ObservableProperty]
    private int _dxClusterMaxIntentosReconexion = 5;

    // --- General ---

    /// <summary>Ruta de la base de datos.</summary>
    [ObservableProperty]
    private string _rutaBaseDatos = string.Empty;

    /// <summary>Idioma de la interfaz.</summary>
    [ObservableProperty]
    private string _idiomaInterfaz = "es";

    /// <summary>Iniciar la aplicación minimizada.</summary>
    [ObservableProperty]
    private bool _iniciarMinimizado;

    /// <summary>Mostrar notificaciones de escritorio.</summary>
    [ObservableProperty]
    private bool _mostrarNotificaciones = true;

    // --- Estado ---

    /// <summary>Mensaje de estado para mostrar al usuario tras guardar/cancelar.</summary>
    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    /// <summary>Indica si se guardó exitosamente (para cerrar la ventana).</summary>
    [ObservableProperty]
    private bool _guardadoExitoso;

    /// <summary>Valores de RegionItu disponibles para el ComboBox.</summary>
    public IReadOnlyList<RegionItu> RegionesItu { get; } = Enum.GetValues<RegionItu>();

    /// <summary>Valores de NivelLicencia disponibles para el ComboBox.</summary>
    public IReadOnlyList<NivelLicencia> NivelesLicencia { get; } = Enum.GetValues<NivelLicencia>();

    /// <summary>Frecuencias de muestreo comunes disponibles.</summary>
    public IReadOnlyList<int> FrecuenciasMuestreoDisponibles { get; } = [8000, 11025, 22050, 44100, 48000, 96000];

    /// <summary>
    /// Crea el ViewModel de configuración inyectando el servicio de persistencia.
    /// </summary>
    /// <param name="servicioConfiguracion">Servicio para cargar y guardar la configuración.</param>
    public ConfiguracionViewModel(IServicioConfiguracion servicioConfiguracion)
    {
        _servicioConfiguracion = servicioConfiguracion ?? throw new ArgumentNullException(nameof(servicioConfiguracion));
    }

    /// <summary>
    /// Carga la configuración desde disco y la aplica a las propiedades del ViewModel.
    /// </summary>
    [RelayCommand]
    private async Task CargarAsync()
    {
        ConfiguracionCompleta config = await _servicioConfiguracion.CargarAsync().ConfigureAwait(false);
        AplicarDesdeConfiguracion(config);
        MensajeEstado = string.Empty;
    }

    /// <summary>
    /// Guarda la configuración actual en disco.
    /// </summary>
    [RelayCommand]
    private async Task GuardarAsync()
    {
        ConfiguracionCompleta config = ConstruirConfiguracion();
        await _servicioConfiguracion.GuardarAsync(config).ConfigureAwait(false);
        MensajeEstado = "Configuración guardada correctamente.";
        GuardadoExitoso = true;
    }

    /// <summary>
    /// Cancela los cambios recargando la configuración desde disco.
    /// </summary>
    [RelayCommand]
    private async Task CancelarAsync()
    {
        ConfiguracionCompleta config = await _servicioConfiguracion.CargarAsync().ConfigureAwait(false);
        AplicarDesdeConfiguracion(config);
        MensajeEstado = "Cambios descartados.";
    }

    /// <summary>
    /// Aplica los valores de una ConfiguracionCompleta a las propiedades del ViewModel.
    /// </summary>
    private void AplicarDesdeConfiguracion(ConfiguracionCompleta config)
    {
        // Estación
        IndicativoPropio = config.Estacion.IndicativoPropio;
        Localizador = config.Estacion.Localizador;
        RegionItu = config.Estacion.RegionItu;
        NivelLicencia = config.Estacion.NivelLicencia;
        PotenciaMaximaVatios = config.Estacion.PotenciaMaximaVatios;
        NombreOperador = config.Estacion.Nombre;

        // Rig
        RigHost = config.Rig.Host;
        RigPuerto = config.Rig.Puerto;
        RigIntervaloPollingMs = config.Rig.IntervaloPollingMs;
        RigPotenciaMaximaVatios = config.Rig.PotenciaMaximaVatios;
        RigTimeoutMs = config.Rig.TimeoutMs;

        // Rotador
        RotadorHost = config.Rotador.Host;
        RotadorPuerto = config.Rotador.Puerto;
        RotadorIntervaloPollingMs = config.Rotador.IntervaloPollingMs;
        RotadorUmbralCambioGrados = config.Rotador.UmbralCambioGrados;
        RotadorTimeoutMs = config.Rotador.TimeoutMs;

        // Audio
        DispositivoEntrada = config.Audio.DispositivoEntrada;
        DispositivoSalida = config.Audio.DispositivoSalida;
        FrecuenciaMuestreo = config.Audio.FrecuenciaMuestreo;

        // DX Cluster
        DxClusterServidor = config.DxCluster.Servidor;
        DxClusterPuerto = config.DxCluster.Puerto;
        DxClusterIndicativo = config.DxCluster.IndicativoPropio;
        DxClusterTimeoutMs = config.DxCluster.TimeoutMs;
        DxClusterRetrasoReconexionMs = config.DxCluster.RetrasoReconexionMs;
        DxClusterMaxIntentosReconexion = config.DxCluster.MaxIntentosReconexion;

        // General
        RutaBaseDatos = config.General.RutaBaseDatos;
        IdiomaInterfaz = config.General.IdiomaInterfaz;
        IniciarMinimizado = config.General.IniciarMinimizado;
        MostrarNotificaciones = config.General.MostrarNotificaciones;
    }

    /// <summary>
    /// Construye una ConfiguracionCompleta a partir de las propiedades actuales del ViewModel.
    /// </summary>
    private ConfiguracionCompleta ConstruirConfiguracion()
    {
        return new ConfiguracionCompleta
        {
            Estacion = new ConfiguracionEstacion
            {
                IndicativoPropio = IndicativoPropio,
                Localizador = Localizador,
                RegionItu = RegionItu,
                NivelLicencia = NivelLicencia,
                PotenciaMaximaVatios = PotenciaMaximaVatios,
                Nombre = NombreOperador
            },
            Rig = new ConfiguracionRigDto
            {
                Host = RigHost,
                Puerto = RigPuerto,
                IntervaloPollingMs = RigIntervaloPollingMs,
                PotenciaMaximaVatios = RigPotenciaMaximaVatios,
                TimeoutMs = RigTimeoutMs
            },
            Rotador = new ConfiguracionRotadorDto
            {
                Host = RotadorHost,
                Puerto = RotadorPuerto,
                IntervaloPollingMs = RotadorIntervaloPollingMs,
                UmbralCambioGrados = RotadorUmbralCambioGrados,
                TimeoutMs = RotadorTimeoutMs
            },
            Audio = new ConfiguracionAudio
            {
                DispositivoEntrada = DispositivoEntrada,
                DispositivoSalida = DispositivoSalida,
                FrecuenciaMuestreo = FrecuenciaMuestreo
            },
            DxCluster = new ConfiguracionDxClusterDto
            {
                Servidor = DxClusterServidor,
                Puerto = DxClusterPuerto,
                IndicativoPropio = DxClusterIndicativo,
                TimeoutMs = DxClusterTimeoutMs,
                RetrasoReconexionMs = DxClusterRetrasoReconexionMs,
                MaxIntentosReconexion = DxClusterMaxIntentosReconexion
            },
            General = new ConfiguracionGeneral
            {
                RutaBaseDatos = RutaBaseDatos,
                IdiomaInterfaz = IdiomaInterfaz,
                IniciarMinimizado = IniciarMinimizado,
                MostrarNotificaciones = MostrarNotificaciones
            }
        };
    }
}
