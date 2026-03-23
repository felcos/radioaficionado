using RadioAficionado.Dominio.Aprs;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio para la comunicación con la red APRS-IS (Automatic Packet Reporting System - Internet Service).
/// Permite conectar, enviar posiciones y mensajes, y recibir paquetes de otras estaciones.
/// </summary>
public interface IServicioAprs
{
    /// <summary>
    /// Evento disparado cada vez que se recibe un paquete APRS válido del servidor.
    /// </summary>
    event Action<PaqueteAprs> PaqueteRecibido;

    /// <summary>
    /// Indica si el cliente está actualmente conectado al servidor APRS-IS.
    /// </summary>
    bool EstaConectado { get; }

    /// <summary>
    /// Conecta al servidor APRS-IS con la configuración proporcionada.
    /// </summary>
    /// <param name="configuracion">Configuración de conexión (servidor, puerto, indicativo, passcode, filtro).</param>
    /// <param name="tokenCancelacion">Token para cancelar la operación.</param>
    /// <returns>Tarea que representa la operación asíncrona de conexión.</returns>
    Task ConectarAsync(ConfiguracionAprs configuracion, CancellationToken tokenCancelacion = default);

    /// <summary>
    /// Desconecta del servidor APRS-IS y libera los recursos de red.
    /// </summary>
    /// <returns>Tarea que representa la operación asíncrona de desconexión.</returns>
    Task DesconectarAsync();

    /// <summary>
    /// Envía un reporte de posición al servidor APRS-IS.
    /// </summary>
    /// <param name="coordenadas">Coordenadas geográficas a reportar.</param>
    /// <param name="comentario">Comentario libre adjunto a la posición.</param>
    /// <param name="tokenCancelacion">Token para cancelar la operación.</param>
    /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
    Task EnviarPosicionAsync(Coordenadas coordenadas, string comentario, CancellationToken tokenCancelacion = default);

    /// <summary>
    /// Envía un mensaje APRS dirigido a una estación específica.
    /// </summary>
    /// <param name="destinatario">Indicativo de la estación destinataria.</param>
    /// <param name="texto">Texto del mensaje a enviar.</param>
    /// <param name="tokenCancelacion">Token para cancelar la operación.</param>
    /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
    Task EnviarMensajeAsync(Indicativo destinatario, string texto, CancellationToken tokenCancelacion = default);
}
