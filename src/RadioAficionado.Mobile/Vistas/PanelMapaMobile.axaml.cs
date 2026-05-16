using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RadioAficionado.Mobile.Vistas;

/// <summary>
/// Vista del panel de Mapa móvil con estadísticas de contactos por región.
/// </summary>
public partial class PanelMapaMobile : UserControl
{
    /// <summary>
    /// Crea la vista del panel de mapa móvil.
    /// </summary>
    public PanelMapaMobile()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Inicializa los componentes XAML.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
