namespace RadioAficionado.Servicio.Remoto;

/// <summary>
/// Configuracion para la conexion remota al servidor web via SignalR.
/// Se lee de la seccion "Remoto" de appsettings.json.
/// </summary>
public sealed class ConfiguracionRemoto
{
    /// <summary>URL base del servidor web (ej: "https://radioaficionado.com").</summary>
    public string UrlServidor { get; set; } = string.Empty;

    /// <summary>Clave API para autenticacion con el servidor.</summary>
    public string ClaveApi { get; set; } = string.Empty;

    /// <summary>Indica si la conexion remota esta habilitada.</summary>
    public bool Habilitado { get; set; }
}
