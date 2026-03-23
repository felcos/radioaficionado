namespace RadioAficionado.Dominio.Dxcc;

/// <summary>
/// Representa la confirmación de un QSO para propósitos de premios DXCC.
/// </summary>
/// <param name="QsoId">Identificador del QSO confirmado.</param>
/// <param name="TipoConfirmacion">Tipo de confirmación (LoTW, QSL, eQSL, etc.).</param>
/// <param name="FechaConfirmacion">Fecha en que se recibió la confirmación.</param>
public record ConfirmacionQso(
    Guid QsoId,
    TipoConfirmacion TipoConfirmacion,
    DateTimeOffset FechaConfirmacion);

/// <summary>
/// Tipos de confirmación de QSO aceptados para premios DXCC.
/// </summary>
public enum TipoConfirmacion
{
    /// <summary>Logbook of The World (ARRL).</summary>
    LoTW,

    /// <summary>Tarjeta QSL física verificada.</summary>
    QslFisica,

    /// <summary>eQSL.cc con verificación AG (Authenticity Guaranteed).</summary>
    EQsl,

    /// <summary>Confirmación directa por tarjeta QSL.</summary>
    QslDirecta,

    /// <summary>Confirmación vía bureau QSL.</summary>
    QslBureau
}
