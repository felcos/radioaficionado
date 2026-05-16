namespace RadioAficionado.Nativo.Rig;

/// <summary>
/// Modelos de radio soportados para control CAT directo por puerto serie.
/// </summary>
public enum ModeloRadio
{
    /// <summary>Detección automática del protocolo CAT.</summary>
    Automatico,

    // --- Yaesu ---
    /// <summary>Yaesu FT-991.</summary>
    YaesuFt991,
    /// <summary>Yaesu FT-991A.</summary>
    YaesuFt991A,
    /// <summary>Yaesu FT-891.</summary>
    YaesuFt891,
    /// <summary>Yaesu FT-710.</summary>
    YaesuFt710,
    /// <summary>Yaesu FTDX-10.</summary>
    YaesuFtdx10,
    /// <summary>Yaesu FTDX-101.</summary>
    YaesuFtdx101,

    // --- Icom ---
    /// <summary>Icom IC-7300.</summary>
    IcomIc7300,
    /// <summary>Icom IC-7100.</summary>
    IcomIc7100,
    /// <summary>Icom IC-705.</summary>
    IcomIc705,
    /// <summary>Icom IC-9700.</summary>
    IcomIc9700,

    // --- Kenwood / Elecraft ---
    /// <summary>Kenwood TS-890.</summary>
    KenwoodTs890,
    /// <summary>Kenwood TS-590.</summary>
    KenwoodTs590,
    /// <summary>Elecraft K3.</summary>
    ElecraftK3,
    /// <summary>Elecraft KX3.</summary>
    ElecraftKx3
}
