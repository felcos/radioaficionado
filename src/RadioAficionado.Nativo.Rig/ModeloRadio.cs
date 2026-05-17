namespace RadioAficionado.Nativo.Rig;

/// <summary>
/// Modelos de radio soportados para control CAT directo por puerto serie.
/// </summary>
public enum ModeloRadio
{
    /// <summary>Detección automática del protocolo CAT.</summary>
    Automatico,

    // =========================================================================
    // Yaesu — protocolo texto con terminador ';'
    // =========================================================================

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
    /// <summary>Yaesu FT-450.</summary>
    YaesuFt450,
    /// <summary>Yaesu FT-450D.</summary>
    YaesuFt450D,
    /// <summary>Yaesu FT-950.</summary>
    YaesuFt950,
    /// <summary>Yaesu FT-1200.</summary>
    YaesuFt1200,
    /// <summary>Yaesu FT-2000.</summary>
    YaesuFt2000,
    /// <summary>Yaesu FT-3000.</summary>
    YaesuFt3000,
    /// <summary>Yaesu FT-5000.</summary>
    YaesuFt5000,
    /// <summary>Yaesu FT-DX101MP.</summary>
    YaesuFtdx101Mp,
    /// <summary>Yaesu FTDX-3000.</summary>
    YaesuFtdx3000,
    /// <summary>Yaesu FTDX-5000.</summary>
    YaesuFtdx5000,

    // =========================================================================
    // Icom — protocolo CI-V binario (cada modelo tiene dirección única)
    // =========================================================================

    /// <summary>Icom IC-7300.</summary>
    IcomIc7300,
    /// <summary>Icom IC-7100.</summary>
    IcomIc7100,
    /// <summary>Icom IC-705.</summary>
    IcomIc705,
    /// <summary>Icom IC-9700.</summary>
    IcomIc9700,
    /// <summary>Icom IC-7610.</summary>
    IcomIc7610,
    /// <summary>Icom IC-7851.</summary>
    IcomIc7851,
    /// <summary>Icom IC-7400.</summary>
    IcomIc7400,
    /// <summary>Icom IC-746.</summary>
    IcomIc746,
    /// <summary>Icom IC-746PRO.</summary>
    IcomIc746Pro,
    /// <summary>Icom IC-756PRO3.</summary>
    IcomIc756Pro3,
    /// <summary>Icom IC-718.</summary>
    IcomIc718,
    /// <summary>Icom IC-R8600.</summary>
    IcomIcR8600,

    // =========================================================================
    // Kenwood — protocolo texto estilo Kenwood
    // =========================================================================

    /// <summary>Kenwood TS-890.</summary>
    KenwoodTs890,
    /// <summary>Kenwood TS-590.</summary>
    KenwoodTs590,
    /// <summary>Kenwood TS-990.</summary>
    KenwoodTs990,
    /// <summary>Kenwood TS-480.</summary>
    KenwoodTs480,
    /// <summary>Kenwood TS-2000.</summary>
    KenwoodTs2000,
    /// <summary>Kenwood TS-570.</summary>
    KenwoodTs570,
    /// <summary>Kenwood TH-D74.</summary>
    KenwoodThD74,
    /// <summary>Kenwood TH-D75.</summary>
    KenwoodThD75,

    // =========================================================================
    // Elecraft — protocolo compatible Kenwood
    // =========================================================================

    /// <summary>Elecraft K3.</summary>
    ElecraftK3,
    /// <summary>Elecraft KX3.</summary>
    ElecraftKx3,
    /// <summary>Elecraft K4.</summary>
    ElecraftK4,
    /// <summary>Elecraft KX2.</summary>
    ElecraftKx2,
    /// <summary>Elecraft K2.</summary>
    ElecraftK2,

    // =========================================================================
    // FlexRadio — protocolo propio TCP (soporte futuro)
    // =========================================================================

    /// <summary>FlexRadio Flex-6400.</summary>
    Flex6400,
    /// <summary>FlexRadio Flex-6600.</summary>
    Flex6600,
    /// <summary>FlexRadio Flex-6700.</summary>
    Flex6700
}
