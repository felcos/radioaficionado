using System.Collections.ObjectModel;
using System.IO.Ports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.Audio;
using RadioAficionado.Nativo.Rig;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para el panel de control del radio.
/// Soporta CAT serial directo (como WSJT-X) y rigctld TCP.
/// Persiste la configuracion de conexion en disco.
/// Bandas y modos son mutuamente exclusivos.
/// </summary>
public partial class PanelRigViewModel : ViewModelBase, IDisposable
{
    private IControlRig? _controlRig;
    private readonly ILogger<PanelRigViewModel> _logger;
    private readonly IAudioPipeline _audioPipeline;
    private readonly IServicioWaterfall _servicioWaterfall;
    private readonly ConfiguracionConexionRig _configPersistida;
    private PeriodicTimer? _timerPolling;
    private CancellationTokenSource? _ctsPolling;
    private bool _disposed;

    private static readonly IReadOnlyDictionary<string, long> _frecuenciasPorBanda = new Dictionary<string, long>
    {
        ["160m"] = 1_840_000,
        ["80m"] = 3_573_000,
        ["40m"] = 7_074_000,
        ["30m"] = 10_136_000,
        ["20m"] = 14_074_000,
        ["17m"] = 18_100_000,
        ["15m"] = 21_074_000,
        ["12m"] = 24_915_000,
        ["10m"] = 28_074_000,
        ["6m"] = 50_313_000,
        ["2m"] = 144_174_000
    };

    private static readonly IReadOnlyDictionary<string, (ModoOperacion Modo, SubModoOperacion? SubModo)> _mapeoDeModos =
        new Dictionary<string, (ModoOperacion, SubModoOperacion?)>
        {
            ["LSB"] = (ModoOperacion.SSB, SubModoOperacion.LSB),
            ["USB"] = (ModoOperacion.SSB, SubModoOperacion.USB),
            ["CW"] = (ModoOperacion.CW, null),
            ["FT8"] = (ModoOperacion.FT8, null),
            ["FT4"] = (ModoOperacion.FT4, null),
            ["AM"] = (ModoOperacion.AM, null),
            ["FM"] = (ModoOperacion.FM, null),
            ["RTTY"] = (ModoOperacion.RTTY, null),
            ["PSK"] = (ModoOperacion.PSK, SubModoOperacion.PSK31),
            ["JS8"] = (ModoOperacion.JS8, null),
            ["JT65"] = (ModoOperacion.JT65, null),
        };

    // ================================================================
    // ESTADO DEL RADIO
    // ================================================================

    [ObservableProperty] private string _frecuenciaDisplay = "14.074.000";
    [ObservableProperty] private long _frecuenciaHz = 14_074_000;
    [ObservableProperty] private string _modoActual = "FT8";
    [ObservableProperty] private string _bandaActual = "20m";

    /// <summary>Banda seleccionada (solo una activa a la vez).</summary>
    [ObservableProperty] private string _bandaSeleccionada = "20m";

    /// <summary>Modo seleccionado (solo uno activo a la vez).</summary>
    [ObservableProperty] private string _modoSeleccionado = "FT8";

    [ObservableProperty] private int _nivelSenal = 0;
    [ObservableProperty] private double _nivelSenalPorcentaje = 0.0;
    [ObservableProperty] private bool _esSMeterAlto = false;
    [ObservableProperty] private bool _transmitiendo = false;
    [ObservableProperty] private double _potenciaVatios = 50.0;
    [ObservableProperty] private double _potenciaPorcentaje = 50.0;
    [ObservableProperty] private char _vfoActivo = 'A';
    [ObservableProperty] private bool _splitActivo = false;

    // ================================================================
    // CONEXION
    // ================================================================

    [ObservableProperty] private bool _conectado = false;
    [ObservableProperty] private bool _configuracionVisible = false;
    [ObservableProperty] private bool _usarCatSerial = true;
    [ObservableProperty] private string _tipoConexion = "Sin conexion";
    [ObservableProperty] private string _estadoConexionDetalle = "";
    [ObservableProperty] private bool _conectando = false;

    // CAT Serial
    [ObservableProperty] private string _puertoSerieSeleccionado = "";
    [ObservableProperty] private int _velocidadBaudios = 38400;
    [ObservableProperty] private ModeloRadio _modeloRadioSeleccionado = ModeloRadio.Automatico;
    [ObservableProperty] private int _bitsDeDatos = 8;
    [ObservableProperty] private StopBits _bitsDeParada = StopBits.One;
    [ObservableProperty] private Parity _paridad = Parity.None;
    [ObservableProperty] private bool _rtsEnable = true;
    [ObservableProperty] private bool _dtrEnable = false;
    [ObservableProperty] private string _metodoPtt = "CAT";
    [ObservableProperty] private int _intervaloPollingMs = 200;

