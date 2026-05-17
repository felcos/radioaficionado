using RadioAficionado.Compartido.Contratos;

namespace RadioAficionado.Web.Hubs;

/// <summary>
/// Interfaz que define los metodos que el servidor web puede invocar
/// en el servicio local (RadioAficionado.Servicio) a traves del hub de tunel.
/// </summary>
public interface IClienteHubTunel
{
    /// <summary>
    /// Envia un comando al servicio local para que lo ejecute en el rig.
    /// </summary>
    /// <param name="comando">Comando remoto a ejecutar.</param>
    Task EjecutarComandoRig(ComandoRemotoRig comando);

    /// <summary>
    /// Envia un ping al servicio local para verificar que sigue activo.
    /// </summary>
    Task Ping();

    /// <summary>
    /// Envia señalizacion WebRTC al servicio local (SDP offer/answer o candidato ICE).
    /// </summary>
    /// <param name="senalizacion">Mensaje de señalizacion.</param>
    Task RecibirSenalizacion(SenalizacionWebRtc senalizacion);
}
