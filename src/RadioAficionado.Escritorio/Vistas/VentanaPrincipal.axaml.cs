using Avalonia.Controls;
using RadioAficionado.Escritorio.ViewModels;

namespace RadioAficionado.Escritorio.Vistas;

/// <summary>
/// Ventana principal de la aplicación de escritorio.
/// </summary>
public partial class VentanaPrincipal : Window
{
    /// <summary>
    /// Inicializa la ventana principal con su ViewModel.
    /// </summary>
    public VentanaPrincipal()
    {
        InitializeComponent();
        DataContext = new VentanaPrincipalViewModel();
    }
}
