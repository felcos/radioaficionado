namespace RadioAficionado.Dominio.Aprs;

/// <summary>
/// Tipos de paquetes APRS (Automatic Packet Reporting System).
/// </summary>
public enum TipoPaqueteAprs
{
    /// <summary>
    /// Paquete de reporte de posición.
    /// </summary>
    Posicion,

    /// <summary>
    /// Mensaje de texto entre estaciones.
    /// </summary>
    Mensaje,

    /// <summary>
    /// Objeto posicionado en el mapa.
    /// </summary>
    Objeto,

    /// <summary>
    /// Información de estación.
    /// </summary>
    Estacion,

    /// <summary>
    /// Datos de telemetría.
    /// </summary>
    Telemetria,

    /// <summary>
    /// Paquete de estado.
    /// </summary>
    Estado,

    /// <summary>
    /// Consulta a otras estaciones.
    /// </summary>
    Consulta
}
