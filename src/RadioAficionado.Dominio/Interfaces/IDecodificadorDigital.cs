using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Resultado de decodificación de un modo digital.
/// </summary>
public sealed class MensajeDecodificado
{
    /// <summary>
    /// Marca de tiempo de la decodificación.
    /// </summary>
    public DateTimeOffset MarcaDeTiempo { get; }

    /// <summary>
    /// Frecuencia de audio en Hz dentro de la banda pasante donde se decodificó.
    /// </summary>
    public int FrecuenciaAudioHz { get; }

    /// <summary>
    /// Relación señal/ruido en dB.
    /// </summary>
    public int Snr { get; }

    /// <summary>
    /// Delta de tiempo en segundos (para modos como FT8).
    /// </summary>
    public double DeltaTiempo { get; }

    /// <summary>
    /// Modo digital que generó este mensaje.
    /// </summary>
    public ModoOperacion Modo { get; }

    /// <summary>
    /// Submodo si aplica.
    /// </summary>
    public SubModoOperacion? SubModo { get; }

    /// <summary>
    /// Texto del mensaje decodificado.
    /// </summary>
    public string Texto { get; }

    /// <summary>
    /// Indicativo del emisor (si se pudo extraer del mensaje).
    /// </summary>
    public string? IndicativoEmisor { get; }

    /// <summary>
    /// Indicativo del destinatario (si se pudo extraer).
    /// </summary>
    public string? IndicativoDestinatario { get; }

    /// <summary>
    /// Localizador extraído del mensaje (si existe).
    /// </summary>
    public string? Localizador { get; }

    /// <summary>
    /// Reporte de señal extraído del mensaje (si existe).
    /// </summary>
    public string? ReporteSenal { get; }

    /// <summary>
    /// Crea un nuevo mensaje decodificado.
    /// </summary>
    public MensajeDecodificado(
        DateTimeOffset marcaDeTiempo,
        int frecuenciaAudioHz,
        int snr,
        double deltaTiempo,
        ModoOperacion modo,
        string texto,
        SubModoOperacion? subModo = null,
        string? indicativoEmisor = null,
        string? indicativoDestinatario = null,
        string? localizador = null,
        string? reporteSenal = null)
    {
        MarcaDeTiempo = marcaDeTiempo;
        FrecuenciaAudioHz = frecuenciaAudioHz;
        Snr = snr;
        DeltaTiempo = deltaTiempo;
        Modo = modo;
        SubModo = subModo;
        Texto = texto;
        IndicativoEmisor = indicativoEmisor;
        IndicativoDestinatario = indicativoDestinatario;
        Localizador = localizador;
        ReporteSenal = reporteSenal;
    }
}

/// <summary>
/// Interfaz que deben implementar todos los decodificadores de modos digitales.
/// Permite añadir nuevos modos sin modificar el código existente.
/// </summary>
public interface IDecodificadorDigital : IDisposable
{
    /// <summary>
    /// Modo de operación que este decodificador maneja.
    /// </summary>
    ModoOperacion Modo { get; }

    /// <summary>
    /// Submodos soportados por este decodificador.
    /// </summary>
    IReadOnlyList<SubModoOperacion> SubModosSoportados { get; }

    /// <summary>
    /// Indica si el decodificador está activo procesando audio.
    /// </summary>
    bool EstaActivo { get; }

    /// <summary>
    /// Tasa de muestreo requerida por este decodificador en Hz.
    /// </summary>
    int TasaDeMuestreoRequeridaHz { get; }

    /// <summary>
    /// Inicia el decodificador.
    /// </summary>
    Task IniciarAsync(CancellationToken ct = default);

    /// <summary>
    /// Detiene el decodificador.
    /// </summary>
    Task DetenerAsync(CancellationToken ct = default);

    /// <summary>
    /// Procesa un bloque de muestras de audio y devuelve los mensajes decodificados.
    /// </summary>
    /// <param name="muestra">Muestra de audio a procesar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de mensajes decodificados (puede estar vacía).</returns>
    Task<IReadOnlyList<MensajeDecodificado>> ProcesarAudioAsync(MuestraAudio muestra, CancellationToken ct = default);

    /// <summary>
    /// Evento que se dispara cuando se decodifica un nuevo mensaje.
    /// Útil para notificación en tiempo real sin polling.
    /// </summary>
    event EventHandler<MensajeDecodificado>? MensajeDecodificadoRecibido;
}

/// <summary>
/// Registro central de decodificadores digitales disponibles.
/// Permite descubrir y activar decodificadores dinámicamente.
/// </summary>
public interface IRegistroDecodificadores
{
    /// <summary>
    /// Obtiene todos los decodificadores registrados.
    /// </summary>
    IReadOnlyList<IDecodificadorDigital> ObtenerTodos();

    /// <summary>
    /// Obtiene un decodificador por modo de operación.
    /// </summary>
    IDecodificadorDigital? ObtenerPorModo(ModoOperacion modo);

    /// <summary>
    /// Registra un nuevo decodificador.
    /// </summary>
    void Registrar(IDecodificadorDigital decodificador);

    /// <summary>
    /// Obtiene los modos actualmente disponibles.
    /// </summary>
    IReadOnlyList<ModoOperacion> ObtenerModosDisponibles();
}