    // rigctld TCP
    [ObservableProperty] private string _hostRigctld = "localhost";
    [ObservableProperty] private int _puertoRigctld = 4532;

    // Audio
    /// <summary>ID del dispositivo de audio de entrada seleccionado.</summary>
    [ObservableProperty] private string _dispositivoAudioSeleccionado = "";

    /// <summary>ViewModel del dispositivo de audio seleccionado (para binding en ComboBox).</summary>
    [ObservableProperty] private DispositivoAudioVm? _dispositivoAudioEntradaVm;

    /// <summary>Tasa de muestreo en Hz.</summary>
    [ObservableProperty] private int _tasaDeMuestreoHz = 48000;

    /// <summary>Indica si el audio esta capturando.</summary>
    [ObservableProperty] private bool _audioCapturando = false;

    /// <summary>Dispositivos de audio disponibles.</summary>
    public ObservableCollection<DispositivoAudioVm> DispositivosAudioDisponibles { get; } = new();

    /// <summary>Sincroniza el ID string cuando cambia la seleccion del ComboBox.</summary>
    partial void OnDispositivoAudioEntradaVmChanged(DispositivoAudioVm? value)
    {
        if (value is not null)
        {
            DispositivoAudioSeleccionado = value.Id;
        }
    }

    // Sintonizacion
    [ObservableProperty] private long _pasoSintonizacion = 1000;
    [ObservableProperty] private int _indicePasoSeleccionado = 3;

    // ================================================================
    // COLECCIONES
    // ================================================================

    /// <summary>Puertos serie disponibles.</summary>
    public ObservableCollection<string> PuertosDisponibles { get; } = new();

    /// <summary>Velocidades de baudios.</summary>
    public IReadOnlyList<int> VelocidadesBaudios { get; } = new List<int> { 4800, 9600, 19200, 38400, 57600, 115200 };

    /// <summary>Modelos de radio.</summary>
    public IReadOnlyList<ModeloRadio> ModelosRadio { get; } = Enum.GetValues<ModeloRadio>();

    /// <summary>Opciones de bits de datos.</summary>
    public IReadOnlyList<int> OpcionesBitsDeDatos { get; } = new List<int> { 7, 8 };

    /// <summary>Opciones de bits de parada.</summary>
    public IReadOnlyList<StopBits> OpcionesBitsDeParada { get; } = new List<StopBits> { StopBits.One, StopBits.OnePointFive, StopBits.Two };

    /// <summary>Opciones de paridad.</summary>
    public IReadOnlyList<Parity> OpcionesParidad { get; } = new List<Parity> { Parity.None, Parity.Even, Parity.Odd, Parity.Mark, Parity.Space };

    /// <summary>Metodos de PTT.</summary>
    public IReadOnlyList<string> MetodosPtt { get; } = new List<string> { "CAT", "DTR", "RTS", "VOX" };

    /// <summary>Tasas de muestreo disponibles.</summary>
    public IReadOnlyList<int> TasasDeMuestreo { get; } = new List<int> { 12000, 24000, 48000 };

    /// <summary>Pasos de sintonizacion.</summary>
    public IReadOnlyList<(long Hz, string Etiqueta)> PasosDeSintonizacion { get; } = new List<(long, string)>
    {
        (1, "1"), (10, "10"), (100, "100"), (1_000, "1k"), (10_000, "10k"), (100_000, "100k"), (1_000_000, "1M")
    };

    /// <summary>Bandas disponibles.</summary>
    public IReadOnlyList<string> BandasDisponibles { get; } = new List<string>
    {
        "160m", "80m", "40m", "30m", "20m", "17m", "15m", "12m", "10m", "6m", "2m"
    };

    /// <summary>Modos disponibles.</summary>
    public IReadOnlyList<string> ModosDisponibles { get; } = new List<string>
    {
        "LSB", "USB", "CW", "FT8", "FT4", "AM", "FM", "RTTY", "PSK", "JS8", "JT65"
    };

    // ================================================================
    // PROPIEDADES COMPUTADAS
    // ================================================================

