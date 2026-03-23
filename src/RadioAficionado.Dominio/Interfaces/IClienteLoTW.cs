namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Configuración necesaria para conectarse al servicio Logbook of the World (LoTW) de la ARRL.
/// </summary>
public sealed class ConfiguracionLoTW
{
    /// <summary>Ruta al ejecutable TQSL para firmar archivos ADIF (opcional si se usa subida directa).</summary>
    public string? RutaTqsl { get; set; }

    /// <summary>Indicativo propio registrado en LoTW.</summary>
    public string IndicativoPropio { get; set; } = string.Empty;

    /// <summary>Indica si hay un certificado TQSL activo y válido.</summary>
    public bool CertificadoActivo { get; set; }

    /// <summary>Nombre de usuario de LoTW (generalmente el indicativo).</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Contraseña de LoTW.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>URL base del servicio LoTW.</summary>
    public string UrlBase { get; set; } = "https://lotw.arrl.org";
}

/// <summary>
/// Cliente para interactuar con el servicio Logbook of the World (LoTW) de la ARRL.
/// Permite subir QSOs en formato ADIF y descargar confirmaciones.
/// </summary>
public interface IClienteLoTW
{
    /// <summary>
    /// Sube contenido ADIF al servicio LoTW.
    /// </summary>
    /// <param name="contenidoAdif">Contenido del archivo ADIF a subir.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado de la operación de subida.</returns>
    Task<ResultadoSubida> SubirAdifAsync(string contenidoAdif, CancellationToken ct = default);

    /// <summary>
    /// Descarga las confirmaciones disponibles en LoTW en formato ADIF.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Contenido ADIF con las confirmaciones descargadas.</returns>
    Task<string> DescargarConfirmacionesAsync(CancellationToken ct = default);
}
