using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RadioAficionado.Mobile.Vistas;

/// <summary>
/// Vista del panel de Propagación móvil con índices solares y predicciones por banda.
/// </summary>
public partial class PanelPropagacionMobile : UserControl
{
    /// <summary>
    /// Crea la vista del panel de propagación móvil.
    /// </summary>
    public PanelPropagacionMobile()
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
