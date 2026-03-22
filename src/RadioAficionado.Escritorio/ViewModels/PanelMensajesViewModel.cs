using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para la tabla de mensajes digitales decodificados.
/// </summary>
public partial class PanelMensajesViewModel : ViewModelBase
{
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
