namespace RadioAficionado.Nativo.Rig.Cat;

/// <summary>
/// Fábrica para crear instancias del protocolo CAT adecuado según el modelo de radio.
/// </summary>
public static class FabricaProtocoloCat
{
    /// <summary>
    /// Crea una instancia del protocolo CAT correspondiente al modelo de radio especificado.
    /// </summary>
    /// <param name="modelo">Modelo de radio para el que se quiere obtener el protocolo.</param>
    /// <returns>
    /// Instancia de <see cref="IProtocoloCat"/> correspondiente al modelo,
    /// o null si el modelo es <see cref="ModeloRadio.Automatico"/> (se detectará al conectar).
    /// </returns>
    public static IProtocoloCat? Crear(ModeloRadio modelo)
    {
        return modelo switch
        {
            ModeloRadio.Automatico => null,

            // Yaesu — protocolo texto con terminador ';'
            ModeloRadio.YaesuFt991 or ModeloRadio.YaesuFt991A or
            ModeloRadio.YaesuFt891 or ModeloRadio.YaesuFt710 or
            ModeloRadio.YaesuFtdx10 or ModeloRadio.YaesuFtdx101 or
            ModeloRadio.YaesuFt450 or ModeloRadio.YaesuFt450D or
            ModeloRadio.YaesuFt950 or ModeloRadio.YaesuFt1200 or
            ModeloRadio.YaesuFt2000 or ModeloRadio.YaesuFt3000 or
            ModeloRadio.YaesuFt5000 or ModeloRadio.YaesuFtdx101Mp or
            ModeloRadio.YaesuFtdx3000 or ModeloRadio.YaesuFtdx5000
                => new ProtocoloYaesu(),

            // Icom — cada modelo tiene su dirección CI-V
            ModeloRadio.IcomIc7300 => new ProtocoloIcom(0x94),
            ModeloRadio.IcomIc7100 => new ProtocoloIcom(0x88),
            ModeloRadio.IcomIc705 => new ProtocoloIcom(0xA4),
            ModeloRadio.IcomIc9700 => new ProtocoloIcom(0xA2),
            ModeloRadio.IcomIc7610 => new ProtocoloIcom(0x98),
            ModeloRadio.IcomIc7851 => new ProtocoloIcom(0x8E),
            ModeloRadio.IcomIc7400 => new ProtocoloIcom(0x66),
            ModeloRadio.IcomIc746 => new ProtocoloIcom(0x56),
            ModeloRadio.IcomIc746Pro => new ProtocoloIcom(0x66),
            ModeloRadio.IcomIc756Pro3 => new ProtocoloIcom(0x6E),
            ModeloRadio.IcomIc718 => new ProtocoloIcom(0x5E),
            ModeloRadio.IcomIcR8600 => new ProtocoloIcom(0xA6),

            // Kenwood / Elecraft — protocolo texto Kenwood
            ModeloRadio.KenwoodTs890 or ModeloRadio.KenwoodTs590 or
            ModeloRadio.KenwoodTs990 or ModeloRadio.KenwoodTs480 or
            ModeloRadio.KenwoodTs2000 or ModeloRadio.KenwoodTs570 or
            ModeloRadio.KenwoodThD74 or ModeloRadio.KenwoodThD75 or
            ModeloRadio.ElecraftK3 or ModeloRadio.ElecraftKx3 or
            ModeloRadio.ElecraftK4 or ModeloRadio.ElecraftKx2 or
            ModeloRadio.ElecraftK2
                => new ProtocoloKenwood(),

            // FlexRadio — protocolo propio TCP (soporte futuro)
            ModeloRadio.Flex6400 or ModeloRadio.Flex6600 or
            ModeloRadio.Flex6700
                => null,

            _ => null
        };
    }
}
