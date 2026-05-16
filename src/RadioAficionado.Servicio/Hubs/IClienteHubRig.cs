using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Hubs;

/// <summary>
/// Interfaz de los metodos que el servidor puede invocar en el cliente para el hub de rig.
/// </summary>
public interface IClienteHubRig
{
    /// <summary>Envia el estado actual del rig al cliente.</summary>
    Task RecibirEstadoRig(EstadoRigDto estado);

    /// <summary>Notifica cambio en el estado de conexion.</summary>
    Task RecibirConexionCambiada(bool conectado, string detalle);
}
