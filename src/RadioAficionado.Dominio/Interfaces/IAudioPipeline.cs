using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Representa una muestra de audio capturada.
/// </summary>
public sealed class MuestraAudio
{
    /// <summary>
    /// Datos de audio en formato PCM de 16 bits.
    /// </summary>
    public ReadOnlyMemory<short> Datos { get; }

    /// <summary>
    /// Tasa de muestreo en Hz.
    /// </summary>
    public int TasaDeMuestreoHz { get; }

    /// <summary>
    /// Marca de tiempo de la captura.
    /// </summary>
    public DateTimeOffset MarcaDeTiempo { get; }

    /// <summary>
    /// Crea una nueva muestra de audio.
    /// </summary>
    public MuestraAudio(ReadOnlyMemory<short> datos, int tasaDeMuestreoHz, DateTimeOffset marcaDeTiempo)
    {
        Datos = datos;
        TasaDeMuestreoHz = tasaDeMuestreoHz;
        MarcaDeTiempo = marcaDeTiempo;
    }
}

/// <summary>
/// Pipeline de audio interno que elimina la necesidad de cables de audio virtuales.
/// Captura audio del hardware y lo distribuye a múltiples consumidores.
/// </summary>
public interface IAudioPipeline : IAsyncDisposable
{
    /// <summary>
    /// Indica si el pipeline está activo capturando audio.
    /// </summary>
    bool EstaActivo { get; }

    /// <summary>
    /// Tasa de muestreo actual en Hz.
    /// </summary>
    int TasaDeMuestreoHz { get; }

    /// <summary>
    /// Inicia la captura de audio desde el dispositivo especificado.
    /// </summary>
    /// <param name="dispositivoId">Identificador del dispositivo de audio.</param>
    /// <param name="tasaDeMuestreoHz">Tasa de muestreo deseada (default: 12000 Hz para modos digitales).</param>
    /// <param name="ct">Token de cancelación.</param>
    Task IniciarCapturaAsync(string dispositivoId, int tasaDeMuestreoHz = 12000, CancellationToken ct = default);

    /// <summary>
    /// Detiene la captura de audio.
    /// </summary>
    Task DetenerCapturaAsync(CancellationToken ct = default);

    /// <summary>
    /// Suscribe un consumidor para recibir muestras de audio.
    /// </summary>
    /// <param name="consumidor">Función que procesa cada muestra.</param>
    /// <returns>Identificador de suscripción para cancelar.</returns>
    Guid Suscribir(Action<MuestraAudio> consumidor);

    /// <summary>
    /// Cancela una suscripción de consumidor de audio.
    /// </summary>
    void Desuscribir(Guid suscripcionId);

    /// <summary>
    /// Obtiene la lista de dispositivos de audio disponibles.
    /// </summary>
    Task<IReadOnlyList<DispositivoAudio>> ObtenerDispositivosAsync(CancellationToken ct = default);

    /// <summary>
    /// Envía audio al dispositivo de salida (para transmisión).
    /// </summary>
    Task TransmitirAudioAsync(ReadOnlyMemory<short> datos, CancellationToken ct = default);
}

/// <summary>
/// Información sobre un dispositivo de audio disponible.
/// </summary>
public sealed class DispositivoAudio
{
    /// <summary>
    /// Identificador único del dispositivo.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Nombre legible del dispositivo.
    /// </summary>
    public string Nombre { get; }

    /// <summary>
    /// Si es un dispositivo de entrada (micrófono/línea in).
    /// </summary>
    public bool EsEntrada { get; }

    /// <summary>
    /// Si es un dispositivo de salida (altavoz/línea out).
    /// </summary>
    public bool EsSalida { get; }

    /// <summary>
    /// Crea información de dispositivo de audio.
    /// </summary>
    public DispositivoAudio(string id, string nombre, bool esEntrada, bool esSalida)
    {
        Id = id;
        Nombre = nombre;
        EsEntrada = esEntrada;
        EsSalida = esSalida;
    }
}