    /// <summary>Texto del boton PTT.</summary>
    public string TextoPtt => Transmitiendo ? "TX" : "TX";

    /// <summary>Texto del boton de conexion.</summary>
    public string TextoBotonConexion => Conectado ? "DESCONECTAR" : "CONECTAR";

    /// <summary>Texto del boton VFO.</summary>
    public string TextoVfo => $"VFO {VfoActivo}";

    /// <summary>Etiqueta del paso de sintonizacion.</summary>
    public string EtiquetaPaso => PasosDeSintonizacion.FirstOrDefault(p => p.Hz == PasoSintonizacion).Etiqueta ?? $"{PasoSintonizacion}";

    // ================================================================
    // HELPERS PARA EXCLUSIVIDAD DE BANDA/MODO
    // Cada banda/modo tiene una propiedad bool que indica si esta activa.
    // Al cambiar BandaSeleccionada/ModoSeleccionado, se notifican todas.
    // ================================================================

    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda160m => BandaSeleccionada == "160m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda80m => BandaSeleccionada == "80m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda40m => BandaSeleccionada == "40m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda30m => BandaSeleccionada == "30m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda20m => BandaSeleccionada == "20m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda17m => BandaSeleccionada == "17m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda15m => BandaSeleccionada == "15m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda12m => BandaSeleccionada == "12m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda10m => BandaSeleccionada == "10m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda6m => BandaSeleccionada == "6m";
    /// <summary>Indica si la banda especificada esta seleccionada.</summary>
    public bool EsBanda2m => BandaSeleccionada == "2m";

    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoLSB => ModoSeleccionado == "LSB";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoUSB => ModoSeleccionado == "USB";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoCW => ModoSeleccionado == "CW";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoFT8 => ModoSeleccionado == "FT8";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoFT4 => ModoSeleccionado == "FT4";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoAM => ModoSeleccionado == "AM";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoFM => ModoSeleccionado == "FM";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoRTTY => ModoSeleccionado == "RTTY";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoPSK => ModoSeleccionado == "PSK";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoJS8 => ModoSeleccionado == "JS8";
    /// <summary>Indica si el modo especificado esta seleccionado.</summary>
    public bool EsModoJT65 => ModoSeleccionado == "JT65";

    /// <summary>Notifica cambio en todas las propiedades de banda.</summary>
    private void NotificarCambioBandas()
    {
        OnPropertyChanged(nameof(EsBanda160m));
        OnPropertyChanged(nameof(EsBanda80m));
        OnPropertyChanged(nameof(EsBanda40m));
        OnPropertyChanged(nameof(EsBanda30m));
        OnPropertyChanged(nameof(EsBanda20m));
        OnPropertyChanged(nameof(EsBanda17m));
        OnPropertyChanged(nameof(EsBanda15m));
        OnPropertyChanged(nameof(EsBanda12m));
        OnPropertyChanged(nameof(EsBanda10m));
        OnPropertyChanged(nameof(EsBanda6m));
        OnPropertyChanged(nameof(EsBanda2m));
    }

    /// <summary>Notifica cambio en todas las propiedades de modo.</summary>
    private void NotificarCambioModos()
    {
        OnPropertyChanged(nameof(EsModoLSB));
        OnPropertyChanged(nameof(EsModoUSB));
        OnPropertyChanged(nameof(EsModoCW));
        OnPropertyChanged(nameof(EsModoFT8));
        OnPropertyChanged(nameof(EsModoFT4));
        OnPropertyChanged(nameof(EsModoAM));
        OnPropertyChanged(nameof(EsModoFM));
        OnPropertyChanged(nameof(EsModoRTTY));
        OnPropertyChanged(nameof(EsModoPSK));
        OnPropertyChanged(nameof(EsModoJS8));
        OnPropertyChanged(nameof(EsModoJT65));
    }

    // ================================================================
    // CONSTRUCTOR
    // ================================================================

    /// <summary>
    /// Crea el ViewModel cargando la configuracion persistida.
    /// </summary>
    public PanelRigViewModel(IAudioPipeline audioPipeline, IServicioWaterfall servicioWaterfall, ILogger<PanelRigViewModel> logger)
    {
        _audioPipeline = audioPipeline ?? throw new ArgumentNullException(nameof(audioPipeline));
        _servicioWaterfall = servicioWaterfall ?? throw new ArgumentNullException(nameof(servicioWaterfall));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _configPersistida = ConfiguracionConexionRig.Cargar();
        CargarDesdeConfiguracion();

        RefrescarPuertosDisponibles();
        _ = RefrescarDispositivosAudioAsync();
    }

