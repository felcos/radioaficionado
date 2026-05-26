using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

namespace RadioAficionado.Servicio.Remoto;

/// <summary>
/// Adaptador WebRTC de audio basado en SIPSorcery.
/// Gestiona la conexion peer-to-peer para transmitir audio del radio al browser.
/// </summary>
public sealed class AdaptadorWebRtcAudio : IDisposable
{
    private readonly ILogger _logger;
    private RTCPeerConnection? _conexionPeer;
    private bool _disposed;
    private bool _inicializado;

    /// <summary>
    /// ID de formato RTP para PCMU (G.711 mu-law) segun RFC 3551.
    /// </summary>
    private const int PCMU_FORMAT_ID = 0;

    /// <summary>
    /// Formato de audio PCM usado para la transmision (mulaw 8kHz mono para compatibilidad WebRTC).
    /// </summary>
    private static readonly AudioFormat _formatoAudioMulaw = new(
        AudioCodecsEnum.PCMU,
        PCMU_FORMAT_ID,
        8000,
        1);

    /// <summary>
    /// Se dispara cuando se genera una respuesta SDP que debe enviarse al browser.
    /// </summary>
    public event EventHandler<string>? RespuestaSdpGenerada;

    /// <summary>
    /// Se dispara cuando se genera un candidato ICE local que debe enviarse al browser.
    /// </summary>
    public event EventHandler<CandidatoIceLocal>? CandidatoIceGenerado;

    /// <summary>
    /// Indica si el adaptador WebRTC esta disponible para uso.
    /// </summary>
    public bool Disponible => _inicializado;

    /// <summary>
    /// Indica si hay una conexion WebRTC activa.
    /// </summary>
    public bool Conectado => _conexionPeer?.connectionState == RTCPeerConnectionState.connected;

    /// <summary>
    /// Crea una nueva instancia del adaptador WebRTC de audio.
    /// </summary>
    /// <param name="logger">Logger para registrar eventos del adaptador.</param>
    public AdaptadorWebRtcAudio(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Inicializar();
    }

    /// <summary>
    /// Procesa una oferta SDP recibida del browser a traves del relay.
    /// Crea la conexion peer, configura el audio track y genera la respuesta SDP.
    /// </summary>
    /// <param name="sdp">Contenido de la oferta SDP.</param>
    /// <returns>Respuesta SDP si se pudo procesar; null si hubo error.</returns>
    public async Task<string?> ProcesarOfertaAsync(string sdp)
    {
        ArgumentNullException.ThrowIfNull(sdp);

        if (!_inicializado)
        {
            _logger.LogWarning("AdaptadorWebRtcAudio no inicializado. No se puede procesar la oferta SDP");
            return null;
        }

        try
        {
            _logger.LogInformation("Procesando oferta SDP ({Longitud} caracteres)", sdp.Length);

            // Liberar conexion anterior si existe
            LiberarConexionPeer();

            RTCConfiguration configuracion = new()
            {
                iceServers =
                [
                    new RTCIceServer { urls = "stun:stun.l.google.com:19302" },
                    new RTCIceServer { urls = "stun:stun1.l.google.com:19302" }
                ]
            };

            _conexionPeer = new RTCPeerConnection(configuracion);

            // Crear track de audio con formato PCMU (G.711 mu-law, ampliamente soportado)
            MediaStreamTrack pistaAudio = new(
                SDPMediaTypesEnum.audio,
                false,
                [new SDPAudioVideoMediaFormat(_formatoAudioMulaw)],
                MediaStreamStatusEnum.SendOnly);

            _conexionPeer.addTrack(pistaAudio);

            // Configurar callbacks
            ConfigurarCallbacksConexion();

            // Establecer descripcion remota (oferta del browser)
            RTCSessionDescriptionInit ofertaInit = new()
            {
                type = RTCSdpType.offer,
                sdp = sdp
            };

            SetDescriptionResultEnum resultadoOferta = _conexionPeer.setRemoteDescription(ofertaInit);
            if (resultadoOferta != SetDescriptionResultEnum.OK)
            {
                _logger.LogError(
                    "Error al establecer descripcion remota: {Resultado}",
                    resultadoOferta);
                LiberarConexionPeer();
                return null;
            }

            // Crear respuesta SDP
            RTCSessionDescriptionInit respuesta = _conexionPeer.createAnswer();
            await Task.Run(() => _conexionPeer.setLocalDescription(respuesta));

            _logger.LogInformation(
                "Respuesta SDP generada ({Longitud} caracteres). Estado: {Estado}",
                respuesta.sdp.Length,
                _conexionPeer.connectionState);

            // Notificar la respuesta SDP
            RespuestaSdpGenerada?.Invoke(this, respuesta.sdp);

            return respuesta.sdp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar oferta SDP");
            LiberarConexionPeer();
            return null;
        }
    }

