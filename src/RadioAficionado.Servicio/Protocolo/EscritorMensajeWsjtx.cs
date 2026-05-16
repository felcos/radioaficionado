using System.Text;

namespace RadioAficionado.Servicio.Protocolo;

/// <summary>
/// Serializa mensajes al formato binario QDataStream del protocolo WSJT-X UDP.
/// Big-endian. Strings: uint32 length + UTF-8 bytes (0xFFFFFFFF = null).
/// Referencia: NetworkMessage.hpp de WSJT-X.
/// </summary>
public sealed class EscritorMensajeWsjtx
{
    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;

    /// <summary>
    /// Crea un nuevo escritor de mensajes WSJT-X.
    /// </summary>
    public EscritorMensajeWsjtx()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream);
    }

    /// <summary>Escribe el header del mensaje (magic + schema + tipo).</summary>
    public void EscribirHeader(TipoMensajeWsjtx tipo)
    {
        EscribirUInt32(ProtocoloWsjtx.Magic);
        EscribirUInt32(ProtocoloWsjtx.SchemaVersion);
        EscribirUInt32((uint)tipo);
    }

    /// <summary>Escribe un uint32 big-endian.</summary>
    public void EscribirUInt32(uint valor)
    {
        byte[] bytes = BitConverter.GetBytes(valor);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _writer.Write(bytes);
    }

    /// <summary>Escribe un int32 big-endian.</summary>
    public void EscribirInt32(int valor)
    {
        byte[] bytes = BitConverter.GetBytes(valor);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _writer.Write(bytes);
    }

    /// <summary>Escribe un uint64 big-endian.</summary>
    public void EscribirUInt64(ulong valor)
    {
        byte[] bytes = BitConverter.GetBytes(valor);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _writer.Write(bytes);
    }

    /// <summary>Escribe un double big-endian (IEEE 754).</summary>
    public void EscribirDouble(double valor)
    {
        byte[] bytes = BitConverter.GetBytes(valor);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _writer.Write(bytes);
    }

    /// <summary>Escribe un bool como byte.</summary>
    public void EscribirBool(bool valor)
    {
        _writer.Write(valor ? (byte)1 : (byte)0);
    }

    /// <summary>Escribe un byte.</summary>
    public void EscribirByte(byte valor)
    {
        _writer.Write(valor);
    }

    /// <summary>Escribe un string QDataStream (uint32 length + UTF-8 bytes, 0xFFFFFFFF = null).</summary>
    public void EscribirString(string? valor)
    {
        if (valor is null)
        {
            EscribirUInt32(0xFFFFFFFF);
            return;
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(valor);
        EscribirUInt32((uint)utf8.Length);
        _writer.Write(utf8);
    }

    /// <summary>Escribe un DateTime como QDateTime (date + time + timespec).</summary>
    public void EscribirDateTime(DateTime valor)
    {
        // QDate: Julian Day Number como int64
        // QTime: milliseconds since midnight como uint32
        // Timespec: 1 = UTC

        long julianDay = CalcularDiaJuliano(valor);
        EscribirUInt64((ulong)julianDay);

        uint msDesdeMedianoche = (uint)(valor.Hour * 3600000 + valor.Minute * 60000 +
            valor.Second * 1000 + valor.Millisecond);
        EscribirUInt32(msDesdeMedianoche);
        EscribirByte(1); // UTC
    }

    /// <summary>Obtiene los bytes serializados.</summary>
    public byte[] ObtenerBytes()
    {
        _writer.Flush();
        return _stream.ToArray();
    }

    // ================================================================
    // MENSAJES COMPLETOS
    // ================================================================

    /// <summary>Serializa un mensaje Heartbeat.</summary>
    public static byte[] SerializarHeartbeat(MensajeHeartbeat heartbeat)
    {
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirHeader(TipoMensajeWsjtx.Heartbeat);
        escritor.EscribirString(heartbeat.Id);
        escritor.EscribirUInt32(heartbeat.MaxSchemaVersion);
        escritor.EscribirString(heartbeat.Version);
        escritor.EscribirString(heartbeat.Revision);
        return escritor.ObtenerBytes();
    }

    /// <summary>Serializa un mensaje Status.</summary>
    public static byte[] SerializarStatus(MensajeStatus status)
    {
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirHeader(TipoMensajeWsjtx.Status);
        escritor.EscribirString(status.Id);
        escritor.EscribirUInt64(status.FrecuenciaDialHz);
        escritor.EscribirString(status.Modo);
        escritor.EscribirString(status.DxCall);
        escritor.EscribirString(status.Report);
        escritor.EscribirString(status.TxMode);
        escritor.EscribirBool(status.TxEnabled);
        escritor.EscribirBool(status.Transmitting);
        escritor.EscribirBool(status.Decoding);
        escritor.EscribirUInt32(status.RxDF);
        escritor.EscribirUInt32(status.TxDF);
        escritor.EscribirString(status.DeCall);
        escritor.EscribirString(status.DeGrid);
        escritor.EscribirString(status.DxGrid);
        escritor.EscribirBool(status.TxWatchdog);
        escritor.EscribirString(status.SubMode);
        escritor.EscribirBool(status.FastMode);
        escritor.EscribirByte(status.SpecialOperationMode);
        escritor.EscribirUInt32(status.FrequencyTolerance);
        escritor.EscribirUInt32(status.TRPeriod);
        escritor.EscribirString(status.ConfigurationName);
        return escritor.ObtenerBytes();
    }

    /// <summary>Serializa un mensaje Decode.</summary>
    public static byte[] SerializarDecode(MensajeDecode decode)
    {
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirHeader(TipoMensajeWsjtx.Decode);
        escritor.EscribirString(decode.Id);
        escritor.EscribirBool(decode.New);
        escritor.EscribirUInt32(decode.TimeMs);
        escritor.EscribirInt32(decode.Snr);
        escritor.EscribirDouble(decode.DeltaTime);
        escritor.EscribirUInt32(decode.DeltaFrequency);
        escritor.EscribirString(decode.Mode);
        escritor.EscribirString(decode.Message);
        escritor.EscribirBool(decode.LowConfidence);
        escritor.EscribirBool(decode.OffAir);
        return escritor.ObtenerBytes();
    }

    /// <summary>Serializa un mensaje QSO Logged.</summary>
    public static byte[] SerializarQsoLogged(MensajeQsoLogged qso)
    {
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirHeader(TipoMensajeWsjtx.QSOLogged);
        escritor.EscribirString(qso.Id);
        escritor.EscribirDateTime(qso.DateTimeOff);
        escritor.EscribirString(qso.DxCall);
        escritor.EscribirString(qso.DxGrid);
        escritor.EscribirUInt64(qso.TxFrequencyHz);
        escritor.EscribirString(qso.Mode);
        escritor.EscribirString(qso.ReportSent);
        escritor.EscribirString(qso.ReportReceived);
        escritor.EscribirString(qso.TxPower);
        escritor.EscribirString(qso.Comments);
        escritor.EscribirString(qso.Name);
        escritor.EscribirDateTime(qso.DateTimeOn);
        escritor.EscribirString(qso.OperatorCall);
        escritor.EscribirString(qso.MyCall);
        escritor.EscribirString(qso.MyGrid);
        return escritor.ObtenerBytes();
    }

    /// <summary>Serializa un string ADIF como mensaje LoggedADIF.</summary>
    public static byte[] SerializarAdif(string id, string adif)
    {
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirHeader(TipoMensajeWsjtx.LoggedADIF);
        escritor.EscribirString(id);
        escritor.EscribirString(adif);
        return escritor.ObtenerBytes();
    }

    private static long CalcularDiaJuliano(DateTime fecha)
    {
        int a = (14 - fecha.Month) / 12;
        int y = fecha.Year + 4800 - a;
        int m = fecha.Month + (12 * a) - 3;
        return fecha.Day + ((153 * m + 2) / 5) + (365 * y) + (y / 4) - (y / 100) + (y / 400) - 32045;
    }
}
