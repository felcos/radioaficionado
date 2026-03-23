using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Aprs;

/// <summary>
/// Representa la información de posición extraída de un paquete APRS.
/// Incluye coordenadas, símbolo, y opcionalmente velocidad, rumbo y altitud.
/// </summary>
/// <param name="Coordenadas">Coordenadas geográficas de la estación.</param>
/// <param name="Simbolo">Carácter del símbolo APRS (ej: '-' para casa, '>' para coche).</param>
/// <param name="Tabla">Tabla de símbolos APRS ('/' para primaria, '\' para secundaria).</param>
/// <param name="Velocidad">Velocidad en nudos, si está disponible.</param>
/// <param name="Rumbo">Rumbo en grados (0-360), si está disponible.</param>
/// <param name="Altitud">Altitud en metros, si está disponible.</param>
/// <param name="Comentario">Comentario libre adjunto a la posición.</param>
public record PosicionAprs(
    Coordenadas Coordenadas,
    char Simbolo,
    char Tabla,
    double? Velocidad,
    double? Rumbo,
    double? Altitud,
    string? Comentario);
