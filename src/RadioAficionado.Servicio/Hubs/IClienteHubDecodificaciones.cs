using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Hubs;

/// <summary>
/// Interfaz de los metodos que el servidor puede invocar en el cliente para el hub de decodificaciones.
/// </summary>
public interface IClienteHubDecodificaciones
{
    /// <summary>Envia un mensaje decodificado al cliente.</summary>
    Task RecibirMensaje(MensajeDecodificadoDto mensaje);
}