    /// <summary>Aplica la configuracion persistida a las propiedades de la UI.</summary>
    private void CargarDesdeConfiguracion()
    {
        UsarCatSerial = _configPersistida.UsarCatSerial;
        PuertoSerieSeleccionado = _configPersistida.PuertoSerie;
        VelocidadBaudios = _configPersistida.VelocidadBaudios;
        ModeloRadioSeleccionado = _configPersistida.ModeloRadio;
        BitsDeDatos = _configPersistida.BitsDeDatos;
        BitsDeParada = _configPersistida.BitsDeParada;
        Paridad = _configPersistida.Paridad;
        RtsEnable = _configPersistida.RtsEnable;
        DtrEnable = _configPersistida.DtrEnable;
        MetodoPtt = _configPersistida.MetodoPtt;
        IntervaloPollingMs = _configPersistida.IntervaloPollingMs;
        HostRigctld = _configPersistida.HostRigctld;
        PuertoRigctld = _configPersistida.PuertoRigctld;
        DispositivoAudioSeleccionado = _configPersistida.DispositivoAudioEntrada;
        TasaDeMuestreoHz = _configPersistida.TasaDeMuestreoHz;
    }

    /// <summary>Guarda la configuracion actual de la UI a disco.</summary>
    private void GuardarConfiguracion()
    {
        _configPersistida.UsarCatSerial = UsarCatSerial;
        _configPersistida.PuertoSerie = PuertoSerieSeleccionado;
        _configPersistida.VelocidadBaudios = VelocidadBaudios;
        _configPersistida.ModeloRadio = ModeloRadioSeleccionado;
        _configPersistida.BitsDeDatos = BitsDeDatos;
        _configPersistida.BitsDeParada = BitsDeParada;
        _configPersistida.Paridad = Paridad;
        _configPersistida.RtsEnable = RtsEnable;
        _configPersistida.DtrEnable = DtrEnable;
        _configPersistida.MetodoPtt = MetodoPtt;
        _configPersistida.IntervaloPollingMs = IntervaloPollingMs;
        _configPersistida.HostRigctld = HostRigctld;
        _configPersistida.PuertoRigctld = PuertoRigctld;
        _configPersistida.DispositivoAudioEntrada = DispositivoAudioSeleccionado;
        _configPersistida.TasaDeMuestreoHz = TasaDeMuestreoHz;
        _configPersistida.Guardar();
    }

    // ================================================================
    // CONEXION
    // ================================================================

