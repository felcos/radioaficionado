using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RadioAficionado.Mobile.Vistas;

/// <summary>
/// Vista del panel de Logbook móvil con lista de QSOs, búsqueda y botón de creación.
/// </summary>
public partial class PanelLogbookMobile : UserControl
{
    /// <summary>
    /// Crea la vista del panel de logbook móvil.
    /// </summary>
    public PanelLogbookMobile()
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
