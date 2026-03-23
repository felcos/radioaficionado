namespace RadioAficionado.Dominio.Aprs;

/// <summary>
/// Configuración para la conexión al servidor APRS-IS (Internet Service).
/// </summary>
/// <param name="Servidor">Dirección del servidor APRS-IS (ej: "rotate.aprs2.net").</param>
/// <param name="Puerto">Puerto TCP del servidor (normalmente 14580).</param>
/// <param name="Indicativo">Indicativo de radioaficionado para autenticarse.</param>
/// <param name="Passcode">Código de verificación APRS calculado a partir del indicativo.</param>
/// <param name="Filtro">Filtro de servidor APRS-IS (ej: "r/40.41/-3.70/200" para radio de 200 km).</param>
public record ConfiguracionAprs(
    string Servidor,
    int Puerto,
    string Indicativo,
    int Passcode,
    string? Filtro);
