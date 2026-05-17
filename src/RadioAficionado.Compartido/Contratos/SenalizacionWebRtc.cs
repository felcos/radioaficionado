namespace RadioAficionado.Compartido.Contratos;

/// <summary>
/// Tipos de mensaje de senalizacion WebRTC.
/// </summary>
public enum TipoSenalizacion
{
    /// <summary>Oferta SDP (offer) del iniciador de la conexion.</summary>
    Oferta = 0,

    /// <summary>Respuesta SDP (answer) del receptor de la conexion.</summary>
    Respuesta = 1,

    /// <summary>Candidato ICE para establecer conectividad.</summary>
    CandidatoIce = 2
}

/// <summary>
/// Mensaje de senalizacion WebRTC que se transmite entre browser y servicio
/// a traves del servidor web como relay.
/// </summary>
/// <param name="Tipo">Tipo de mensaje de senalizacion.</param>
/// <param name="Sdp">Contenido SDP (offer o answer). Nulo para candidatos ICE.</param>
/// <param name="CandidatoIce">Cadena del candidato ICE. Nulo para offer/answer.</param>
/// <param name="SdpMid">Identificador de linea de medios SDP asociado al candidato ICE.</param>
/// <param name="IndiceLineaSdp">Indice de linea SDP asociado al candidato ICE.</param>
public sealed record SenalizacionWebRtc(
    TipoSenalizacion Tipo,
    string? Sdp,
    string? CandidatoIce,
    string? SdpMid,
    int? IndiceLineaSdp);
