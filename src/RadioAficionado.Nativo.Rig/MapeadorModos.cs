using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.Rig;

/// <summary>
/// Mapea entre las cadenas de modo de rigctld (Hamlib) y los enums
/// <see cref="ModoOperacion"/> / <see cref="SubModoOperacion"/> del dominio.
/// </summary>
public static class MapeadorModos
{
    /// <summary>
    /// Resultado del mapeo de un modo rigctld al dominio.
    /// </summary>
    /// <param name="Modo">Modo de operación principal.</param>
    /// <param name="SubModo">Submodo opcional (ej: USB/LSB para SSB).</param>
    public readonly record struct ResultadoModo(ModoOperacion Modo, SubModoOperacion? SubModo);

    /// <summary>
    /// Convierte una cadena de modo rigctld al modo y submodo del dominio.
    /// </summary>
    /// <param name="modoRigctld">Cadena de modo devuelta por rigctld (ej: "USB", "CW", "PKTUSB").</param>
    /// <returns>Tupla con el modo y submodo correspondientes.</returns>
    /// <exception cref="ArgumentException">Si la cadena de modo está vacía.</exception>
    public static ResultadoModo DesdeRigctld(string modoRigctld)
    {
        if (string.IsNullOrWhiteSpace(modoRigctld))
        {
            throw new ArgumentException("La cadena de modo rigctld no puede estar vacía.", nameof(modoRigctld));
        }

        string modoNormalizado = modoRigctld.Trim().ToUpperInvariant();

        return modoNormalizado switch
        {
            "USB" => new ResultadoModo(ModoOperacion.SSB, SubModoOperacion.USB),
            "LSB" => new ResultadoModo(ModoOperacion.SSB, SubModoOperacion.LSB),
            "CW" or "CWR" => new ResultadoModo(ModoOperacion.CW, null),
            "AM" => new ResultadoModo(ModoOperacion.AM, null),
            "FM" or "WFM" => new ResultadoModo(ModoOperacion.FM, null),
            "RTTY" or "RTTYR" => new ResultadoModo(ModoOperacion.RTTY, null),
            "PKTUSB" => new ResultadoModo(ModoOperacion.PKT, SubModoOperacion.USB),
            "PKTLSB" => new ResultadoModo(ModoOperacion.PKT, SubModoOperacion.LSB),
            "PKTFM" => new ResultadoModo(ModoOperacion.PKT, null),
            "FT8" => new ResultadoModo(ModoOperacion.FT8, null),
            "FT4" => new ResultadoModo(ModoOperacion.FT4, null),
            "PSK" or "PSK31" => new ResultadoModo(ModoOperacion.PSK, SubModoOperacion.PSK31),
            "PSK63" => new ResultadoModo(ModoOperacion.PSK, SubModoOperacion.PSK63),
            "PSK125" => new ResultadoModo(ModoOperacion.PSK, SubModoOperacion.PSK125),
            "MFSK" => new ResultadoModo(ModoOperacion.MFSK, null),
            "OLIVIA" => new ResultadoModo(ModoOperacion.OLIVIA, null),
            "JT65" => new ResultadoModo(ModoOperacion.JT65, null),
            "JT9" => new ResultadoModo(ModoOperacion.JT9, null),
            "WSPR" => new ResultadoModo(ModoOperacion.WSPR, null),
            "JS8" => new ResultadoModo(ModoOperacion.JS8, null),
            "MSK144" => new ResultadoModo(ModoOperacion.MSK144, null),
            "Q65" => new ResultadoModo(ModoOperacion.Q65, null),
            "FST4" => new ResultadoModo(ModoOperacion.FST4, null),
            "FST4W" => new ResultadoModo(ModoOperacion.FST4W, null),
            "SSTV" => new ResultadoModo(ModoOperacion.SSTV, null),
            _ => new ResultadoModo(ModoOperacion.SSB, SubModoOperacion.USB)
        };
    }

