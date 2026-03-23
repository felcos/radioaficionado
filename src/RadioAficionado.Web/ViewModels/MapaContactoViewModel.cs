namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para representar un QSO como marcador en el mapa de contactos.
/// Contiene las coordenadas geográficas y datos básicos del contacto.
/// </summary>
public class MapaContactoViewModel
{
    /// <summary>
    /// Latitud del contacto en grados decimales.
    /// </summary>
    public double Latitud { get; set; }

    /// <summary>
    /// Longitud del contacto en grados decimales.
    /// </summary>
    public double Longitud { get; set; }

    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    public string Indicativo { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora del contacto formateada como cadena.
    /// </summary>
    public string Fecha { get; set; } = string.Empty;

    /// <summary>
    /// Banda de radio utilizada (ej: "20 metros").
    /// </summary>
    public string? Banda { get; set; }

    /// <summary>
    /// Modo de operación utilizado (ej: "FT8", "SSB").
    /// </summary>
    public string Modo { get; set; } = string.Empty;

    /// <summary>
    /// Localizador Maidenhead del contacto.
    /// </summary>
    public string Localizador { get; set; } = string.Empty;
}
