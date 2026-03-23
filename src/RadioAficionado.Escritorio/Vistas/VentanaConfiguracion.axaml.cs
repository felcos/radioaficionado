using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Escritorio.ViewModels;

namespace RadioAficionado.Escritorio.Vistas;

/// <summary>
/// Ventana modal de configuración de la aplicación.
/// Resuelve el ConfiguracionViewModel desde DI y carga la configuración al abrirse.
/// </summary>
public partial class VentanaConfiguracion : Window
{
    /// <summary>
    /// Inicializa la ventana de configuración, resuelve el ViewModel desde DI
    /// y lanza la carga de la configuración desde disco.
    /// </summary>
    public VentanaConfiguracion()
    {
        InitializeComponent();

        ConfiguracionViewModel viewModel = App.Servicios!.GetRequiredService<ConfiguracionViewModel>();
        DataContext = viewModel;

        // Cargar configuración al abrir la ventana
        Opened += async (_, _) =>
        {
            await viewModel.CargarCommand.ExecuteAsync(null);
        };

        // Cerrar la ventana al guardar exitosamente (desde el hilo de UI)
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ConfiguracionViewModel.GuardadoExitoso) && viewModel.GuardadoExitoso)
            {
                Dispatcher.UIThread.Post(() => Close());
            }
        };
    }
}
