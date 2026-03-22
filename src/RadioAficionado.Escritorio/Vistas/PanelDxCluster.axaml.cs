using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using RadioAficionado.Escritorio.ViewModels;

namespace RadioAficionado.Escritorio.Vistas;

/// <summary>
/// Vista del panel de DX Cluster que muestra spots DX en tiempo real.
/// </summary>
public partial class PanelDxCluster : UserControl
{
    /// <summary>
    /// Inicializa el control del panel DX Cluster.
    /// </summary>
    public PanelDxCluster()
    {
        InitializeComponent();
    }
}
