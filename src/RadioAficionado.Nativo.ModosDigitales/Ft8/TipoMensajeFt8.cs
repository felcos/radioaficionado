namespace RadioAficionado.Nativo.ModosDigitales.Ft8;

/// <summary>
/// Tipos de mensaje en el protocolo FT8/FT4.
/// Cada tipo corresponde a una fase del QSO estándar.
/// </summary>
public enum TipoMensajeFt8
{
    /// <summary>Llamada general (CQ). Ejemplo: "CQ EA4K IN80".</summary>
    CQ,

    /// <summary>Respuesta a un CQ. Ejemplo: "W1AW EA4K -09".</summary>
    Respuesta,

    /// <summary>Reporte de señal con confirmación. Ejemplo: "EA4K W1AW R-12".</summary>
    Reporte,

    /// <summary>Confirmación de recepción del reporte. Ejemplo: "W1AW EA4K RRR".</summary>
    RRR,

    /// <summary>Despedida (73). Ejemplo: "EA4K W1AW 73".</summary>
    Setenta73,

    /// <summary>Mensaje de texto libre que no sigue el formato estándar.</summary>
    Libre
}
