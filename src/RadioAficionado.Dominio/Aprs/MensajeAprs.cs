using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Aprs;

/// <summary>
/// Representa un mensaje APRS dirigido a una estación específica.
/// El formato APRS de mensajes es ":DEST     :Texto del mensaje{NNN".
/// </summary>
/// <param name="Destinatario">Indicativo de la estación destinataria del mensaje.</param>
/// <param name="Texto">Contenido textual del mensaje.</param>
/// <param name="NumeroMensaje">Número de secuencia del mensaje para confirmación (ACK), si está presente.</param>
public record MensajeAprs(
    Indicativo Destinatario,
    string Texto,
    string? NumeroMensaje);
