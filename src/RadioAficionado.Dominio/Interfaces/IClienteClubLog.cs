namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Configuración necesaria para conectarse al servicio Club Log.
/// </summary>
public sealed class ConfiguracionClubLog
{
    /// <summary>Dirección de correo electrónico registrada en Club Log.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña de Club Log.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Indicativo propio registrado en Club Log.</summary>
    public string Indicativo { get; set; } = string.Empty;

    /// <summary>Clave de API proporcionada por Club Log para acceso programático.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>URL base del servicio Club Log.</summary>
    public string UrlBase { get; set; } = "https://clublog.org";
}

/// <summary>
/// Cliente para interactuar con el servicio Club Log.
/// Permite subir QSOs en formato ADIF para análisis y estadísticas DXCC.
/// </summary>
public interface IClienteClubLog
{
    /// <summary>
    /// Sube contenido ADIF al servicio Club Log.
    /// </summary>
    /// <param name="contenidoAdif">Contenido del archivo ADIF a subir.</param>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <param name="password">Contraseña del usuario.</param>
    /// <param name="indicativo">Indicativo propio.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado de la operación de subida.</returns>
    Task<ResultadoSubida> SubirAdifAsync(string contenidoAdif, string email, string password, string indicativo, CancellationToken ct = default);
}
