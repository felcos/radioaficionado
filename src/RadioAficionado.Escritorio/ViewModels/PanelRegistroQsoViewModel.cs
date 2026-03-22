using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using RadioAficionado.Aplicacion.Qsos.RegistrarQso;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para el formulario de registro de QSO.
/// Envía comandos a la capa de aplicación vía MediatR.
/// </summary>
public partial class PanelRegistroQsoViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PanelRegistroQsoViewModel> _logger;

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
    /// Indicativo propio de la estación (configurable).
    /// </summary>
    [ObservableProperty]
    private string _indicativoPropio = string.Empty;

    /// <summary>
    /// Frecuencia actual en Hz (sincronizada desde PanelRigViewModel).
    /// </summary>
    [ObservableProperty]
    private long _frecuenciaActualHz = 14_074_000;

    /// <summary>
    /// Modo actual (sincronizado desde PanelRigViewModel).
    /// </summary>
    [ObservableProperty]
    private string _modoActual = "FT8";

    /// <summary>
    /// Potencia en vatios (sincronizada desde PanelRigViewModel).
    /// </summary>
    [ObservableProperty]
    private double _potenciaVatios = 50.0;

    /// <summary>
    /// Total de QSOs registrados en la sesión actual.
    /// </summary>
    [ObservableProperty]
    private int _totalQsos = 0;

    /// <summary>
    /// Mensaje de error del último intento de guardado.
    /// </summary>
    [ObservableProperty]
    private string _mensajeError = string.Empty;

    /// <summary>
    /// QSOs registrados recientemente en esta sesión.
    /// </summary>
    public ObservableCollection<QsoRecienteVm> QsosRecientes { get; } = new();

    /// <summary>
    /// Crea el ViewModel del panel de registro de QSO.
    /// </summary>
    /// <param name="mediator">Mediador para enviar comandos a la capa de aplicación.</param>
    /// <param name="logger">Logger para diagnóstico.</param>
    public PanelRegistroQsoViewModel(IMediator mediator, ILogger<PanelRegistroQsoViewModel> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Guarda el QSO actual enviando un RegistrarQsoComando vía MediatR y limpia el formulario.
    /// </summary>
    [RelayCommand]
    private async Task GuardarQsoAsync()
    {
        if (string.IsNullOrWhiteSpace(IndicativoContacto))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(IndicativoPropio))
        {
            MensajeError = "Debe configurar el indicativo propio antes de registrar QSOs.";
            _logger.LogWarning("Intento de guardar QSO sin indicativo propio configurado.");
            return;
        }

        MensajeError = string.Empty;

        bool modoValido = ModoOperacionExtensiones.IntentarDesdeAdif(ModoActual, out ModoOperacion modo);
        if (!modoValido)
        {
            modo = ModoOperacion.FT8;
            _logger.LogWarning("Modo '{Modo}' no reconocido, usando FT8 por defecto.", ModoActual);
        }

        RegistrarQsoComando comando = new RegistrarQsoComando
        {
            IndicativoPropio = IndicativoPropio,
            IndicativoContacto = IndicativoContacto,
            FechaHoraInicio = DateTimeOffset.UtcNow,
            FrecuenciaHz = FrecuenciaActualHz,
            Modo = modo,
            SenalEnviada = SenalEnviada,
            SenalRecibida = SenalRecibida,
            Potencia = PotenciaVatios,
            LocalizadorContacto = string.IsNullOrWhiteSpace(Localizador) ? null : Localizador,
            Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas
        };

        try
        {
            _logger.LogInformation("Registrando QSO con {Indicativo}...", IndicativoContacto);
            RegistrarQsoResultado resultado = await _mediator.Send(comando);

            if (resultado.Exitoso)
            {
                _logger.LogInformation("QSO registrado exitosamente. ID: {QsoId}", resultado.QsoId);

                QsoRecienteVm qsoReciente = new QsoRecienteVm
                {
                    Hora = DateTimeOffset.UtcNow.ToString("HH:mm"),
                    Indicativo = IndicativoContacto,
                    SenalEnviada = SenalEnviada,
                    SenalRecibida = SenalRecibida
                };
                QsosRecientes.Insert(0, qsoReciente);
                TotalQsos++;

                LimpiarCamposFormulario();
            }
            else
            {
                MensajeError = resultado.Error ?? "Error desconocido al registrar el QSO.";
                _logger.LogWarning("Error al registrar QSO: {Error}", resultado.Error);
            }
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al guardar: {ex.Message}";
            _logger.LogError(ex, "Excepción al registrar QSO con {Indicativo}.", IndicativoContacto);
        }
    }

    /// <summary>
    /// Limpia todos los campos del formulario de QSO.
    /// </summary>
    [RelayCommand]
    private void LimpiarFormulario()
    {
        LimpiarCamposFormulario();
        MensajeError = string.Empty;
    }

    /// <summary>
    /// Limpia los campos editables del formulario sin borrar el mensaje de error.
    /// </summary>
    private void LimpiarCamposFormulario()
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
