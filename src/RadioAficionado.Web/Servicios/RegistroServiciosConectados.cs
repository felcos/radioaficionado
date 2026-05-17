using System.Collections.Concurrent;

namespace RadioAficionado.Web.Servicios;

/// <summary>
/// Registro singleton thread-safe que mapea usuarios a las conexiones SignalR
/// de sus servicios locales (RadioAficionado.Servicio). Permite al servidor web
/// saber que servicios estan conectados y enrutar comandos al servicio correcto.
/// </summary>
public class RegistroServiciosConectados
{
    private readonly ConcurrentDictionary<string, string> _conexiones = new();

    /// <summary>
    /// Registra la conexion de un servicio local para un usuario.
    /// Si el usuario ya tiene un servicio conectado, se reemplaza la conexion anterior.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario propietario del servicio.</param>
    /// <param name="connectionId">Identificador de conexion SignalR del servicio.</param>
    public void Registrar(string usuarioId, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            throw new ArgumentException("El identificador de usuario no puede estar vacio.", nameof(usuarioId));
        }

        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("El identificador de conexion no puede estar vacio.", nameof(connectionId));
        }

        _conexiones.AddOrUpdate(usuarioId, connectionId, (_, _) => connectionId);
    }

    /// <summary>
    /// Elimina el registro de conexion de un servicio local.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario cuyo servicio se desconecto.</param>
    public void Eliminar(string usuarioId)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return;
        }

        _conexiones.TryRemove(usuarioId, out _);
    }

    /// <summary>
    /// Obtiene el identificador de conexion SignalR del servicio local de un usuario.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario.</param>
    /// <returns>ConnectionId del servicio si esta conectado, null en caso contrario.</returns>
    public string? ObtenerConnectionId(string usuarioId)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return null;
        }

        _conexiones.TryGetValue(usuarioId, out string? connectionId);
        return connectionId;
    }

    /// <summary>
    /// Verifica si el servicio local de un usuario esta conectado.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario.</param>
    /// <returns>True si el servicio esta conectado, false en caso contrario.</returns>
    public bool EstaConectado(string usuarioId)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        return _conexiones.ContainsKey(usuarioId);
    }
}
