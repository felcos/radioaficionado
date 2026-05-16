using System.Globalization;
using System.Text;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.Rig.Cat;

/// <summary>
/// Protocolo Kenwood para radios TS-890, TS-590, Elecraft K3 y KX3.
/// Comandos de texto terminados en punto y coma (;).
/// Compatible también con Elecraft que usa el mismo protocolo.
/// </summary>
public sealed class ProtocoloKenwood : IProtocoloCat
{
    /// <inheritdoc />
    public string NombreFabricante => "Kenwood";

    /// <inheritdoc />
    public int TamanoRespuestaFrecuencia => 14; // FA00014074000;

    /// <inheritdoc />
    public int TamanoRespuestaModo => 4; // MD1;

    /// <inheritdoc />
    public int TamanoRespuestaPtt => 4; // TX0;

    /// <inheritdoc />
    public int TamanoRespuestaNivelSenal => 7; // SM0025;

    /// <inheritdoc />
    public int TamanoRespuestaSplit => 4; // FT0; o FT1;

    /// <inheritdoc />
    public byte[] ComandoLeerFrecuencia()
    {
        return Encoding.ASCII.GetBytes("FA;");
    }

    /// <inheritdoc />
    public byte[] ComandoCambiarFrecuencia(long frecuenciaHz)
    {
        string comando = $"FA{frecuenciaHz:D11};";
        return Encoding.ASCII.GetBytes(comando);
    }

    /// <inheritdoc />
    public byte[] ComandoLeerModo()
    {
        return Encoding.ASCII.GetBytes("MD;");
    }

    /// <inheritdoc />
    public byte[] ComandoCambiarModo(ModoOperacion modo)
    {
        string codigoModo = MapearModoAKenwood(modo);
        string comando = $"MD{codigoModo};";
        return Encoding.ASCII.GetBytes(comando);
    }

    /// <inheritdoc />
    public byte[] ComandoLeerPtt()
    {
        return Encoding.ASCII.GetBytes("TX;");
    }

    /// <inheritdoc />
    public byte[] ComandoCambiarPtt(bool activar)
    {
        string comando = activar ? "TX;" : "RX;";
        return Encoding.ASCII.GetBytes(comando);
    }

    /// <inheritdoc />
    public byte[] ComandoLeerNivelSenal()
    {
        return Encoding.ASCII.GetBytes("SM;");
    }

    /// <inheritdoc />
    public long ParsearFrecuencia(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaFrecuencia)
        {
            throw new InvalidOperationException(
                $"Respuesta de frecuencia Kenwood inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaFrecuencia}.");
        }

        string texto = Encoding.ASCII.GetString(respuesta, 0, TamanoRespuestaFrecuencia);

        // Formato: FA00014074000;
        if (!texto.StartsWith("FA", StringComparison.Ordinal) || !texto.EndsWith(';'))
        {
            throw new InvalidOperationException($"Respuesta de frecuencia Kenwood inválida: '{texto}'.");
        }

        string valorFrecuencia = texto.Substring(2, 11);

        if (!long.TryParse(valorFrecuencia, NumberStyles.Integer, CultureInfo.InvariantCulture, out long frecuenciaHz))
        {
            throw new InvalidOperationException($"No se pudo parsear la frecuencia Kenwood: '{valorFrecuencia}'.");
        }

