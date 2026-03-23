using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Aprs;

/// <summary>
/// Representa un paquete APRS recibido o enviado a través de APRS-IS.
/// Contiene la información de cabecera común a todos los tipos de paquete.
/// </summary>
/// <param name="Origen">Indicativo de la estación que origina el paquete.</param>
/// <param name="Destino">Dirección de destino del paquete (normalmente un tocall como APRS, APxxxx).</param>
/// <param name="Ruta">Ruta de digipeaters por la que ha pasado el paquete.</param>
/// <param name="TipoPaquete">Tipo de paquete APRS identificado.</param>
/// <param name="Contenido">Contenido crudo (raw) del campo de información del paquete.</param>
/// <param name="Timestamp">Marca de tiempo en la que se recibió o generó el paquete.</param>
public record PaqueteAprs(
    Indicativo Origen,
    string Destino,
    IReadOnlyList<string> Ruta,
    TipoPaqueteAprs TipoPaquete,
    string Contenido,
    DateTime Timestamp);