    /// <summary>
    /// Procesa un candidato ICE recibido del browser a traves del relay.
    /// </summary>
    /// <param name="candidato">Cadena del candidato ICE.</param>
    /// <param name="sdpMid">Identificador de linea de medios SDP.</param>
    /// <param name="indiceLinea">Indice de linea SDP.</param>
    public Task ProcesarCandidatoIceAsync(string candidato, string? sdpMid, int? indiceLinea)
    {
        ArgumentNullException.ThrowIfNull(candidato);

        if (_conexionPeer is null)
        {
            _logger.LogWarning(
                "Candidato ICE recibido pero no hay conexion peer activa. Ignorando");
            return Task.CompletedTask;
        }

        try
        {
            RTCIceCandidateInit candidatoInit = new()
            {
                candidate = candidato,
                sdpMid = sdpMid ?? "0",
                sdpMLineIndex = (ushort)(indiceLinea ?? 0)
            };

            _conexionPeer.addIceCandidate(candidatoInit);

            _logger.LogDebug(
                "Candidato ICE agregado (sdpMid={SdpMid}, indice={Indice})",
                sdpMid ?? "0",
                indiceLinea ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar candidato ICE");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Envia muestras de audio PCM al peer remoto a traves de la conexion WebRTC.
    /// Las muestras se codifican a mu-law (G.711) antes de enviar.
    /// </summary>
    /// <param name="muestras">Muestras PCM de 16 bits.</param>
    /// <param name="tasaMuestreo">Tasa de muestreo de las muestras de entrada (Hz).</param>
    public void EnviarAudioPcm(short[] muestras, int tasaMuestreo)
    {
        if (_conexionPeer is null || _conexionPeer.connectionState != RTCPeerConnectionState.connected)
        {
            return;
        }

        try
        {
            // Remuestrear a 8kHz si es necesario
            short[] muestras8k = tasaMuestreo == 8000
                ? muestras
                : RemuestrearA8kHz(muestras, tasaMuestreo);

            // Codificar PCM16 a mu-law (G.711)
            byte[] datosMulaw = new byte[muestras8k.Length];
            for (int i = 0; i < muestras8k.Length; i++)
            {
                datosMulaw[i] = CodificarMulaw(muestras8k[i]);
            }

            // Enviar al track de audio
            // Duracion en milisegundos = muestras / tasaMuestreo * 1000
            uint duracionMs = (uint)(muestras8k.Length * 1000 / 8000);
            int tamanoMarco = muestras8k.Length;

            _conexionPeer.SendAudio(duracionMs, datosMulaw);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error al enviar audio PCM por WebRTC");
        }
    }

    /// <summary>
    /// Detiene la conexion WebRTC activa y libera los recursos asociados.
    /// </summary>
    public Task DetenerAsync()
    {
        _logger.LogInformation("Deteniendo conexion WebRTC");
        LiberarConexionPeer();
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

        LiberarConexionPeer();
        _logger.LogDebug("AdaptadorWebRtcAudio liberado");
        _disposed = true;
    }

    /// <summary>
    /// Inicializa el adaptador y marca como disponible.
    /// </summary>
    private void Inicializar()
    {
        try
        {
            _inicializado = true;
            _logger.LogInformation(
                "AdaptadorWebRtcAudio inicializado con SIPSorcery. WebRTC disponible");
        }
        catch (Exception ex)
        {
            _inicializado = false;
            _logger.LogError(ex, "Error al inicializar AdaptadorWebRtcAudio. WebRTC no disponible");
        }
    }

    /// <summary>
    /// Configura los callbacks de la conexion peer para logging y eventos ICE.
    /// </summary>
    private void ConfigurarCallbacksConexion()
    {
        if (_conexionPeer is null)
        {
            return;
        }

        _conexionPeer.onconnectionstatechange += (RTCPeerConnectionState estado) =>
        {
            _logger.LogInformation("Estado de conexion WebRTC: {Estado}", estado);

            if (estado == RTCPeerConnectionState.failed ||
                estado == RTCPeerConnectionState.disconnected)
            {
                _logger.LogWarning("Conexion WebRTC perdida (estado: {Estado})", estado);
            }
        };

        _conexionPeer.onicecandidate += (RTCIceCandidate candidato) =>
        {
            _logger.LogDebug(
                "Candidato ICE local generado: {Candidato} (sdpMid={SdpMid})",
                candidato.candidate,
                candidato.sdpMid);

            CandidatoIceLocal datos = new(
                candidato.candidate,
                candidato.sdpMid,
                candidato.sdpMLineIndex);

            CandidatoIceGenerado?.Invoke(this, datos);
        };

        _conexionPeer.onicecandidateerror += (RTCIceCandidate candidato, string error) =>
        {
            _logger.LogDebug("Error en candidato ICE: {Error}", error);
        };

        _conexionPeer.oniceconnectionstatechange += (RTCIceConnectionState estado) =>
        {
            _logger.LogDebug("Estado ICE: {Estado}", estado);
        };
    }

    /// <summary>
    /// Libera la conexion peer actual si existe.
    /// </summary>
    private void LiberarConexionPeer()
    {
        if (_conexionPeer is not null)
        {
            try
            {
                _conexionPeer.close();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error al cerrar conexion peer");
            }

            _conexionPeer.Dispose();
            _conexionPeer = null;
        }
    }

    /// <summary>
    /// Remuestrea muestras PCM de una tasa arbitraria a 8kHz mediante interpolacion lineal.
    /// </summary>
    /// <param name="muestras">Muestras de entrada.</param>
    /// <param name="tasaOrigen">Tasa de muestreo de origen (Hz).</param>
    /// <returns>Muestras remuestreadas a 8kHz.</returns>
    private static short[] RemuestrearA8kHz(short[] muestras, int tasaOrigen)
    {
        double proporcion = (double)tasaOrigen / 8000.0;
        int longitudSalida = (int)(muestras.Length / proporcion);
        short[] salida = new short[longitudSalida];

        for (int i = 0; i < longitudSalida; i++)
        {
            double posicion = i * proporcion;
            int indice = (int)posicion;
            double fraccion = posicion - indice;

            if (indice + 1 < muestras.Length)
            {
                salida[i] = (short)(muestras[indice] * (1.0 - fraccion) + muestras[indice + 1] * fraccion);
            }
            else if (indice < muestras.Length)
            {
                salida[i] = muestras[indice];
            }
        }

        return salida;
    }

    /// <summary>
    /// Codifica una muestra PCM de 16 bits a mu-law (G.711).
    /// Implementacion segun ITU-T G.711.
    /// </summary>
    /// <param name="muestra">Muestra PCM de 16 bits con signo.</param>
    /// <returns>Byte codificado en mu-law.</returns>
    private static byte CodificarMulaw(short muestra)
    {
        const int MULAW_BIAS = 33;
        const int CLIP = 8159;

        int signo = (muestra >> 8) & 0x80;
        int magnitud = muestra < 0 ? -muestra : muestra;

        if (magnitud > CLIP)
        {
            magnitud = CLIP;
        }

        magnitud += MULAW_BIAS;

        int exponente = 7;
        int mascara = 0x4000;
        while ((magnitud & mascara) == 0 && exponente > 0)
        {
            exponente--;
            mascara >>= 1;
        }

        int mantisa = (magnitud >> (exponente + 3)) & 0x0F;
        byte byteMulaw = (byte)(~(signo | (exponente << 4) | mantisa));

        return byteMulaw;
    }
}

/// <summary>
/// Datos de un candidato ICE local generado por la conexion peer.
/// </summary>
/// <param name="Candidato">Cadena del candidato ICE.</param>
/// <param name="SdpMid">Identificador de linea de medios SDP.</param>
/// <param name="IndiceLinea">Indice de linea SDP.</param>
public sealed record CandidatoIceLocal(
    string Candidato,
    string SdpMid,
    ushort IndiceLinea);
