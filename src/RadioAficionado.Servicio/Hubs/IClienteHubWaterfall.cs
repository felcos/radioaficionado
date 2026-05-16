using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Hubs;

/// <summary>
/// Interfaz de los metodos que el servidor puede invocar en el cliente para el hub de waterfall.
/// </summary>
public interface IClienteHubWaterfall
{
    /// <summary>Envia una linea de espectro al cliente para renderizar en el waterfall.</summary>
    Task RecibirLineaEspectro(LineaEspectroDto linea);
}
