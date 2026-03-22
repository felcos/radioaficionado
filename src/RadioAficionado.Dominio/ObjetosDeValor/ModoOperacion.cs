namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Modos de operacion principales segun el estandar ADIF 3.1.4.
/// Cada modo principal puede tener submodos asociados en <see cref="SubModoOperacion"/>.
/// </summary>
public enum ModoOperacion
{
    /// <summary>Amplitud modulada.</summary>
    AM,

    /// <summary>ARDOP (Amateur Radio Digital Open Protocol).</summary>
    ARDOP,

    /// <summary>ATV — Television de aficionados.</summary>
    ATV,

    /// <summary>Chip modulacion.</summary>
    CHIP,

    /// <summary>CLO — Onda continua modulada (experimental).</summary>
    CLO,

    /// <summary>Contestia — modo MFSK robusto.</summary>
    CONTESTIA,

    /// <summary>Onda continua (telegrafia Morse).</summary>
    CW,

    /// <summary>Digitalvoice — voz digital generica.</summary>
    DIGITALVOICE,

    /// <summary>DominoEX — IFK con correccion de errores.</summary>
    DOMINO,

    /// <summary>Dynamic Relay Exchange (DSTAR data).</summary>
    DYNAMIC,

    /// <summary>FAX — facsimil por radio.</summary>
    FAX,

    /// <summary>Frecuencia modulada.</summary>
    FM,

    /// <summary>FSK441 — meteor scatter.</summary>
    FSK441,

    /// <summary>FT8 (Franke-Taylor 8-FSK). El modo digital mas popular.</summary>
    FT8,

    /// <summary>FT4 (Franke-Taylor 4-FSK). Disenado para contests rapidos.</summary>
    FT4,

    /// <summary>Hellschreiber — texto como imagen bitmap.</summary>
    HELL,

    /// <summary>ISCAT — modo de scatter ionosferico.</summary>
    ISCAT,

    /// <summary>JT4 — senal extremadamente debil EME.</summary>
    JT4,

    /// <summary>JT6M — meteor scatter 6m.</summary>
    JT6M,

    /// <summary>JT9 — senal debil HF, 1 dB mejor que JT65.</summary>
    JT9,

    /// <summary>JT44 — modo experimental senal debil.</summary>
    JT44,

    /// <summary>JT65 — disenado para EME (moonbounce).</summary>
    JT65,

    /// <summary>JS8 — mensajeria keyboard-to-keyboard sobre FT8.</summary>
    JS8,

    /// <summary>MFSK — Multi-Frequency Shift Keying.</summary>
    MFSK,

    /// <summary>MSK144 — meteor scatter optimizado.</summary>
    MSK144,

    /// <summary>MT63 — modo robusto de gran ancho de banda.</summary>
    MT63,

    /// <summary>Olivia — MFSK extremadamente robusto en condiciones dificiles.</summary>
    OLIVIA,

    /// <summary>Opera — senal debil LF/MF.</summary>
    OPERA,

    /// <summary>PAC — Pactor.</summary>
    PAC,

    /// <summary>PAX — Pactor 2.</summary>
    PAX,

    /// <summary>PKT — Packet radio (AX.25).</summary>
    PKT,

    /// <summary>PSK — Phase Shift Keying.</summary>
    PSK,

    /// <summary>PSK2K — PSK de alto rendimiento.</summary>
    PSK2K,

    /// <summary>Q65 — senal debil para troposcatter, EME, lluvia.</summary>
    Q65,

    /// <summary>QRA64 — senal debil EME VHF+.</summary>
    QRA64,

    /// <summary>ROS — modo de senal debil.</summary>
    ROS,

    /// <summary>RTTY — radioteletipo, el modo digital mas antiguo.</summary>
    RTTY,

    /// <summary>RTTYM — RTTY mediante MFSK.</summary>
    RTTYM,

    /// <summary>SSB — banda lateral unica.</summary>
    SSB,

    /// <summary>SSTV — television de barrido lento.</summary>
    SSTV,

    /// <summary>T10 — modo experimental de 10 tonos.</summary>
    T10,

    /// <summary>Thor — FEC sobre DominoEX.</summary>
    THOR,

    /// <summary>Throb — modo experimental.</summary>
    THROB,

    /// <summary>TOR — Teleprinting Over Radio (AMTOR/SITOR).</summary>
    TOR,

    /// <summary>V4 — modo VARA v4.</summary>
    V4,

    /// <summary>VOI — modo de voz sobre IP por radio.</summary>
    VOI,

    /// <summary>Winmor — Winlink modulation.</summary>
    WINMOR,

