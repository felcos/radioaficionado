using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para el panel de control del radio.
/// Conecta con el servicio IControlRig para comunicarse con rigctld.
/// </summary>
public partial class PanelRigViewModel : ViewModelBase, IDisposable
{
    private readonly IControlRig _controlRig;
    private readonly ILogger<PanelRigViewModel> _logger;
    private PeriodicTimer? _timerPolling;
    private CancellationTokenSource? _ctsPolling;
    private bool _disposed;

    /// <summary>
    /// Frecuencia actual formateada para mostrar (ej: "14.074.000").
    /// </summary>
    [ObservableProperty]
    private string _frecuenciaDisplay = "14.074.000";

    /// <summary>
    /// Frecuencia actual en Hz (para operaciones internas).
    /// </summary>
    [ObservableProperty]
    private long _frecuenciaHz = 14_074_000;

    /// <summary>
    /// Modo de operación actual (FT8, SSB, CW, etc.).
    /// </summary>
    [ObservableProperty]
    private string _modoActual = "FT8";

    /// <summary>
    /// Banda actual (20m, 40m, etc.).
    /// </summary>
    [ObservableProperty]
    private string _bandaActual = "20m";

    /// <summary>
    /// Nivel de señal en S-units (0-9).
    /// </summary>
    [ObservableProperty]
    private int _nivelSenal = 0;

    /// <summary>
    /// Indica si el radio está transmitiendo.
    /// </summary>
    [ObservableProperty]
    private bool _transmitiendo = false;

    /// <summary>
    /// Potencia de transmisión en vatios.
    /// </summary>
    [ObservableProperty]
    private double _potenciaVatios = 50.0;

    /// <summary>
    /// Indica si hay conexión con el radio.
    /// </summary>
    [ObservableProperty]
    private bool _conectado = false;

    /// <summary>
    /// Host de rigctld para conexión.
    /// </summary>
    [ObservableProperty]
    private string _hostRigctld = "localhost";

    /// <summary>
    /// Puerto de rigctld para conexión.
    /// </summary>
    [ObservableProperty]
    private int _puertoRigctld = 4532;

    /// <summary>
    /// Texto del botón PTT según el estado.
    /// </summary>
    public string TextoPtt => Transmitiendo ? "TX ON" : "TX";

