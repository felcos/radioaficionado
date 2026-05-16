namespace RadioAficionado.Servicio.Protocolo;

/// <summary>
/// Tipos de mensaje del protocolo UDP de WSJT-X.
/// Referencia: NetworkMessage.hpp de WSJT-X.
/// </summary>
public enum TipoMensajeWsjtx : uint
{
    /// <summary>Heartbeat — emitido periodicamente.</summary>
    Heartbeat = 0,
    /// <summary>Status — estado actual de la operacion.</summary>
    Status = 1,
    /// <summary>Decode — mensaje decodificado.</summary>
    Decode = 2,
    /// <summary>Clear — solicitud de limpiar decodificaciones (recibido).</summary>
    Clear = 3,
    /// <summary>Reply — respuesta a una decodificacion (recibido).</summary>
    Reply = 4,
    /// <summary>QSO Logged — QSO registrado en el logbook.</summary>
    QSOLogged = 5,
    /// <summary>Close — la aplicacion se esta cerrando.</summary>
    Close = 6,
    /// <summary>Replay — solicitud de reenviar decodificaciones.</summary>
    Replay = 7,
    /// <summary>Halt TX — detener transmision (recibido).</summary>
    HaltTx = 8,
    /// <summary>Free Text — texto libre (recibido).</summary>
    FreeText = 9,
    /// <summary>WSPR Decode — spot WSPR decodificado.</summary>
    WSPRDecode = 10,
    /// <summary>Location — ubicacion de la estacion.</summary>
    Location = 11,
    /// <summary>Logged ADIF — QSO en formato ADIF.</summary>
    LoggedADIF = 12,
    /// <summary>Highlight Callsign — resaltar un indicativo (recibido).</summary>
    HighlightCallsign = 13,
    /// <summary>Switch Configuration — cambiar configuracion (recibido).</summary>
    SwitchConfiguration = 14,
    /// <summary>Configure — configuracion actual.</summary>
    Configure = 15
}

/// <summary>
/// Magic number del protocolo WSJT-X UDP.
/// </summary>
public static class ProtocoloWsjtx
{
    /// <summary>Magic number: 0xADBCCBDA.</summary>
    public const uint Magic = 0xADBCCBDA;

    /// <summary>Version del esquema.</summary>
    public const uint SchemaVersion = 2;

    /// <summary>Puerto UDP por defecto.</summary>
    public const int PuertoDefecto = 2237;

    /// <summary>Identificador de la aplicacion.</summary>
    public const string IdAplicacion = "RadioAficionado";
}

/// <summary>
/// Mensaje Heartbeat del protocolo WSJT-X.
/// </summary>
/// <param name="Id">Identificador de la aplicacion.</param>
/// <param name="MaxSchemaVersion">Version maxima del esquema soportada.</param>
/// <param name="Version">Version de la aplicacion.</param>
/// <param name="Revision">Revision del codigo.</param>
public sealed record MensajeHeartbeat(
    string Id,
    uint MaxSchemaVersion,
    string Version,
    string Revision);

/// <summary>
/// Mensaje Status del protocolo WSJT-X.
/// </summary>
public sealed record MensajeStatus(
    string Id,
    ulong FrecuenciaDialHz,
    string Modo,
    string DxCall,
    string Report,
    string TxMode,
    bool TxEnabled,
    bool Transmitting,
    bool Decoding,
    uint RxDF,
    uint TxDF,
    string DeCall,
    string DeGrid,
    string DxGrid,
    bool TxWatchdog,
    string SubMode,
    bool FastMode,
    byte SpecialOperationMode,
    uint FrequencyTolerance,
    uint TRPeriod,
    string ConfigurationName);

/// <summary>
/// Mensaje Decode del protocolo WSJT-X.
/// </summary>
public sealed record MensajeDecode(
    string Id,
    bool New,
    uint TimeMs,
    int Snr,
    double DeltaTime,
    uint DeltaFrequency,
    string Mode,
    string Message,
    bool LowConfidence,
    bool OffAir);

/// <summary>
/// Mensaje QSO Logged del protocolo WSJT-X.
/// </summary>
public sealed record MensajeQsoLogged(
    string Id,
    DateTime DateTimeOff,
    string DxCall,
    string DxGrid,
    ulong TxFrequencyHz,
    string Mode,
    string ReportSent,
    string ReportReceived,
    string TxPower,
    string Comments,
    string Name,
    DateTime DateTimeOn,
    string OperatorCall,
    string MyCall,
    string MyGrid);
