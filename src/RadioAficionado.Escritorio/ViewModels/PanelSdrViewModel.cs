using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Sdr;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para el panel de control del receptor SDR.
/// Gestiona la conexión, configuración y visualización de datos
/// desde dispositivos SDR vía SoapySDR, alimentando el waterfall.
/// Integra el servicio de waterfall SDR para visualización de espectro IQ.
/// </summary>
public partial class PanelSdrViewModel : ViewModelBase, IDisposable
{
    private readonly IReceptorSdr _receptorSdr;
    private readonly IServicioWaterfall _servicioWaterfall;
    private readonly IServicioWaterfallSdr _servicioWaterfallSdr;
    private readonly ILogger<PanelSdrViewModel> _logger;
    private bool _disposed;

    /// <summary>
    /// Frecuencia central de sintonización en Hz.
    /// </summary>
    [ObservableProperty]
    private double _frecuenciaCentral = 145_000_000;

    /// <summary>
    /// Ancho de banda del filtro en Hz.
    /// </summary>
    [ObservableProperty]
    private double _anchoDeBanda = 200_000;

    /// <summary>
    /// Ganancia del LNA en dB.
    /// </summary>
    [ObservableProperty]
    private double _ganancia = 40.0;

    /// <summary>
    /// Indica si el receptor SDR está conectado.
    /// </summary>
    [ObservableProperty]
    private bool _estaConectado;

    /// <summary>
    /// Lista de dispositivos SDR disponibles en el sistema.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<DispositivoSdr> _dispositivosDisponibles = Array.Empty<DispositivoSdr>();

    /// <summary>
    /// Dispositivo SDR seleccionado por el usuario.
    /// </summary>
    [ObservableProperty]
    private DispositivoSdr? _dispositivoSeleccionado;

    /// <summary>
    /// Texto descriptivo del estado actual de la conexión SDR.
    /// </summary>
    [ObservableProperty]
    private string _estadoConexion = "Desconectado";

    /// <summary>
    /// Fuente de datos activa del waterfall (Audio o SDR).
    /// </summary>
    [ObservableProperty]
    private FuenteDeDatosWaterfall _fuenteWaterfall = FuenteDeDatosWaterfall.Ninguna;

    /// <summary>
    /// Servicio de waterfall SDR expuesto para enlace de datos.
    /// </summary>
    public IServicioWaterfallSdr ServicioWaterfallSdr => _servicioWaterfallSdr;

    /// <summary>
    /// Crea una nueva instancia del ViewModel del panel SDR.
    /// </summary>
    /// <param name="receptorSdr">Servicio de receptor SDR.</param>
    /// <param name="servicioWaterfall">Servicio de waterfall para visualización de espectro.</param>
    /// <param name="servicioWaterfallSdr">Servicio de waterfall especializado para fuentes SDR.</param>
    /// <param name="logger">Logger para diagnóstico.</param>
    public PanelSdrViewModel(
        IReceptorSdr receptorSdr,
        IServicioWaterfall servicioWaterfall,
        IServicioWaterfallSdr servicioWaterfallSdr,
        ILogger<PanelSdrViewModel> logger)
    {
        _receptorSdr = receptorSdr ?? throw new ArgumentNullException(nameof(receptorSdr));
        _servicioWaterfall = servicioWaterfall ?? throw new ArgumentNullException(nameof(servicioWaterfall));
        _servicioWaterfallSdr = servicioWaterfallSdr ?? throw new ArgumentNullException(nameof(servicioWaterfallSdr));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _receptorSdr.MuestrasRecibidas += AlRecibirMuestras;
    }

    /// <summary>
    /// Conecta al dispositivo SDR seleccionado e inicia la recepción de muestras IQ.
    /// </summary>
    [RelayCommand]
    private async Task ConectarAsync()
    {
        try
        {
            if (DispositivoSeleccionado is null)
            {
                EstadoConexion = "Seleccione un dispositivo SDR primero.";
                return;
            }

            EstadoConexion = "Conectando...";
            string argumentosConexion = $"driver={DispositivoSeleccionado.Controlador}";

            if (!string.IsNullOrWhiteSpace(DispositivoSeleccionado.NumeroSerie))
            {
                argumentosConexion += $",serial={DispositivoSeleccionado.NumeroSerie}";
            }

            await _receptorSdr.ConectarAsync(argumentosConexion).ConfigureAwait(false);

            if (_receptorSdr.EstaConectado)
            {
                await _receptorSdr.ConfigurarFrecuenciaAsync(FrecuenciaCentral).ConfigureAwait(false);
                await _receptorSdr.ConfigurarGananciaAsync(Ganancia).ConfigureAwait(false);
                await _receptorSdr.ConfigurarAnchoDeBandaAsync(AnchoDeBanda).ConfigureAwait(false);
            }

            EstaConectado = _receptorSdr.EstaConectado;

            // Iniciar waterfall SDR automáticamente al conectar
            if (EstaConectado)
            {
                await _servicioWaterfallSdr.IniciarConSdrAsync(_receptorSdr).ConfigureAwait(false);
                FuenteWaterfall = FuenteDeDatosWaterfall.Sdr;
            }

            EstadoConexion = EstaConectado
                ? $"Conectado — {DispositivoSeleccionado.Nombre} — {FrecuenciaCentral / 1_000_000:F3} MHz"
                : "Error al conectar.";

            _logger.LogInformation("SDR conectado: {Dispositivo}.", DispositivoSeleccionado.Nombre);
        }
        catch (Exception ex)
        {
            EstadoConexion = $"Error: {ex.Message}";
            EstaConectado = false;
            _logger.LogError(ex, "Error al conectar al dispositivo SDR.");
        }
    }

