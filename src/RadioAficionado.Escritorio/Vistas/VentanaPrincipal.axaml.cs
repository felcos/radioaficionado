using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Escritorio.ViewModels;

namespace RadioAficionado.Escritorio.Vistas;

/// <summary>
/// Ventana principal de la aplicación de escritorio.
/// Resuelve su ViewModel desde el contenedor de inyección de dependencias.
/// </summary>
public partial class VentanaPrincipal : Window
{
    /// <summary>
    /// Inicializa la ventana principal resolviendo el ViewModel desde App.Servicios.
    /// </summary>
    public VentanaPrincipal()
    {
        InitializeComponent();
        DataContext = App.Servicios!.GetRequiredService<VentanaPrincipalViewModel>();

        Button botonConfig = this.FindControl<Button>("BotonConfiguracion")!;
        botonConfig.Click += AbrirConfiguracion;
    }

    /// <summary>
    /// Abre la ventana de configuración como diálogo modal.
    /// </summary>
    private async void AbrirConfiguracion(object? sender, RoutedEventArgs e)
    {
        VentanaConfiguracion ventana = new VentanaConfiguracion();
        await ventana.ShowDialog(this);
    }
}