        return frecuenciaHz;
    }

    /// <inheritdoc />
    public (ModoOperacion modo, SubModoOperacion? submodo) ParsearModo(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaModo)
        {
            throw new InvalidOperationException(
                $"Respuesta de modo Kenwood inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaModo}.");
        }

        string texto = Encoding.ASCII.GetString(respuesta, 0, TamanoRespuestaModo);

        // Formato: MDX; donde X es el código de modo
        if (!texto.StartsWith("MD", StringComparison.Ordinal) || !texto.EndsWith(';'))
        {
            throw new InvalidOperationException($"Respuesta de modo Kenwood inválida: '{texto}'.");
        }

        char codigoModo = texto[2];

        return codigoModo switch
        {
            '1' => (ModoOperacion.SSB, SubModoOperacion.LSB),
            '2' => (ModoOperacion.SSB, SubModoOperacion.USB),
            '3' => (ModoOperacion.CW, null),
            '4' => (ModoOperacion.FM, null),
            '5' => (ModoOperacion.AM, null),
            '6' => (ModoOperacion.RTTY, null),        // FSK
            '7' => (ModoOperacion.CW, null),           // CW-R
            '9' => (ModoOperacion.RTTY, null),         // FSK-R
            _ => (ModoOperacion.SSB, SubModoOperacion.USB)
        };
    }

    /// <inheritdoc />
    public bool ParsearPtt(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaPtt)
        {
            throw new InvalidOperationException(
                $"Respuesta de PTT Kenwood inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaPtt}.");
        }

        string texto = Encoding.ASCII.GetString(respuesta, 0, TamanoRespuestaPtt);

        // Formato: TX0; o TX1;
        if (!texto.StartsWith("TX", StringComparison.Ordinal) || !texto.EndsWith(';'))
        {
            throw new InvalidOperationException($"Respuesta de PTT Kenwood inválida: '{texto}'.");
        }

        return texto[2] != '0';
    }

    /// <inheritdoc />
    public int ParsearNivelSenal(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaNivelSenal)
        {
            throw new InvalidOperationException(
                $"Respuesta de S-meter Kenwood inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaNivelSenal}.");
        }

        string texto = Encoding.ASCII.GetString(respuesta, 0, TamanoRespuestaNivelSenal);

        // Formato: SM0XXX; o SMXXXX; — usamos SM seguido de 4 dígitos
        if (!texto.StartsWith("SM", StringComparison.Ordinal) || !texto.EndsWith(';'))
        {
            throw new InvalidOperationException($"Respuesta de S-meter Kenwood inválida: '{texto}'.");
        }

        string valorNivel = texto.Substring(2, 4);

        if (!int.TryParse(valorNivel, NumberStyles.Integer, CultureInfo.InvariantCulture, out int nivel))
        {
            throw new InvalidOperationException($"No se pudo parsear el nivel de señal Kenwood: '{valorNivel}'.");
        }

        // Kenwood devuelve 0000-0030 (S0 a S9+60dB)
        return Math.Clamp(nivel, 0, 30);
    }

    /// <inheritdoc />
    public byte[] ComandoActivarSplit(bool activar)
    {
        // FT0 = split off, FT1 = split on (FR0;FT1; para RX en VFO A, TX en VFO B)
        string comando = activar ? "FT1;" : "FT0;";
        return Encoding.ASCII.GetBytes(comando);
    }

    /// <inheritdoc />
    public byte[] ComandoLeerSplit()
    {
        return Encoding.ASCII.GetBytes("FT;");
    }

    /// <inheritdoc />
    public bool ParsearSplit(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaSplit)
        {
            throw new InvalidOperationException(
                $"Respuesta de split Kenwood inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaSplit}.");
        }

        string texto = Encoding.ASCII.GetString(respuesta, 0, TamanoRespuestaSplit);

        // Formato: FT0; o FT1;
        if (!texto.StartsWith("FT", StringComparison.Ordinal) || !texto.EndsWith(';'))
        {
            throw new InvalidOperationException($"Respuesta de split Kenwood inválida: '{texto}'.");
        }

        return texto[2] != '0';
    }

    /// <inheritdoc />
    public byte[] ComandoCambiarFrecuenciaVfoB(long frecuenciaHz)
    {
        // FB = Set VFO B frequency
        string comando = $"FB{frecuenciaHz:D11};";
        return Encoding.ASCII.GetBytes(comando);
    }

    /// <summary>
    /// Mapea un modo de operación del dominio al código Kenwood correspondiente.
    /// </summary>
    /// <param name="modo">Modo de operación a mapear.</param>
    /// <returns>Código de modo Kenwood como string de un carácter.</returns>
    private static string MapearModoAKenwood(ModoOperacion modo)
    {
        return modo switch
        {
            ModoOperacion.SSB => "2",   // USB por defecto
            ModoOperacion.CW => "3",
            ModoOperacion.FM => "4",
            ModoOperacion.AM => "5",
            ModoOperacion.RTTY => "6",  // FSK
            ModoOperacion.FT8 or ModoOperacion.FT4 or ModoOperacion.PSK or
            ModoOperacion.PKT or ModoOperacion.MFSK or ModoOperacion.OLIVIA or
            ModoOperacion.JT65 or ModoOperacion.JT9 or ModoOperacion.WSPR or
            ModoOperacion.JS8 or ModoOperacion.MSK144 or ModoOperacion.Q65 or
            ModoOperacion.FST4 or ModoOperacion.FST4W => "2", // USB para digitales
            _ => "2" // USB por defecto
        };
    }
}
