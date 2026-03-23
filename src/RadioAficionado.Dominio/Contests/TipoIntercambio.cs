namespace RadioAficionado.Dominio.Contests;

/// <summary>
/// Define el tipo de intercambio requerido en un contest.
/// El intercambio es la información que cada estación debe enviar y recibir para que el QSO sea válido.
/// </summary>
public enum TipoIntercambio
{
    /// <summary>RST + número de zona CQ (por ejemplo, 59 14).</summary>
    RstZona,

    /// <summary>RST + abreviatura de estado/provincia (por ejemplo, 599 CA).</summary>
    RstEstado,

    /// <summary>RST + número serial secuencial (por ejemplo, 599 001).</summary>
    RstSerial,

    /// <summary>RST + edad del operador (por ejemplo, 59 35). Usado en All Asian.</summary>
    RstEdad,

    /// <summary>RST + número de sección ARRL (por ejemplo, 599 EPA).</summary>
    RstSeccion,

    /// <summary>RST + entidad DXCC o ITU zone (por ejemplo, 599 37).</summary>
    RstZonaItu,

    /// <summary>Número de transmisores + clase ARRL (por ejemplo, 2A). Usado en Field Day.</summary>
    ClaseTransmisores,

    /// <summary>RST + QTH (por ejemplo, 599 TOKYO).</summary>
    RstQth
}
