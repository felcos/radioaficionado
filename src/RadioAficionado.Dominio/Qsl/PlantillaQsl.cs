namespace RadioAficionado.Dominio.Qsl;

/// <summary>
/// Plantilla visual para la generación de tarjetas QSL digitales.
/// Define los parámetros de diseño como dimensiones, colores y fondo.
/// </summary>
/// <param name="Nombre">Nombre identificativo de la plantilla.</param>
/// <param name="AnchoPixeles">Ancho de la tarjeta en píxeles. Por defecto 800.</param>
/// <param name="AltoPixeles">Alto de la tarjeta en píxeles. Por defecto 500.</param>
/// <param name="ColorFondo">Color de fondo en formato hexadecimal (por ejemplo, "#1A237E").</param>
/// <param name="ColorTexto">Color del texto en formato hexadecimal (por ejemplo, "#FFFFFF").</param>
/// <param name="RutaImagenFondo">Ruta opcional a una imagen de fondo personalizada.</param>
/// <param name="MostrarMapa">Indica si se debe mostrar un mapa con la ubicación del operador.</param>
public record PlantillaQsl(
    string Nombre,
    int AnchoPixeles = 800,
    int AltoPixeles = 500,
    string ColorFondo = "#1A237E",
    string ColorTexto = "#FFFFFF",
    string? RutaImagenFondo = null,
    bool MostrarMapa = false);