    /// <summary>
    /// Desconecta del dispositivo SDR activo.
    /// </summary>
    [RelayCommand]
    private async Task DesconectarAsync()
    {
        try
        {
            EstadoConexion = "Desconectando...";

            // Detener waterfall SDR antes de desconectar
            if (_servicioWaterfallSdr.EstaActivo)
            {
                await _servicioWaterfallSdr.DetenerAsync().ConfigureAwait(false);
                FuenteWaterfall = FuenteDeDatosWaterfall.Ninguna;
            }

            await _receptorSdr.DesconectarAsync().ConfigureAwait(false);
            EstaConectado = false;
            EstadoConexion = "Desconectado";
            _logger.LogInformation("SDR desconectado.");
        }
        catch (Exception ex)
        {
            EstadoConexion = $"Error al desconectar: {ex.Message}";
            _logger.LogError(ex, "Error al desconectar del dispositivo SDR.");
        }
    }

    /// <summary>
    /// Busca los dispositivos SDR disponibles en el sistema.
    /// </summary>
    [RelayCommand]
    private Task BuscarDispositivosAsync()
    {
        try
        {
            EstadoConexion = "Buscando dispositivos SDR...";
            DispositivosDisponibles = _receptorSdr.ObtenerDispositivosDisponibles();

            EstadoConexion = DispositivosDisponibles.Count > 0
                ? $"Se encontraron {DispositivosDisponibles.Count} dispositivo(s) SDR."
                : "No se encontraron dispositivos SDR.";

            _logger.LogInformation("Búsqueda SDR completada. Dispositivos: {Cantidad}.", DispositivosDisponibles.Count);
        }
        catch (Exception ex)
        {
            EstadoConexion = $"Error al buscar: {ex.Message}";
            _logger.LogError(ex, "Error al buscar dispositivos SDR.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Cambia la fuente de datos del waterfall entre Audio y SDR.
    /// Si se selecciona SDR, inicia el waterfall SDR con el receptor actual.
    /// Si se selecciona Audio, detiene el waterfall SDR.
    /// </summary>
    [RelayCommand]
    private async Task CambiarFuenteWaterfallAsync()
    {
        try
        {
            if (FuenteWaterfall == FuenteDeDatosWaterfall.Sdr)
            {
                // Cambiar a Audio: detener waterfall SDR
                await _servicioWaterfallSdr.DetenerAsync().ConfigureAwait(false);
                FuenteWaterfall = FuenteDeDatosWaterfall.Audio;
                _logger.LogInformation("Fuente de waterfall cambiada a Audio.");
            }
            else if (EstaConectado)
            {
                // Cambiar a SDR: iniciar waterfall SDR
                await _servicioWaterfallSdr.IniciarConSdrAsync(_receptorSdr).ConfigureAwait(false);
                FuenteWaterfall = FuenteDeDatosWaterfall.Sdr;
                _logger.LogInformation("Fuente de waterfall cambiada a SDR.");
            }
            else
            {
                _logger.LogWarning("No se puede cambiar a SDR sin un dispositivo conectado.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar la fuente del waterfall.");
        }
    }

    /// <summary>
    /// Maneja las muestras IQ recibidas del receptor SDR.
    /// Las reenvía al servicio de waterfall para visualización.
    /// </summary>
    private void AlRecibirMuestras(object? sender, MuestrasSdrEventArgs e)
    {
        // Las muestras IQ se pueden transformar y alimentar al waterfall.
        // El servicio de waterfall procesará las muestras para generar líneas de espectro.
        _logger.LogTrace(
            "Muestras SDR recibidas: {CantidadI} I, {CantidadQ} Q, Fc={Frecuencia} Hz.",
            e.MuestrasI.Length, e.MuestrasQ.Length, e.FrecuenciaCentralHz);
    }

    /// <summary>
    /// Libera los recursos y desuscribe los eventos del receptor SDR.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _receptorSdr.MuestrasRecibidas -= AlRecibirMuestras;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
