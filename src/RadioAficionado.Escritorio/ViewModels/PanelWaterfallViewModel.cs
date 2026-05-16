using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Nativo.Dsp;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para el control de waterfall en vivo.
/// Conecta el servicio de waterfall con el control visual,
/// gestionando el inicio/parada del procesamiento y la configuracion de FFT.
/// </summary>
public partial class PanelWaterfallViewModel : ViewModelBase
{
    private readonly IServicioWaterfall _servicioWaterfall;

    [ObservableProperty]
    private bool _estaActivo;

    [ObservableProperty]
    private int _tamanoFft = 2048;

    [ObservableProperty]
    private string _estadoWaterfall = "Detenido";

    [ObservableProperty]
    private string _implementacionFft = FabricaTransformadaFourier.ObtenerNombreImplementacion();

    /// <summary>
    /// Evento disparado cuando hay una nueva linea de espectro lista para el waterfall.
    /// El control de waterfall se suscribe a este evento para actualizar la visualizacion.
    /// </summary>
    public event EventHandler<LineaEspectroEventArgs>? LineaEspectroRecibida;

    /// <summary>
    /// Tamanos de FFT disponibles para seleccion en la UI.
    /// </summary>
    public IReadOnlyList<int> TamanosDisponibles { get; } = new List<int>
    {
        512, 1024, 2048, 4096, 8192
    };

    /// <summary>
    /// Crea una nueva instancia del ViewModel de waterfall.
    /// </summary>
    /// <param name="servicioWaterfall">Servicio de waterfall para procesar audio.</param>
    public PanelWaterfallViewModel(IServicioWaterfall servicioWaterfall)
    {
        _servicioWaterfall = servicioWaterfall ?? throw new ArgumentNullException(nameof(servicioWaterfall));
        _servicioWaterfall.LineaEspectroGenerada += AlRecibirLineaEspectro;
    }

    /// <summary>
    /// Inicia el procesamiento de waterfall.
    /// </summary>
    [RelayCommand]
    private async Task IniciarWaterfallAsync()
    {
        try
        {
            await _servicioWaterfall.IniciarAsync(TamanoFft);
            EstaActivo = true;
            EstadoWaterfall = $"Activo — FFT {TamanoFft} — {_servicioWaterfall.TasaDeMuestreoHz} Hz";
        }
        catch (InvalidOperationException ex)
        {
            EstadoWaterfall = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Detiene el procesamiento de waterfall.
    /// </summary>
    [RelayCommand]
    private async Task DetenerWaterfallAsync()
    {
        await _servicioWaterfall.DetenerAsync();
        EstaActivo = false;
        EstadoWaterfall = "Detenido";
    }

    /// <summary>
    /// Maneja las lineas de espectro recibidas del servicio y las retransmite a la UI.
    /// </summary>
    private void AlRecibirLineaEspectro(object? sender, LineaEspectroEventArgs e)
    {
        LineaEspectroRecibida?.Invoke(this, e);
    }
}
