using System.Text.Json;

namespace RadioAficionado.Lanzador;

/// <summary>
/// Configuracion persistida del lanzador WebView2. Se guarda en JSON
/// junto al ejecutable para recordar posicion, tamano y estado de la ventana.
/// </summary>
public sealed class ConfiguracionLanzador
{
    /// <summary>Ancho de la ventana en pixeles.</summary>
    public int Ancho { get; set; } = 1400;

    /// <summary>Alto de la ventana en pixeles.</summary>
    public int Alto { get; set; } = 900;

    /// <summary>Posicion X de la ventana.</summary>
    public int PosicionX { get; set; } = -1;

    /// <summary>Posicion Y de la ventana.</summary>
    public int PosicionY { get; set; } = -1;

    /// <summary>True si la ventana estaba maximizada al cerrar.</summary>
    public bool Maximizada { get; set; }

    /// <summary>Puerto del servicio.</summary>
    public int Puerto { get; set; } = 5200;

    /// <summary>True para abrir DevTools (debug).</summary>
    public bool DevToolsHabilitados { get; set; }

    private static readonly string RutaArchivo = Path.Combine(
        AppContext.BaseDirectory, "configuracion-lanzador.json");

    /// <summary>
    /// Carga la configuracion desde disco. Retorna valores por defecto si no existe.
    /// </summary>
    public static ConfiguracionLanzador Cargar()
    {
        if (!File.Exists(RutaArchivo))
        {
            return new ConfiguracionLanzador();
        }

        try
        {
            string json = File.ReadAllText(RutaArchivo);
            return JsonSerializer.Deserialize<ConfiguracionLanzador>(json) ?? new ConfiguracionLanzador();
        }
        catch
        {
            return new ConfiguracionLanzador();
        }
    }

    /// <summary>
    /// Guarda la configuracion en disco en formato JSON.
    /// </summary>
    public void Guardar()
    {
        try
        {
            JsonSerializerOptions opciones = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, opciones);
            File.WriteAllText(RutaArchivo, json);
        }
        catch
        {
            // Si falla el guardado, no bloquear la app
        }
    }
}
