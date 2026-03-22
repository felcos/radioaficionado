using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para el panel de control del radio.
/// </summary>
public partial class PanelRigViewModel : ViewModelBase
{
    /// <summary>
    /// Frecuencia actual formateada para mostrar (ej: "14.074.000").
    /// </summary>
    [ObservableProperty]
    private string _frecuenciaDisplay = "14.074.000";

    /// <summary>
    /// Modo de operación actual (FT8, SSB, CW, etc.).
    /// </summary>
    [ObservableProperty]
    private string _modoActual = "FT8";

    /// <summary>
    /// Banda actual (20m, 40m, etc.).
    /// </summary>
    [ObservableProperty]
    private string _bandaActual = "20m";

    /// <summary>
    /// Nivel de señal en S-units (0-9).
    /// </summary>
    [ObservableProperty]
    private int _nivelSenal = 0;

    /// <summary>
    /// Indica si el radio está transmitiendo.
    /// </summary>
    [ObservableProperty]
    private bool _transmitiendo = false;

    /// <summary>
    /// Potencia de transmisión en vatios.
    /// </summary>
    [ObservableProperty]
    private double _potenciaVatios = 50.0;

    /// <summary>
    /// Indica si hay conexión con el radio.
    /// </summary>
    [ObservableProperty]
    private bool _conectado = false;

    /// <summary>
    /// Host de rigctld para conexión.
    /// </summary>
    [ObservableProperty]
    private string _hostRigctld = "localhost";

    /// <summary>
    /// Puerto de rigctld para conexión.
    /// </summary>
    [ObservableProperty]
    private int _puertoRigctld = 4532;

    /// <summary>
    /// Texto del botón PTT según el estado.
    /// </summary>
    public string TextoPtt => Transmitiendo ? "TX ON" : "TX";

    /// <summary>
    /// Conecta o desconecta del radio vía rigctld.
    /// </summary>
    [RelayCommand]
    private async Task ConectarAsync()
    {
        // TODO: Conectar vía IControlRig
        await Task.CompletedTask;
        Conectado = !Conectado;
    }

    /// <summary>
    /// Alterna el estado de transmisión (PTT).
    /// </summary>
    [RelayCommand]
    private void CambiarPtt()
    {
        Transmitiendo = !Transmitiendo;
        OnPropertyChanged(nameof(TextoPtt));
    }

    /// <summary>
    /// Formatea una frecuencia en Hz con separadores de punto (ej: 14.074.000).
    /// </summary>
    /// <param name="hz">Frecuencia en hercios.</param>
    /// <returns>Cadena formateada con separadores de miles usando punto.</returns>
    public static string FormatearFrecuencia(long hz)
    {
        string hzStr = hz.ToString();
        if (hzStr.Length > 6)
        {
            return $"{hzStr[..^6]}.{hzStr[^6..^3]}.{hzStr[^3..]}";
        }
        if (hzStr.Length > 3)
        {
            return $"{hzStr[..^3]}.{hzStr[^3..]}";
        }
        return hzStr;
    }
}
