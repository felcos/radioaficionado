using Microsoft.Extensions.Logging;

namespace RadioAficionado.Servicio.Remoto;

/// <summary>
/// Adaptador stub para la conexion WebRTC de audio en el lado del servicio local.
/// Encapsula la logica de senalizacion y conexion WebRTC.
/// Actualmente es un stub que registra operaciones; la implementacion real
/// requiere SIPSorcery u otra libreria WebRTC nativa.
/// </summary>
public sealed class AdaptadorWebRtcAudio : IDisposable
{
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Indica si el adaptador WebRTC esta disponible para uso.
    /// Actualmente siempre devuelve false (stub).
    /// </summary>
    public bool Disponible => false;

    /// <summary>
    /// Indica si hay una conexion WebRTC activa.
    /// Actualmente siempre devuelve false (stub).
    /// </summary>
    public bool Conectado => false;

    /// <summary>
    /// Crea una nueva instancia del adaptador WebRTC de audio.
    /// </summary>
    /// <param name="logger">Logger para registrar eventos del adaptador.</param>
    public AdaptadorWebRtcAudio(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogWarning(
            "AdaptadorWebRtcAudio inicializado como STUB. " +
            "La funcionalidad WebRTC real no esta disponible aun. " +
            "Se requiere integrar SIPSorcery para conexiones P2P de audio.");
    }

    /// <summary>
    /// Procesa una oferta SDP recibida del browser a traves del relay.
    /// En la implementacion real, crearia una respuesta SDP y estableceria la conexion.
    /// </summary>
    /// <param name="sdp">Contenido de la oferta SDP.</param>
    /// <returns>Respuesta SDP si se pudo procesar; null si WebRTC no esta disponible.</returns>
    public Task<string?> ProcesarOfertaAsync(string sdp)
    {
        ArgumentNullException.ThrowIfNull(sdp);

        _logger.LogInformation(
            "Oferta SDP recibida ({Longitud} caracteres). " +
            "WebRTC no disponible — ignorando oferta.",
            sdp.Length);

        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Procesa un candidato ICE recibido del browser a traves del relay.
    /// En la implementacion real, agregaria el candidato a la conexion activa.
    /// </summary>
    /// <param name="candidato">Cadena del candidato ICE.</param>
    /// <param name="sdpMid">Identificador de linea de medios SDP.</param>
    /// <param name="indiceLinea">Indice de linea SDP.</param>
    public Task ProcesarCandidatoIceAsync(string candidato, string? sdpMid, int? indiceLinea)
    {
        ArgumentNullException.ThrowIfNull(candidato);

        _logger.LogDebug(
            "Candidato ICE recibido (sdpMid={SdpMid}, indice={Indice}). " +
            "WebRTC no disponible — ignorando candidato.",
            sdpMid ?? "null",
            indiceLinea?.ToString() ?? "null");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Detiene la conexion WebRTC activa y libera los recursos asociados.
    /// En la implementacion real, cerraria la conexion P2P y detendria el audio.
    /// </summary>
    public Task DetenerAsync()
    {
        _logger.LogInformation("DetenerAsync invocado. WebRTC no disponible — nada que detener.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Libera los recursos del adaptador.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("AdaptadorWebRtcAudio liberado.");
        _disposed = true;
    }
}
