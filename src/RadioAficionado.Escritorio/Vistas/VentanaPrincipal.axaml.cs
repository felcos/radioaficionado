using Avalonia.Controls;
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
    }
}
