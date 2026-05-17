using RadioAficionado.Compartido.Contratos;

namespace RadioAficionado.Web.Hubs;

/// <summary>
/// Interfaz que define los metodos que el servidor web puede invocar
/// en el navegador del usuario a traves del hub de relay.
/// </summary>
public interface IClienteHubRelay
{
    /// <summary>
    /// Envia el estado actual del rig al navegador del usuario.
    /// </summary>
    /// <param name="estado">Estado actualizado del rig.</param>
    Task RecibirEstadoRig(EstadoRigRemotoDto estado);

    /// <summary>
    /// Envia la respuesta de un comando al navegador del usuario.
    /// </summary>
    /// <param name="respuesta">Respuesta del comando ejecutado.</param>
    Task RecibirRespuestaComando(RespuestaRemotoRig respuesta);

    /// <summary>
    /// Notifica al navegador si el servicio local esta conectado o desconectado.
    /// </summary>
    /// <param name="conectado">True si el servicio esta conectado, false si se desconecto.</param>
    Task RecibirConexionServicio(bool conectado);

    /// <summary>
    /// Envia una linea de espectro (waterfall) al browser para visualizacion remota.
    /// </summary>
    /// <param name="linea">Linea de espectro comprimida.</param>
    Task RecibirLineaEspectro(LineaEspectroRemotaDto linea);

    /// <summary>
    /// Envia un mensaje decodificado (FT8, CW, etc.) al browser.
    /// </summary>
    /// <param name="mensaje">Mensaje decodificado.</param>
    Task RecibirMensajeDecodificado(MensajeDecodificadoRemotoDto mensaje);

    /// <summary>
    /// Envia señalizacion WebRTC al browser (SDP offer/answer o candidato ICE).
    /// </summary>
    /// <param name="senalizacion">Mensaje de señalizacion.</param>
    Task RecibirSenalizacion(SenalizacionWebRtc senalizacion);
}