    /// <summary>WSPR — Weak Signal Propagation Reporter.</summary>
    WSPR,

    /// <summary>FST4 — senal debil para LF/MF.</summary>
    FST4,

    /// <summary>FST4W — WSPR-like para LF/MF.</summary>
    FST4W
}

/// <summary>
/// Submodos de operacion segun el estandar ADIF 3.1.4.
/// Cada submodo pertenece a un <see cref="ModoOperacion"/> principal.
/// </summary>
public enum SubModoOperacion
{
    /// <summary>Sin submodo especificado.</summary>
    Ninguno,

    // --- SSB submodos ---
    /// <summary>Banda lateral inferior.</summary>
    LSB,
    /// <summary>Banda lateral superior.</summary>
    USB,

    // --- CW submodos ---
    /// <summary>CW por audio (mediante software).</summary>
    PCW,

    // --- Digitalvoice submodos ---
    /// <summary>D-STAR — voz digital Icom.</summary>
    DSTAR,
    /// <summary>DMR — Digital Mobile Radio.</summary>
    DMR,
    /// <summary>C4FM — System Fusion de Yaesu.</summary>
    C4FM,
    /// <summary>FreeDV — voz digital HF open-source (Codec2).</summary>
    FREEDV,
    /// <summary>M17 — voz digital VHF/UHF open-source (Codec2).</summary>
    M17,
    /// <summary>P25 — Project 25 (origen policial).</summary>
    P25,
    /// <summary>NXDN — voz digital narrowband.</summary>
    NXDN,

    // --- PSK submodos ---
    /// <summary>PSK de 31 baudios.</summary>
    PSK31,
    /// <summary>PSK de 63 baudios.</summary>
    PSK63,
    /// <summary>PSK de 125 baudios.</summary>
    PSK125,
    /// <summary>QPSK de 31 baudios.</summary>
    QPSK31,
    /// <summary>QPSK de 63 baudios.</summary>
    QPSK63,
    /// <summary>QPSK de 125 baudios.</summary>
    QPSK125,

    // --- PKT submodos ---
    /// <summary>APRS — Automatic Packet Reporting System.</summary>
    APRS,
    /// <summary>AX.25 packet generico.</summary>
    AX25,

    // --- MFSK submodos ---
    /// <summary>MFSK de 4 tonos.</summary>
    MFSK4,
    /// <summary>MFSK de 8 tonos.</summary>
    MFSK8,
    /// <summary>MFSK de 16 tonos.</summary>
    MFSK16,
    /// <summary>MFSK de 32 tonos.</summary>
    MFSK32,
    /// <summary>MFSK de 64 tonos.</summary>
    MFSK64,

    // --- HELL submodos ---
    /// <summary>Feld Hell original.</summary>
    FMHELL,
    /// <summary>PSK Hell.</summary>
    PSKHELL,
    /// <summary>Hell 80.</summary>
    HELL80,

    // --- JT65 submodos ---
    /// <summary>JT65A — subbanda A.</summary>
    JT65A,
    /// <summary>JT65B — subbanda B (EME).</summary>
    JT65B,
    /// <summary>JT65C — subbanda C.</summary>
    JT65C,

    // --- Olivia submodos ---
    /// <summary>Olivia 8/250.</summary>
    OLIVIA_8_250,
    /// <summary>Olivia 8/500.</summary>
    OLIVIA_8_500,
    /// <summary>Olivia 16/500.</summary>
    OLIVIA_16_500,
    /// <summary>Olivia 16/1000.</summary>
    OLIVIA_16_1000,
    /// <summary>Olivia 32/1000.</summary>
    OLIVIA_32_1000,

    // --- RTTY submodos ---
    /// <summary>ASCI — Baudot ASCII RTTY.</summary>
    ASCI,

    // --- VARA submodos ---
    /// <summary>VARA HF.</summary>
    VARA_HF,
    /// <summary>VARA FM.</summary>
    VARA_FM,
    /// <summary>VARA SAT.</summary>
    VARA_SAT,

    // --- TOR submodos ---
    /// <summary>AMTOR — modo ARQ.</summary>
    AMTOR,
    /// <summary>NAVTEX.</summary>
    NAVTEX
}