    /// <summary>
    /// Crea el ViewModel del panel de control del radio.
    /// </summary>
    /// <param name="controlRig">Servicio de control del radio vía rigctld.</param>
    /// <param name="logger">Logger para diagnóstico.</param>
    public PanelRigViewModel(IControlRig controlRig, ILogger<PanelRigViewModel> logger)
    {
        _controlRig = controlRig ?? throw new ArgumentNullException(nameof(controlRig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Conecta o desconecta del radio vía rigctld.
    /// Al conectar, inicia el polling periódico del estado del radio.
    /// </summary>
    [RelayCommand]
    private async Task ConectarAsync()
    {
        try
        {
            if (!Conectado)
            {
                _logger.LogInformation("Conectando a rigctld en {Host}:{Puerto}...", HostRigctld, PuertoRigctld);
                await _controlRig.ConectarAsync(HostRigctld, PuertoRigctld);
                Conectado = _controlRig.EstaConectado;

                if (Conectado)
                {
                    _logger.LogInformation("Conectado a rigctld. Modelo: {Modelo}", _controlRig.ModeloRadio ?? "Desconocido");
                    IniciarPolling();
                }
            }
            else
            {
                _logger.LogInformation("Desconectando de rigctld...");
                DetenerPolling();
                await _controlRig.DesconectarAsync();
                Conectado = false;
                _logger.LogInformation("Desconectado de rigctld.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar/desconectar de rigctld.");
            Conectado = false;
            DetenerPolling();
        }
    }

    /// <summary>
    /// Alterna el estado de transmisión (PTT).
    /// </summary>
    [RelayCommand]
    private async Task CambiarPttAsync()
    {
        try
        {
            if (!Conectado)
            {
                return;
            }

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

    /// <summary>
    /// Cambia la frecuencia del radio al valor especificado en Hz.
    /// </summary>
    /// <param name="frecuenciaEnHz">Frecuencia objetivo en hercios.</param>
    [RelayCommand]
    private async Task CambiarFrecuenciaAsync(long frecuenciaEnHz)
    {
        try
        {
            if (!Conectado || frecuenciaEnHz <= 0)
            {
                return;
            }

            Frecuencia frecuencia = Frecuencia.DesdeHz(frecuenciaEnHz);
            await _controlRig.CambiarFrecuenciaAsync(frecuencia);
            FrecuenciaHz = frecuenciaEnHz;
            FrecuenciaDisplay = FormatearFrecuencia(frecuenciaEnHz);
            _logger.LogDebug("Frecuencia cambiada a {Frecuencia} Hz.", frecuenciaEnHz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar frecuencia a {Frecuencia} Hz.", frecuenciaEnHz);
        }
    }

    /// <summary>
    /// Cambia el modo de operación del radio.
    /// </summary>
    /// <param name="modo">Modo de operación objetivo.</param>
    [RelayCommand]
    private async Task CambiarModoAsync(ModoOperacion modo)
    {
        try
        {
            if (!Conectado)
            {
                return;
            }

            await _controlRig.CambiarModoAsync(modo);
            ModoActual = modo.ToString();
            _logger.LogDebug("Modo cambiado a {Modo}.", modo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar modo a {Modo}.", modo);
        }
    }

    /// <summary>
    /// Formatea una frecuencia en Hz con separadores de punto (ej: 14.074.000).
    /// </summary>
    /// <param name="hz">Frecuencia en hercios.</param>
    /// <returns>Cadena formateada con separadores de miles usando punto.</returns>
    public static string FormatearFrecuencia(long hz)
    {
        string hzStr = hz.ToString();
        if (hzStr.Length > 6)
        {
            return $"{hzStr[..^6]}.{hzStr[^6..^3]}.{hzStr[^3..]}";
        }
        if (hzStr.Length > 3)
        {
            return $"{hzStr[..^3]}.{hzStr[^3..]}";
        }
        return hzStr;
    }

    /// <summary>
    /// Inicia el polling periódico del estado del radio (cada 500ms).
    /// </summary>
    private void IniciarPolling()
    {
        DetenerPolling();
        _ctsPolling = new CancellationTokenSource();
        _timerPolling = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
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
            catch (OperationCanceledException)
            {
                // Polling cancelado normalmente.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el bucle de polling del rig.");
            }
        }, token);

        _logger.LogDebug("Polling del rig iniciado (cada 500ms).");
    }

    /// <summary>
    /// Detiene el polling periódico del estado del radio.
    /// </summary>
    private void DetenerPolling()
    {
        _ctsPolling?.Cancel();
        _ctsPolling?.Dispose();
        _ctsPolling = null;
        _timerPolling?.Dispose();
        _timerPolling = null;
    }

    /// <summary>
    /// Consulta el estado actual del radio y actualiza las propiedades del ViewModel.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    private async Task ActualizarEstadoDesdeRigAsync(CancellationToken ct)
    {
        try
        {
            EstadoRig estado = await _controlRig.ObtenerEstadoAsync(ct);

            FrecuenciaHz = estado.Frecuencia.Hz;
            FrecuenciaDisplay = FormatearFrecuencia(estado.Frecuencia.Hz);
            ModoActual = estado.Modo.ToString();
            NivelSenal = estado.NivelSenal;
            PotenciaVatios = estado.PotenciaVatios;
            Transmitiendo = estado.Transmitiendo;
            OnPropertyChanged(nameof(TextoPtt));

            BandaRadio? banda = estado.Frecuencia.ObtenerBanda();
            if (banda.HasValue)
            {
                BandaActual = banda.Value.ToString();
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelación normal, no loguear.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al obtener estado del rig durante polling.");
        }
    }

    /// <summary>
    /// Libera los recursos del polling y la conexión con el rig.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DetenerPolling();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
