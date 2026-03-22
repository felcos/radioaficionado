using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para la tabla de mensajes digitales decodificados.
/// Recibe mensajes desde los decodificadores digitales registrados.
/// </summary>
public partial class PanelMensajesViewModel : ViewModelBase
{
    private readonly ILogger<PanelMensajesViewModel> _logger;

    /// <summary>
    /// Colección de mensajes decodificados mostrados en la tabla.
    /// </summary>
    public ObservableCollection<MensajeDigitalVm> Mensajes { get; } = new();

    /// <summary>
    /// Indica si el decodificador está activo.
    /// </summary>
    [ObservableProperty]
    private bool _decodificando = false;

    /// <summary>
    /// Modo digital activo (FT8, FT4, etc.).
    /// </summary>
    [ObservableProperty]
    private string _modoDigital = "FT8";

    /// <summary>
    /// Período actual del ciclo de decodificación (0-14 para FT8 de 15s).
    /// </summary>
    [ObservableProperty]
    private int _periodoActual = 0;

    /// <summary>
    /// Crea el ViewModel del panel de mensajes digitales.
    /// </summary>
    /// <param name="logger">Logger para diagnóstico.</param>
    public PanelMensajesViewModel(ILogger<PanelMensajesViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Procesa un mensaje decodificado recibido desde un IDecodificadorDigital
    /// y lo agrega a la lista de mensajes visibles.
    /// </summary>
    /// <param name="mensajeDecodificado">Mensaje proveniente del decodificador digital.</param>
    public void ProcesarMensajeDecodificado(MensajeDecodificado mensajeDecodificado)
    {
        if (mensajeDecodificado is null)
        {
            return;
        }

        MensajeDigitalVm mensajeVm = new MensajeDigitalVm
        {
            Hora = mensajeDecodificado.MarcaDeTiempo.ToString("HH:mm:ss"),
            Snr = mensajeDecodificado.Snr,
            DeltaTiempo = mensajeDecodificado.DeltaTiempo,
            FrecuenciaHz = mensajeDecodificado.FrecuenciaAudioHz,
            Mensaje = mensajeDecodificado.Texto,
            IndicativoEmisor = mensajeDecodificado.IndicativoEmisor ?? string.Empty,
            Localizador = mensajeDecodificado.Localizador ?? string.Empty
        };

        AgregarMensaje(mensajeVm);
        _logger.LogDebug("Mensaje decodificado agregado: {Texto}", mensajeDecodificado.Texto);
    }

    /// <summary>
    /// Añade un mensaje al inicio de la tabla, manteniendo un máximo de 500.
    /// </summary>
    /// <param name="mensaje">Mensaje decodificado a agregar.</param>
    public void AgregarMensaje(MensajeDigitalVm mensaje)
    {
        Mensajes.Insert(0, mensaje);
        if (Mensajes.Count > 500)
        {
            Mensajes.RemoveAt(Mensajes.Count - 1);
        }
    }

    /// <summary>
    /// Limpia todos los mensajes de la tabla.
    /// </summary>
    public void LimpiarMensajes()
    {
        Mensajes.Clear();
    }
}

/// <summary>
/// ViewModel para un mensaje digital decodificado (una fila en la tabla de mensajes).
/// </summary>
public partial class MensajeDigitalVm : ViewModelBase
{
    /// <summary>
    /// Hora UTC de recepción.
    /// </summary>
    [ObservableProperty]
    private string _hora = string.Empty;

    /// <summary>
    /// Relación señal/ruido en dB.
    /// </summary>
    [ObservableProperty]
    private int _snr;

    /// <summary>
    /// Delta de tiempo respecto al reloj UTC.
    /// </summary>
    [ObservableProperty]
    private double _deltaTiempo;

    /// <summary>
    /// Frecuencia de audio del mensaje en Hz.
    /// </summary>
    [ObservableProperty]
    private int _frecuenciaHz;

    /// <summary>
    /// Texto del mensaje decodificado.
    /// </summary>
    [ObservableProperty]
    private string _mensaje = string.Empty;

    /// <summary>
    /// Indicativo del emisor extraído del mensaje.
    /// </summary>
    [ObservableProperty]
    private string _indicativoEmisor = string.Empty;

    /// <summary>
    /// Localizador Maidenhead del emisor.
    /// </summary>
    [ObservableProperty]
    private string _localizador = string.Empty;

    /// <summary>
    /// Indica si el emisor pertenece a un DXCC nuevo (para resaltar en la tabla).
    /// </summary>
    [ObservableProperty]
    private bool _esNuevoDxcc = false;
}
