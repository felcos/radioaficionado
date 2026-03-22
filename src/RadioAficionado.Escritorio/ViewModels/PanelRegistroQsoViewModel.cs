using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para el formulario de registro de QSO.
/// </summary>
public partial class PanelRegistroQsoViewModel : ViewModelBase
{
    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    [ObservableProperty]
    private string _indicativoContacto = string.Empty;

    /// <summary>
    /// Reporte de señal enviado (RST TX).
    /// </summary>
    [ObservableProperty]
    private string _senalEnviada = "59";

    /// <summary>
    /// Reporte de señal recibido (RST RX).
    /// </summary>
    [ObservableProperty]
    private string _senalRecibida = "59";

    /// <summary>
    /// Localizador Maidenhead de la estación contactada.
    /// </summary>
    [ObservableProperty]
    private string _localizador = string.Empty;

    /// <summary>
    /// Notas adicionales del contacto.
    /// </summary>
    [ObservableProperty]
    private string _notas = string.Empty;

    /// <summary>
    /// Nombre del operador contactado.
    /// </summary>
    [ObservableProperty]
    private string _nombre = string.Empty;

    /// <summary>
    /// Total de QSOs registrados en la sesión actual.
    /// </summary>
    [ObservableProperty]
    private int _totalQsos = 0;

    /// <summary>
    /// QSOs registrados recientemente en esta sesión.
    /// </summary>
    public ObservableCollection<QsoRecienteVm> QsosRecientes { get; } = new();

    /// <summary>
    /// Guarda el QSO actual y limpia el formulario.
    /// </summary>
    [RelayCommand]
    private async Task GuardarQsoAsync()
    {
        if (string.IsNullOrWhiteSpace(IndicativoContacto))
        {
            return;
        }

        // TODO: Enviar comando vía MediatR
        await Task.CompletedTask;

        QsoRecienteVm qsoReciente = new QsoRecienteVm
        {
            Hora = DateTimeOffset.UtcNow.ToString("HH:mm"),
            Indicativo = IndicativoContacto,
            SenalEnviada = SenalEnviada,
            SenalRecibida = SenalRecibida
        };
        QsosRecientes.Insert(0, qsoReciente);
        TotalQsos++;

        IndicativoContacto = string.Empty;
        SenalRecibida = "59";
        Localizador = string.Empty;
        Notas = string.Empty;
        Nombre = string.Empty;
    }

    /// <summary>
    /// Limpia todos los campos del formulario de QSO.
    /// </summary>
    [RelayCommand]
    private void LimpiarFormulario()
    {
        IndicativoContacto = string.Empty;
        SenalEnviada = "59";
        SenalRecibida = "59";
        Localizador = string.Empty;
        Notas = string.Empty;
        Nombre = string.Empty;
    }
}

/// <summary>
/// ViewModel para un QSO reciente mostrado en la lista de la sesión.
/// </summary>
public partial class QsoRecienteVm : ViewModelBase
{
    /// <summary>
    /// Hora UTC del contacto.
    /// </summary>
    [ObservableProperty]
    private string _hora = string.Empty;

    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    [ObservableProperty]
    private string _indicativo = string.Empty;

    /// <summary>
    /// Reporte de señal enviado.
    /// </summary>
    [ObservableProperty]
    private string _senalEnviada = string.Empty;

    /// <summary>
    /// Reporte de señal recibido.
    /// </summary>
    [ObservableProperty]
    private string _senalRecibida = string.Empty;
}