    /// <summary>
    /// Convierte un modo y submodo del dominio a la cadena de modo que espera rigctld.
    /// </summary>
    /// <param name="modo">Modo de operación principal.</param>
    /// <param name="subModo">Submodo opcional.</param>
    /// <returns>Cadena de modo para enviar a rigctld (ej: "USB", "CW", "PKTUSB").</returns>
    public static string HaciaRigctld(ModoOperacion modo, SubModoOperacion? subModo)
    {
        return modo switch
        {
            ModoOperacion.SSB => subModo switch
            {
                SubModoOperacion.LSB => "LSB",
                _ => "USB"
            },
            ModoOperacion.CW => "CW",
            ModoOperacion.AM => "AM",
            ModoOperacion.FM => "FM",
            ModoOperacion.RTTY => "RTTY",
            ModoOperacion.PKT => subModo switch
            {
                SubModoOperacion.LSB => "PKTLSB",
                _ => "PKTUSB"
            },
            ModoOperacion.FT8 => "PKTUSB",
            ModoOperacion.FT4 => "PKTUSB",
            ModoOperacion.PSK => subModo switch
            {
                SubModoOperacion.PSK63 => "PSK63",
                SubModoOperacion.PSK125 => "PSK125",
                _ => "PSK31"
            },
            ModoOperacion.MFSK => "MFSK",
            ModoOperacion.OLIVIA => "OLIVIA",
            ModoOperacion.JT65 => "JT65",
            ModoOperacion.JT9 => "JT9",
            ModoOperacion.WSPR => "WSPR",
            ModoOperacion.JS8 => "JS8",
            ModoOperacion.MSK144 => "MSK144",
            ModoOperacion.Q65 => "Q65",
            ModoOperacion.FST4 => "FST4",
            ModoOperacion.FST4W => "FST4W",
            ModoOperacion.SSTV => "SSTV",
            _ => "USB"
        };
    }

    /// <summary>
    /// Convierte una cadena de VFO de rigctld al carácter de VFO del dominio.
    /// </summary>
    /// <param name="vfoRigctld">Cadena VFO de rigctld (ej: "VFOA", "VFOB", "Main", "Sub").</param>
    /// <returns>Carácter que identifica el VFO ('A' o 'B').</returns>
    public static char VfoDesdeRigctld(string vfoRigctld)
    {
        if (string.IsNullOrWhiteSpace(vfoRigctld))
        {
            return 'A';
        }

        string vfoNormalizado = vfoRigctld.Trim().ToUpperInvariant();

        return vfoNormalizado switch
        {
            "VFOB" or "SUB" => 'B',
            _ => 'A'
        };
    }

    /// <summary>
    /// Convierte un carácter de VFO del dominio a la cadena que espera rigctld.
    /// </summary>
    /// <param name="vfo">Carácter del VFO ('A' o 'B').</param>
    /// <returns>Cadena VFO para rigctld (ej: "VFOA", "VFOB").</returns>
    public static string VfoHaciaRigctld(char vfo)
    {
        return vfo switch
        {
            'B' or 'b' => "VFOB",
            _ => "VFOA"
        };
    }

    /// <summary>
    /// Convierte un valor de intensidad de señal en dBm a unidades S (S-meter).
    /// S9 = -73 dBm, cada unidad S = 6 dB.
    /// </summary>
    /// <param name="dBm">Nivel de señal en dBm (valor negativo).</param>
    /// <returns>Nivel S-meter como entero (0 a 9+, donde valores > 9 indican S9+XdB).</returns>
    public static int ConvertirDbmAUnidadesS(double dBm)
    {
        // S9 = -73 dBm, cada unidad S = 6 dB
        // S0 = -127 dBm, S1 = -121 dBm, ..., S9 = -73 dBm
        // Por encima de S9: S9+10 = -63 dBm, S9+20 = -53 dBm, etc.
        const double DbmEnS9 = -73.0;
        const double DbPorUnidadS = 6.0;

        double unidadesSDecimales = (dBm - DbmEnS9) / DbPorUnidadS + 9.0;

        int nivelS = (int)Math.Round(unidadesSDecimales);

        return Math.Max(0, nivelS);
    }
}
