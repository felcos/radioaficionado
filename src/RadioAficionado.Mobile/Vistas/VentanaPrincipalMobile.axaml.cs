using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Mobile.ViewModels;

namespace RadioAficionado.Mobile.Vistas;

/// <summary>
/// Vista principal de la aplicación móvil con navegación por pestañas inferiores.
/// </summary>
public partial class VentanaPrincipalMobile : UserControl
{
    /// <summary>
    /// Crea la vista principal móvil y asigna el DataContext desde el contenedor de DI.
    /// </summary>
    public VentanaPrincipalMobile()
    {
        InitializeComponent();

        if (App.Servicios is not null)
        {
            DataContext = App.Servicios.GetRequiredService<VentanaPrincipalMobileViewModel>();
        }
    }

    /// <summary>
    /// Inicializa los componentes XAML.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