    /// <summary>Conecta o desconecta del radio. Guarda la configuracion al conectar.</summary>
    [RelayCommand]
    private async Task ConectarAsync()
    {
        if (Conectando)
        {
            return;
        }

        try
        {
            if (!Conectado)
            {
                Conectando = true;
                EstadoConexionDetalle = "Conectando...";

                // Guardar configuracion antes de conectar
                GuardarConfiguracion();

                if (UsarCatSerial)
                {
                    await ConectarCatSerialAsync();
                }
                else
                {
                    await ConectarRigctldAsync();
                }

                if (Conectado)
                {
                    EstadoConexionDetalle = "Conectado";
                    OnPropertyChanged(nameof(TextoBotonConexion));
                    ConfiguracionVisible = false;
                    IniciarPolling();
                    await IniciarCapturaAudioAsync();
                }
                else
                {
                    EstadoConexionDetalle = "No se pudo conectar";
                }
            }
            else
            {
                EstadoConexionDetalle = "Desconectando...";
                DetenerPolling();
                await DetenerCapturaAudioAsync();

                if (_controlRig is not null)
                {
                    await _controlRig.DesconectarAsync();

                    if (_controlRig is IAsyncDisposable disposable)
                    {
                        await disposable.DisposeAsync();
                    }

                    _controlRig = null;
                }

                Conectado = false;
                TipoConexion = "Sin conexion";
                EstadoConexionDetalle = "Desconectado";
                OnPropertyChanged(nameof(TextoBotonConexion));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar/desconectar del radio.");
            Conectado = false;
            TipoConexion = "Error";
            EstadoConexionDetalle = $"Error: {ex.Message}";
            OnPropertyChanged(nameof(TextoBotonConexion));
            DetenerPolling();
        }
        finally
        {
            Conectando = false;
        }
    }

    /// <summary>Prueba la conexion sin iniciar polling.</summary>
    [RelayCommand]
    private async Task TestConexionAsync()
    {
        if (Conectando || Conectado)
        {
            return;
        }

        Conectando = true;
        EstadoConexionDetalle = "Probando conexion...";

        // Guardar config al probar tambien
        GuardarConfiguracion();

        try
        {
            IControlRig clienteTest;

            if (UsarCatSerial)
            {
                if (string.IsNullOrWhiteSpace(PuertoSerieSeleccionado))
                {
                    EstadoConexionDetalle = "Seleccione un puerto COM";
                    return;
                }

                ConfiguracionPuertoSerie configuracion = CrearConfiguracionSerial();
                clienteTest = new ClienteCatSerial(configuracion);
                await clienteTest.ConectarAsync();
            }
            else
            {
                clienteTest = new ClienteRigctld();
                await clienteTest.ConectarAsync(HostRigctld, PuertoRigctld);
            }

            if (clienteTest.EstaConectado)
            {
                EstadoRig estado = await clienteTest.ObtenerEstadoAsync();
                string frecuencia = FormatearFrecuencia(estado.Frecuencia.Hz);
                string modelo = clienteTest.ModeloRadio ?? "Detectado";
                EstadoConexionDetalle = $"OK — {modelo} @ {frecuencia} Hz, Modo: {estado.Modo}";
            }
            else
            {
                EstadoConexionDetalle = "No se pudo conectar";
            }

            await clienteTest.DesconectarAsync();

            if (clienteTest is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            EstadoConexionDetalle = $"Error: {ex.Message}";
            _logger.LogWarning(ex, "Test de conexion fallido.");
        }
        finally
        {
            Conectando = false;
        }
    }

    private async Task ConectarCatSerialAsync()
    {
        if (string.IsNullOrWhiteSpace(PuertoSerieSeleccionado))
        {
            EstadoConexionDetalle = "Seleccione un puerto COM";
            return;
        }

        ConfiguracionPuertoSerie configuracion = CrearConfiguracionSerial();
        ClienteCatSerial cliente = new(configuracion);
        await cliente.ConectarAsync();
        _controlRig = cliente;
        Conectado = cliente.EstaConectado;

        if (Conectado)
        {
            string modelo = cliente.ModeloRadio ?? "Auto";
            TipoConexion = $"CAT {PuertoSerieSeleccionado} @ {VelocidadBaudios} | {modelo}";
        }
    }

    private async Task ConectarRigctldAsync()
    {
        ClienteRigctld cliente = new();
        await cliente.ConectarAsync(HostRigctld, PuertoRigctld);
        _controlRig = cliente;
        Conectado = cliente.EstaConectado;

        if (Conectado)
        {
            string modelo = cliente.ModeloRadio ?? "Desconocido";
            TipoConexion = $"rigctld @ {HostRigctld}:{PuertoRigctld} | {modelo}";
        }
    }

    private ConfiguracionPuertoSerie CrearConfiguracionSerial()
    {
        return new ConfiguracionPuertoSerie
        {
            PuertoSerie = PuertoSerieSeleccionado,
            VelocidadBaudios = VelocidadBaudios,
            Modelo = ModeloRadioSeleccionado,
            BitsDeDatos = BitsDeDatos,
            BitsDeParada = BitsDeParada,
            Paridad = Paridad,
            RtsEnable = RtsEnable,
            DtrEnable = DtrEnable,
            IntervaloPollingMs = IntervaloPollingMs
        };
    }

    [RelayCommand]
    private void MostrarConfiguracion()
    {
        ConfiguracionVisible = !ConfiguracionVisible;
        if (ConfiguracionVisible)
        {
            RefrescarPuertosDisponibles();
        }
    }

    [RelayCommand]
    private void RefrescarPuertosDisponibles()
    {
        string seleccionAnterior = PuertoSerieSeleccionado;
        PuertosDisponibles.Clear();

        try
        {
            foreach (string puerto in ClienteCatSerial.ObtenerPuertosDisponibles())
            {
                PuertosDisponibles.Add(puerto);
            }

            // Restaurar seleccion anterior si sigue disponible
            if (!string.IsNullOrWhiteSpace(seleccionAnterior) && PuertosDisponibles.Contains(seleccionAnterior))
            {
                PuertoSerieSeleccionado = seleccionAnterior;
            }
            else if (PuertosDisponibles.Count > 0 && string.IsNullOrWhiteSpace(PuertoSerieSeleccionado))
            {
                PuertoSerieSeleccionado = PuertosDisponibles[0];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al enumerar puertos serie.");
        }
    }

    // ================================================================
    // AUDIO
    // ================================================================

    /// <summary>Refresca la lista de dispositivos de audio disponibles.</summary>
    [RelayCommand]
    private async Task RefrescarDispositivosAudioAsync()
    {
        try
        {
            IReadOnlyList<DispositivoAudio> dispositivos = await _audioPipeline.ObtenerDispositivosAsync();
            DispositivosAudioDisponibles.Clear();

            foreach (DispositivoAudio dispositivo in dispositivos)
            {
                if (dispositivo.EsEntrada)
                {
                    DispositivosAudioDisponibles.Add(new DispositivoAudioVm(
                        dispositivo.Id, dispositivo.Nombre, dispositivo.EsEntrada));
                }
            }

            // Restaurar seleccion anterior si sigue disponible
            DispositivoAudioVm? vmSeleccionado = null;

            if (!string.IsNullOrWhiteSpace(DispositivoAudioSeleccionado))
            {
                foreach (DispositivoAudioVm vm in DispositivosAudioDisponibles)
                {
                    if (vm.Id == DispositivoAudioSeleccionado)
                    {
                        vmSeleccionado = vm;
                        break;
                    }
                }
            }

            if (vmSeleccionado is null && DispositivosAudioDisponibles.Count > 0)
            {
                vmSeleccionado = DispositivosAudioDisponibles[0];
            }

            DispositivoAudioEntradaVm = vmSeleccionado;
            if (vmSeleccionado is not null)
            {
                DispositivoAudioSeleccionado = vmSeleccionado.Id;
            }

            _logger.LogInformation("Dispositivos de audio detectados: {Cantidad}", DispositivosAudioDisponibles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al enumerar dispositivos de audio.");
        }
    }

    /// <summary>Inicia la captura de audio desde el dispositivo seleccionado.</summary>
    private async Task IniciarCapturaAudioAsync()
    {
        if (string.IsNullOrWhiteSpace(DispositivoAudioSeleccionado))
        {
            _logger.LogWarning("No hay dispositivo de audio seleccionado. La captura no se iniciara.");
            return;
        }

        try
        {
            if (_audioPipeline.EstaActivo)
            {
                await _audioPipeline.DetenerCapturaAsync();
            }

            await _audioPipeline.IniciarCapturaAsync(DispositivoAudioSeleccionado, TasaDeMuestreoHz);
            AudioCapturando = true;
            _logger.LogInformation("Captura de audio iniciada: {Dispositivo} @ {TasaMuestreo} Hz.",
                DispositivoAudioSeleccionado, TasaDeMuestreoHz);

            // Iniciar waterfall automaticamente para visualizar el espectro
            if (!_servicioWaterfall.EstaActivo)
            {
                await _servicioWaterfall.IniciarAsync(2048);
                _logger.LogInformation("Waterfall iniciado automaticamente con FFT 2048.");
            }
        }
        catch (Exception ex)
        {
            AudioCapturando = false;
            _logger.LogError(ex, "Error al iniciar captura de audio desde {Dispositivo}.",
                DispositivoAudioSeleccionado);
        }
    }

    /// <summary>Detiene la captura de audio.</summary>
    private async Task DetenerCapturaAudioAsync()
    {
        try
        {
            if (_servicioWaterfall.EstaActivo)
            {
                await _servicioWaterfall.DetenerAsync();
            }

            if (_audioPipeline.EstaActivo)
            {
                await _audioPipeline.DetenerCapturaAsync();
            }

            AudioCapturando = false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al detener captura de audio.");
        }
    }

    // ================================================================
    // CONTROL DEL RADIO
    // ================================================================

    [RelayCommand]
    private async Task CambiarPttAsync()
    {
        try
        {
            if (!Conectado || _controlRig is null) { return; }
            bool nuevoEstado = !Transmitiendo;
            await _controlRig.CambiarPttAsync(nuevoEstado);
            Transmitiendo = nuevoEstado;
            OnPropertyChanged(nameof(TextoPtt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar PTT.");
        }
    }

    [RelayCommand]
    private async Task CambiarFrecuenciaAsync(long frecuenciaEnHz)
    {
        try
        {
            if (frecuenciaEnHz <= 0) { return; }

            FrecuenciaHz = frecuenciaEnHz;
            FrecuenciaDisplay = FormatearFrecuencia(frecuenciaEnHz);

            string bandaDetectada = DeterminarBandaDesdeFrecuencia(frecuenciaEnHz);
            BandaActual = bandaDetectada;
            BandaSeleccionada = bandaDetectada;
            NotificarCambioBandas();

            if (Conectado && _controlRig is not null)
            {
                Frecuencia frecuencia = Frecuencia.DesdeHz(frecuenciaEnHz);
                await _controlRig.CambiarFrecuenciaAsync(frecuencia);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar frecuencia a {Frecuencia} Hz.", frecuenciaEnHz);
        }
    }

    [RelayCommand]
    private async Task CambiarModoAsync(ModoOperacion modo)
    {
        try
        {
            if (!Conectado || _controlRig is null) { return; }
            await _controlRig.CambiarModoAsync(modo);
            ModoActual = modo.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar modo a {Modo}.", modo);
        }
    }

    /// <summary>Cambia la banda — mutuamente exclusivo, envia frecuencia al radio.</summary>
    [RelayCommand]
    private async Task CambiarBandaAsync(string banda)
    {
        if (string.IsNullOrWhiteSpace(banda)) { return; }

        BandaSeleccionada = banda;
        BandaActual = banda;
        NotificarCambioBandas();

        if (_frecuenciasPorBanda.TryGetValue(banda, out long frecuenciaHz))
        {
            FrecuenciaHz = frecuenciaHz;
            FrecuenciaDisplay = FormatearFrecuencia(frecuenciaHz);

            if (Conectado && _controlRig is not null)
            {
                try
                {
                    Frecuencia frecuencia = Frecuencia.DesdeHz(frecuenciaHz);
                    await _controlRig.CambiarFrecuenciaAsync(frecuencia);
                    _logger.LogInformation("Banda cambiada a {Banda} ({Frecuencia} Hz).", banda, frecuenciaHz);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cambiar banda a {Banda}.", banda);
                }
            }
        }
    }

    /// <summary>Cambia el modo — mutuamente exclusivo, envia modo al radio.</summary>
    [RelayCommand]
    private async Task CambiarModoTextoAsync(string nombreModo)
    {
        if (string.IsNullOrWhiteSpace(nombreModo)) { return; }

        ModoSeleccionado = nombreModo;
        ModoActual = nombreModo;
        NotificarCambioModos();

        if (Conectado && _controlRig is not null && _mapeoDeModos.TryGetValue(nombreModo, out (ModoOperacion Modo, SubModoOperacion? SubModo) mapeo))
        {
            try
            {
                await _controlRig.CambiarModoAsync(mapeo.Modo, mapeo.SubModo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar modo a {Modo}.", nombreModo);
            }
        }
    }

    [RelayCommand]
    private async Task CambiarVfoAsync()
    {
        char nuevoVfo = VfoActivo == 'A' ? 'B' : 'A';

        if (Conectado && _controlRig is not null)
        {
            try
            {
                await _controlRig.CambiarVfoAsync(nuevoVfo);
                VfoActivo = nuevoVfo;
                OnPropertyChanged(nameof(TextoVfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar VFO a {Vfo}.", nuevoVfo);
            }
        }
        else
        {
            VfoActivo = nuevoVfo;
            OnPropertyChanged(nameof(TextoVfo));
        }
    }

    // ================================================================
    // SINTONIZACION
    // ================================================================

    [RelayCommand]
    private async Task FrecuenciaArribaAsync()
    {
        await CambiarFrecuenciaAsync(FrecuenciaHz + PasoSintonizacion);
    }

    [RelayCommand]
    private async Task FrecuenciaAbajoAsync()
    {
        long nuevaFrecuencia = FrecuenciaHz - PasoSintonizacion;
        if (nuevaFrecuencia > 0)
        {
            await CambiarFrecuenciaAsync(nuevaFrecuencia);
        }
    }

    [RelayCommand]
    private void SeleccionarPaso(string indiceTexto)
    {
        if (int.TryParse(indiceTexto, out int indice) && indice >= 0 && indice < PasosDeSintonizacion.Count)
        {
            IndicePasoSeleccionado = indice;
            PasoSintonizacion = PasosDeSintonizacion[indice].Hz;
            OnPropertyChanged(nameof(EtiquetaPaso));
        }
    }

    // ================================================================
    // AUXILIARES
    // ================================================================

    /// <summary>Formatea frecuencia en Hz con separadores.</summary>
    public static string FormatearFrecuencia(long hz)
    {
        string hzStr = hz.ToString();
        if (hzStr.Length > 6) { return $"{hzStr[..^6]}.{hzStr[^6..^3]}.{hzStr[^3..]}"; }
        if (hzStr.Length > 3) { return $"{hzStr[..^3]}.{hzStr[^3..]}"; }
        return hzStr;
    }

    private static double CalcularPorcentajeSenal(int nivelSenal)
    {
        if (nivelSenal <= 0) { return 0.0; }
        if (nivelSenal <= 9) { return nivelSenal * (60.0 / 9.0); }
        double exceso = Math.Min(nivelSenal - 9, 6);
        return 60.0 + (exceso * (40.0 / 6.0));
    }

    private static string DeterminarBandaDesdeFrecuencia(long frecuenciaHz)
    {
        foreach (KeyValuePair<string, long> kvp in _frecuenciasPorBanda)
        {
            long frecBanda = kvp.Value;
            long margen = frecBanda switch
            {
                < 5_000_000 => 500_000,
                < 15_000_000 => 350_000,
                < 30_000_000 => 500_000,
                _ => 2_000_000
            };

            if (Math.Abs(frecuenciaHz - frecBanda) < margen) { return kvp.Key; }
        }

        BandaRadio? banda = BandaRadioExtensiones.DesdeFrecuencia(Frecuencia.DesdeHz(frecuenciaHz));
        if (banda.HasValue)
        {
            return banda.Value.ToString().Replace("Banda", "");
        }

        return "---";
    }

    // ================================================================
    // POLLING
    // ================================================================

    private void IniciarPolling()
    {
        DetenerPolling();
        _ctsPolling = new CancellationTokenSource();
        int intervalo = Math.Max(IntervaloPollingMs, 100);
        _timerPolling = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalo));
        CancellationToken token = _ctsPolling.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timerPolling.WaitForNextTickAsync(token))
                {
                    await ActualizarEstadoDesdeRigAsync(token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el bucle de polling del rig.");
                // Si fallo de polling, marcar como desconectado
                Conectado = false;
                TipoConexion = "Conexion perdida";
                EstadoConexionDetalle = $"Polling fallido: {ex.Message}";
                OnPropertyChanged(nameof(TextoBotonConexion));
            }
        }, token);
    }

    private void DetenerPolling()
    {
        _ctsPolling?.Cancel();
        _ctsPolling?.Dispose();
        _ctsPolling = null;
        _timerPolling?.Dispose();
        _timerPolling = null;
    }

    private async Task ActualizarEstadoDesdeRigAsync(CancellationToken ct)
    {
        try
        {
            if (_controlRig is null) { return; }

            // Verificar que sigue conectado
            if (!_controlRig.EstaConectado)
            {
                Conectado = false;
                TipoConexion = "Conexion perdida";
                EstadoConexionDetalle = "El radio se desconecto";
                OnPropertyChanged(nameof(TextoBotonConexion));
                DetenerPolling();
                return;
            }

            EstadoRig estado = await _controlRig.ObtenerEstadoAsync(ct);

            FrecuenciaHz = estado.Frecuencia.Hz;
            FrecuenciaDisplay = FormatearFrecuencia(estado.Frecuencia.Hz);
            NivelSenal = estado.NivelSenal;
            NivelSenalPorcentaje = CalcularPorcentajeSenal(estado.NivelSenal);
            EsSMeterAlto = estado.NivelSenal > 9;
            PotenciaVatios = estado.PotenciaVatios;
            Transmitiendo = estado.Transmitiendo;
            VfoActivo = estado.VfoActivo;
            OnPropertyChanged(nameof(TextoPtt));
            OnPropertyChanged(nameof(TextoVfo));

            if (estado.SubModo.HasValue)
            {
                ModoActual = estado.SubModo.Value.ToString();
                ModoSeleccionado = estado.SubModo.Value.ToString();
            }
            else
            {
                ModoActual = estado.Modo.ToString();
                ModoSeleccionado = estado.Modo.ToString();
            }
            NotificarCambioModos();

            string bandaDetectada = DeterminarBandaDesdeFrecuencia(estado.Frecuencia.Hz);
            BandaActual = bandaDetectada;
            BandaSeleccionada = bandaDetectada;
            NotificarCambioBandas();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al obtener estado del rig durante polling.");
        }
    }

    /// <summary>Libera recursos.</summary>
    public void Dispose()
    {
        if (_disposed) { return; }
        DetenerPolling();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
