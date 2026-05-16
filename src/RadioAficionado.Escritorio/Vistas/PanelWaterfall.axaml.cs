using Avalonia.Controls;
using Avalonia.Threading;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Escritorio.Controles;
using RadioAficionado.Escritorio.ViewModels;
using RadioAficionado.Nativo.Dsp;

namespace RadioAficionado.Escritorio.Vistas;

/// <summary>
/// Code-behind del panel de waterfall con visualizacion de espectro.
/// Conecta el evento LineaEspectroRecibida del ViewModel al control visual.
/// </summary>
public partial class PanelWaterfall : UserControl
{
    private PanelWaterfallViewModel? _viewModel;

    /// <summary>
    /// Inicializa el componente visual del panel de waterfall.
    /// </summary>
    public PanelWaterfall()
    {
        InitializeComponent();
        DataContextChanged += AlCambiarDataContext;
    }

    /// <summary>
    /// Al cambiar el DataContext, suscribe el evento de espectro al control waterfall.
    /// </summary>
    private void AlCambiarDataContext(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.LineaEspectroRecibida -= AlRecibirLinea;
        }

        _viewModel = DataContext as PanelWaterfallViewModel;

        if (_viewModel is not null)
        {
            _viewModel.LineaEspectroRecibida += AlRecibirLinea;
        }
    }

    /// <summary>
    /// Convierte LineaEspectroEventArgs a LineaEspectro y alimenta el control visual.
    /// </summary>
    private void AlRecibirLinea(object? sender, LineaEspectroEventArgs e)
    {
        LineaEspectro linea = new()
        {
            MarcaDeTiempo = e.MarcaDeTiempo,
            MagnitudesDb = e.MagnitudesDb,
            ResolucionHz = e.ResolucionHz,
            FrecuenciaMinHz = e.FrecuenciaMinHz,
            FrecuenciaMaxHz = e.FrecuenciaMaxHz
        };

        ControlWaterfall? control = this.FindControl<ControlWaterfall>("WaterfallControl");
        if (control is not null)
        {
            Dispatcher.UIThread.Post(() => control.AgregarLinea(linea));
        }
    }
}
