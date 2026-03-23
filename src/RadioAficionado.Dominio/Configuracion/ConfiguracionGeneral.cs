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
}
