namespace RadioAficionado.Dominio.Configuracion;

/// <summary>
/// Configuración general de la aplicación: rutas, idioma y comportamiento.
/// </summary>
public sealed class ConfiguracionGeneral
{
    /// <summary>Ruta de la base de datos local.</summary>
    public string RutaBaseDatos { get; set; } = string.Empty;

    /// <summary>Código de idioma de la interfaz (ej: "es", "en").</summary>
    public string IdiomaInterfaz { get; set; } = "es";

    /// <summary>Si es true, la aplicación se inicia minimizada en la bandeja del sistema.</summary>
    public bool IniciarMinimizado { get; set; }

    /// <summary>Si es true, se muestran notificaciones de escritorio para eventos importantes.</summary>
    public bool MostrarNotificaciones { get; set; } = true;

    /// <summary>
    /// Formato de fecha para la interfaz. Opciones: "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd".
    /// Por defecto usa formato europeo (dd/MM/yyyy).
    /// </summary>
    public string FormatoFecha { get; set; } = "dd/MM/yyyy";

    /// <summary>
    /// Si es true, se realizan backups automaticos de la configuracion y base de datos local.
    /// </summary>
    public bool BackupAutomatico { get; set; } = true;

    /// <summary>
    /// Numero maximo de backups a retener. Los mas antiguos se eliminan automaticamente.
    /// </summary>
    public int MaxBackups { get; set; } = 10;
}
