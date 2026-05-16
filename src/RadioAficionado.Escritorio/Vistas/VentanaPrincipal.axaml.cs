using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Escritorio.Controles;
using RadioAficionado.Escritorio.ViewModels;
using RadioAficionado.Nativo.Dsp;

namespace RadioAficionado.Escritorio.Vistas;

/// <summary>
/// Ventana principal de la aplicación de escritorio.
/// Resuelve su ViewModel desde el contenedor de inyección de dependencias
/// y conecta el waterfall principal al flujo de espectro.
/// </summary>
public partial class VentanaPrincipal : Window
{
    /// <summary>
    /// Inicializa la ventana principal resolviendo el ViewModel desde App.Servicios.
    /// Suscribe el waterfall principal al evento de espectro.
    /// </summary>
    public VentanaPrincipal()
    {
        InitializeComponent();
        VentanaPrincipalViewModel viewModel = App.Servicios!.GetRequiredService<VentanaPrincipalViewModel>();
        DataContext = viewModel;

        // Conectar el waterfall de la cabecera al flujo de espectro
        viewModel.PanelWaterfall.LineaEspectroRecibida += AlRecibirLineaEspectro;
    }

    /// <summary>
    /// Alimenta el control waterfall principal con cada linea de espectro recibida.
    /// </summary>
    private void AlRecibirLineaEspectro(object? sender, LineaEspectroEventArgs e)
    {
        LineaEspectro linea = new()
        {
            MarcaDeTiempo = e.MarcaDeTiempo,
            MagnitudesDb = e.MagnitudesDb,
            ResolucionHz = e.ResolucionHz,
            FrecuenciaMinHz = e.FrecuenciaMinHz,
            FrecuenciaMaxHz = e.FrecuenciaMaxHz
        };

        ControlWaterfall? control = this.FindControl<ControlWaterfall>("Waterfall");
        if (control is not null)
        {
            Dispatcher.UIThread.Post(() => control.AgregarLinea(linea));
        }
    }
}
