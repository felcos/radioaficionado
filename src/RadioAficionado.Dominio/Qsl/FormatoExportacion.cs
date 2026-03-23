namespace RadioAficionado.Dominio.Qsl;

/// <summary>
/// Formatos de exportación soportados para las tarjetas QSL digitales.
/// </summary>
public enum FormatoExportacion
{
    /// <summary>Formato PNG (Portable Network Graphics) — sin pérdida.</summary>
    Png,

    /// <summary>Formato JPG (JPEG) — con compresión con pérdida.</summary>
    Jpg,

    /// <summary>Formato PDF (Portable Document Format).</summary>
    Pdf,

    /// <summary>Formato SVG (Scalable Vector Graphics).</summary>
    Svg
}
