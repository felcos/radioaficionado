using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Hubs;

/// <summary>
/// Interfaz de los metodos que el servidor puede invocar en el cliente para el hub de estado general.
/// </summary>
public interface IClienteHubEstado
{
    /// <summary>Envia un spot DX al cliente.</summary>
    Task RecibirSpotDx(SpotDxDto spot);

    /// <summary>Envia una notificacion al cliente.</summary>
    Task RecibirNotificacion(string tipo, string mensaje);
}
