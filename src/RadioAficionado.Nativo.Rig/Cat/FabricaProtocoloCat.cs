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

            // Yaesu
            ModeloRadio.YaesuFt991 or ModeloRadio.YaesuFt991A or
            ModeloRadio.YaesuFt891 or ModeloRadio.YaesuFt710 or
            ModeloRadio.YaesuFtdx10 or ModeloRadio.YaesuFtdx101
                => new ProtocoloYaesu(),

            // Icom — cada modelo tiene su dirección CI-V
            ModeloRadio.IcomIc7300 => new ProtocoloIcom(0x94),
            ModeloRadio.IcomIc7100 => new ProtocoloIcom(0x88),
            ModeloRadio.IcomIc705 => new ProtocoloIcom(0xA4),
            ModeloRadio.IcomIc9700 => new ProtocoloIcom(0xA2),

            // Kenwood / Elecraft
            ModeloRadio.KenwoodTs890 or ModeloRadio.KenwoodTs590 or
            ModeloRadio.ElecraftK3 or ModeloRadio.ElecraftKx3
                => new ProtocoloKenwood(),

            _ => null
        };
    }
}
