namespace RadioAficionado.Servicio.Dtos;

/// <summary>
/// DTO para registrar un QSO desde el panel de operacion.
/// </summary>
public sealed record RegistroQsoDto(
    string Indicativo,
    long FrecuenciaHz,
    string Modo,
    string RstEnviado,
    string RstRecibido,
    string? Grid,
    string? Nombre,
    string? Comentario);