/// <summary>
/// Metodos de extension para <see cref="ModoOperacion"/> y <see cref="SubModoOperacion"/>.
/// </summary>
public static class ModoOperacionExtensiones
{
    /// <summary>
    /// Obtiene el modo principal al que pertenece un submodo.
    /// </summary>
    /// <param name="subModo">El submodo a evaluar.</param>
    /// <returns>El <see cref="ModoOperacion"/> principal al que pertenece el submodo.</returns>
    /// <exception cref="ArgumentException">Si el submodo es <see cref="SubModoOperacion.Ninguno"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si el submodo no esta reconocido.</exception>
    public static ModoOperacion ObtenerModoPrincipal(this SubModoOperacion subModo)
    {
        return subModo switch
        {
            SubModoOperacion.Ninguno => throw new ArgumentException("El submodo 'Ninguno' no tiene modo principal.", nameof(subModo)),
            SubModoOperacion.LSB or SubModoOperacion.USB => ModoOperacion.SSB,
            SubModoOperacion.PCW => ModoOperacion.CW,
            SubModoOperacion.DSTAR or SubModoOperacion.DMR or SubModoOperacion.C4FM or
            SubModoOperacion.FREEDV or SubModoOperacion.M17 or SubModoOperacion.P25 or
            SubModoOperacion.NXDN => ModoOperacion.DIGITALVOICE,
            SubModoOperacion.PSK31 or SubModoOperacion.PSK63 or SubModoOperacion.PSK125 or
            SubModoOperacion.QPSK31 or SubModoOperacion.QPSK63 or SubModoOperacion.QPSK125 => ModoOperacion.PSK,
            SubModoOperacion.APRS or SubModoOperacion.AX25 => ModoOperacion.PKT,
            SubModoOperacion.MFSK4 or SubModoOperacion.MFSK8 or SubModoOperacion.MFSK16 or
            SubModoOperacion.MFSK32 or SubModoOperacion.MFSK64 => ModoOperacion.MFSK,
            SubModoOperacion.FMHELL or SubModoOperacion.PSKHELL or SubModoOperacion.HELL80 => ModoOperacion.HELL,
            SubModoOperacion.JT65A or SubModoOperacion.JT65B or SubModoOperacion.JT65C => ModoOperacion.JT65,
            SubModoOperacion.OLIVIA_8_250 or SubModoOperacion.OLIVIA_8_500 or SubModoOperacion.OLIVIA_16_500 or
            SubModoOperacion.OLIVIA_16_1000 or SubModoOperacion.OLIVIA_32_1000 => ModoOperacion.OLIVIA,
            SubModoOperacion.ASCI => ModoOperacion.RTTY,
            SubModoOperacion.VARA_HF or SubModoOperacion.VARA_FM or SubModoOperacion.VARA_SAT => ModoOperacion.V4,
            SubModoOperacion.AMTOR or SubModoOperacion.NAVTEX => ModoOperacion.TOR,
            _ => throw new ArgumentOutOfRangeException(nameof(subModo), subModo, "Submodo no reconocido.")
        };
    }

    /// <summary>
    /// Indica si el modo es digital (vs analogico).
    /// </summary>
    /// <param name="modo">El modo de operacion a evaluar.</param>
    /// <returns>True si el modo es digital.</returns>
    public static bool EsDigital(this ModoOperacion modo)
    {
        return modo is not (ModoOperacion.SSB or ModoOperacion.AM or ModoOperacion.FM);
    }

    /// <summary>
    /// Indica si el modo es de senal debil (weak signal).
    /// </summary>
    /// <param name="modo">El modo de operacion a evaluar.</param>
    /// <returns>True si el modo esta disenado para senales debiles.</returns>
    public static bool EsSenalDebil(this ModoOperacion modo)
    {
        return modo is ModoOperacion.FT8 or ModoOperacion.FT4 or ModoOperacion.JT65 or
            ModoOperacion.JT9 or ModoOperacion.JT4 or ModoOperacion.Q65 or
            ModoOperacion.QRA64 or ModoOperacion.MSK144 or ModoOperacion.WSPR or
            ModoOperacion.FST4 or ModoOperacion.FST4W or ModoOperacion.ISCAT;
    }

    /// <summary>
    /// Obtiene la cadena ADIF estandar para este modo.
    /// </summary>
    /// <param name="modo">El modo de operacion.</param>
    /// <returns>Nombre del modo tal como se usa en archivos ADIF.</returns>
    public static string ObtenerNombreAdif(this ModoOperacion modo)
    {
        return modo.ToString();
    }

    /// <summary>
    /// Intenta convertir una cadena ADIF a un ModoOperacion.
    /// </summary>
    /// <param name="nombreAdif">Nombre del modo en formato ADIF.</param>
    /// <param name="modo">El modo resultante si la conversion fue exitosa.</param>
    /// <returns>True si se pudo convertir la cadena a un modo valido.</returns>
    public static bool IntentarDesdeAdif(string nombreAdif, out ModoOperacion modo)
    {
        return Enum.TryParse(nombreAdif, ignoreCase: true, out modo);
    }
}
