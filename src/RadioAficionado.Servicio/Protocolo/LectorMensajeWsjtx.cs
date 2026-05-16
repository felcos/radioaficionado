using System.Text;

namespace RadioAficionado.Servicio.Protocolo;

/// <summary>
/// Deserializa mensajes del formato binario QDataStream del protocolo WSJT-X UDP.
/// Big-endian. Strings: uint32 length + UTF-8 bytes.
/// </summary>
public sealed class LectorMensajeWsjtx
{
    private readonly byte[] _datos;
    private int _posicion;

    /// <summary>
    /// Crea un lector de mensajes WSJT-X a partir de bytes recibidos.
    /// </summary>
    public LectorMensajeWsjtx(byte[] datos)
    {
        _datos = datos ?? throw new ArgumentNullException(nameof(datos));
        _posicion = 0;
    }

    /// <summary>Lee y valida el header del mensaje. Retorna el tipo de mensaje.</summary>
    public TipoMensajeWsjtx? LeerHeader()
    {
        if (_datos.Length < 12) { return null; }

        uint magic = LeerUInt32();
        if (magic != ProtocoloWsjtx.Magic) { return null; }

        uint schema = LeerUInt32();
        if (schema != ProtocoloWsjtx.SchemaVersion) { return null; }

        uint tipo = LeerUInt32();
        return (TipoMensajeWsjtx)tipo;
    }

    /// <summary>Lee un uint32 big-endian.</summary>
    public uint LeerUInt32()
    {
        if (_posicion + 4 > _datos.Length) { return 0; }

        byte[] bytes = new byte[4];
        Array.Copy(_datos, _posicion, bytes, 0, 4);
        _posicion += 4;

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>Lee un int32 big-endian.</summary>
    public int LeerInt32()
    {
        if (_posicion + 4 > _datos.Length) { return 0; }

        byte[] bytes = new byte[4];
        Array.Copy(_datos, _posicion, bytes, 0, 4);
        _posicion += 4;

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToInt32(bytes, 0);
    }

    /// <summary>Lee un uint64 big-endian.</summary>
    public ulong LeerUInt64()
    {
        if (_posicion + 8 > _datos.Length) { return 0; }

        byte[] bytes = new byte[8];
        Array.Copy(_datos, _posicion, bytes, 0, 8);
        _posicion += 8;

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToUInt64(bytes, 0);
    }

    /// <summary>Lee un double big-endian.</summary>
    public double LeerDouble()
    {
        if (_posicion + 8 > _datos.Length) { return 0; }

        byte[] bytes = new byte[8];
        Array.Copy(_datos, _posicion, bytes, 0, 8);
        _posicion += 8;

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToDouble(bytes, 0);
    }

    /// <summary>Lee un bool como byte.</summary>
    public bool LeerBool()
    {
        if (_posicion >= _datos.Length) { return false; }
        byte valor = _datos[_posicion];
        _posicion += 1;
        return valor != 0;
    }

    /// <summary>Lee un byte.</summary>
    public byte LeerByte()
    {
        if (_posicion >= _datos.Length) { return 0; }
        byte valor = _datos[_posicion];
        _posicion += 1;
        return valor;
    }

    /// <summary>Lee un string QDataStream (uint32 length + UTF-8 bytes).</summary>
    public string? LeerString()
    {
        uint longitud = LeerUInt32();

        if (longitud == 0xFFFFFFFF) { return null; }
        if (longitud == 0) { return string.Empty; }
        if (_posicion + (int)longitud > _datos.Length) { return null; }

        string resultado = Encoding.UTF8.GetString(_datos, _posicion, (int)longitud);
        _posicion += (int)longitud;
        return resultado;
    }

    /// <summary>Posicion actual de lectura.</summary>
    public int Posicion => _posicion;

    /// <summary>Bytes restantes por leer.</summary>
    public int BytesRestantes => _datos.Length - _posicion;
}
