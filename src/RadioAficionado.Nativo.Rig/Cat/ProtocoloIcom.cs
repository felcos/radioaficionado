using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.Rig.Cat;

/// <summary>
/// Protocolo Icom CI-V para radios IC-7300, IC-705, IC-7100, IC-9700.
/// Comandos binarios con preámbulo FE FE, direcciones y terminador FD.
/// </summary>
public sealed class ProtocoloIcom : IProtocoloCat
{
    /// <summary>Preámbulo de todo frame CI-V.</summary>
    private const byte Preambulo = 0xFE;

    /// <summary>Terminador de todo frame CI-V.</summary>
    private const byte Terminador = 0xFD;

    /// <summary>Dirección por defecto del PC (controlador).</summary>
    private const byte DireccionPc = 0xE0;

    private readonly byte _direccionRadio;

    /// <inheritdoc />
    public string NombreFabricante => "Icom";

    /// <inheritdoc />
    public int TamanoRespuestaFrecuencia => 11; // FE FE E0 94 03 [5 bytes BCD] FD

    /// <inheritdoc />
    public int TamanoRespuestaModo => 8; // FE FE E0 94 04 [modo] [filtro] FD

    /// <inheritdoc />
    public int TamanoRespuestaPtt => 8; // FE FE E0 94 1C 00 [estado] FD

    /// <inheritdoc />
    public int TamanoRespuestaNivelSenal => 9; // FE FE E0 94 15 02 [MSB] [LSB] FD

    /// <inheritdoc />
    public int TamanoRespuestaSplit => 7; // FE FE E0 94 0F [estado] FD

    /// <summary>
    /// Crea una nueva instancia del protocolo Icom CI-V.
    /// </summary>
    /// <param name="direccionRadio">Dirección CI-V del radio (ej: 0x94 para IC-7300, 0xA4 para IC-705).</param>
    public ProtocoloIcom(byte direccionRadio = 0x94)
    {
        _direccionRadio = direccionRadio;
    }

    /// <inheritdoc />
    public byte[] ComandoLeerFrecuencia()
    {
        return ConstruirFrame(new byte[] { 0x03 });
    }

    /// <inheritdoc />
    public byte[] ComandoCambiarFrecuencia(long frecuenciaHz)
    {
        byte[] bcd = FrecuenciaABcdInvertido(frecuenciaHz);
        byte[] datos = new byte[6];
        datos[0] = 0x05;
        Array.Copy(bcd, 0, datos, 1, 5);
        return ConstruirFrame(datos);
    }

    /// <inheritdoc />
    public byte[] ComandoLeerModo()
    {
        return ConstruirFrame(new byte[] { 0x04 });
    }

    /// <inheritdoc />
    public byte[] ComandoCambiarModo(ModoOperacion modo)
    {
        byte codigoModo = MapearModoAIcom(modo);
        byte filtro = 0x01; // Filtro 1 por defecto
        return ConstruirFrame(new byte[] { 0x06, codigoModo, filtro });
    }

    /// <inheritdoc />
    public byte[] ComandoLeerPtt()
    {
        return ConstruirFrame(new byte[] { 0x1C, 0x00 });
    }

    /// <inheritdoc />
    public byte[] ComandoCambiarPtt(bool activar)
    {
        byte estado = activar ? (byte)0x01 : (byte)0x00;
        return ConstruirFrame(new byte[] { 0x1C, 0x00, estado });
    }

    /// <inheritdoc />
    public byte[] ComandoLeerNivelSenal()
    {
        return ConstruirFrame(new byte[] { 0x15, 0x02 });
    }

    /// <inheritdoc />
    public long ParsearFrecuencia(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaFrecuencia)
        {
            throw new InvalidOperationException(
                $"Respuesta de frecuencia Icom inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaFrecuencia}.");
        }

        VerificarFrame(respuesta);

        // Los bytes de frecuencia BCD invertido empiezan en posición 5 (después de FE FE dir dir cmd)
        byte[] bcd = new byte[5];
        Array.Copy(respuesta, 5, bcd, 0, 5);

        return BcdInvertidoAFrecuencia(bcd);
    }

    /// <inheritdoc />
    public (ModoOperacion modo, SubModoOperacion? submodo) ParsearModo(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaModo)
        {
            throw new InvalidOperationException(
                $"Respuesta de modo Icom inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaModo}.");
        }

        VerificarFrame(respuesta);

        // Byte de modo en posición 5 (después de FE FE dir dir cmd)
        byte codigoModo = respuesta[5];

        return codigoModo switch
        {
            0x00 => (ModoOperacion.SSB, SubModoOperacion.LSB),
            0x01 => (ModoOperacion.SSB, SubModoOperacion.USB),
            0x03 => (ModoOperacion.CW, null),
            0x04 => (ModoOperacion.RTTY, null),
            0x05 => (ModoOperacion.AM, null),
            0x06 => (ModoOperacion.FM, null),
            0x08 => (ModoOperacion.FT8, null), // DATA mode
            _ => (ModoOperacion.SSB, SubModoOperacion.USB)
        };
    }

    /// <inheritdoc />
    public bool ParsearPtt(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaPtt)
        {
            throw new InvalidOperationException(
                $"Respuesta de PTT Icom inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaPtt}.");
        }

        VerificarFrame(respuesta);

        // Estado PTT en posición 6 (después de FE FE dir dir 1C 00)
        return respuesta[6] != 0x00;
    }

    /// <inheritdoc />
    public int ParsearNivelSenal(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaNivelSenal)
        {
            throw new InvalidOperationException(
                $"Respuesta de S-meter Icom inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaNivelSenal}.");
        }

        VerificarFrame(respuesta);

        // Nivel de señal en bytes 6 y 7 (MSB, LSB) — valor 0-255
        int nivel = (respuesta[6] << 8) | respuesta[7];
        return Math.Clamp(nivel, 0, 255);
    }

    /// <inheritdoc />
    public byte[] ComandoActivarSplit(bool activar)
    {
        // Comando 0x0F: Split on/off
        byte estado = activar ? (byte)0x01 : (byte)0x00;
        return ConstruirFrame(new byte[] { 0x0F, estado });
    }

    /// <inheritdoc />
    public byte[] ComandoLeerSplit()
    {
        return ConstruirFrame(new byte[] { 0x0F });
    }

    /// <inheritdoc />
    public bool ParsearSplit(byte[] respuesta)
    {
        if (respuesta is null || respuesta.Length < TamanoRespuestaSplit)
        {
            throw new InvalidOperationException(
                $"Respuesta de split Icom inválida: longitud {respuesta?.Length ?? 0}, esperada {TamanoRespuestaSplit}.");
        }

        VerificarFrame(respuesta);

        // Estado split en posición 5 (después de FE FE dir dir 0F)
        return respuesta[5] != 0x00;
    }

    /// <inheritdoc />
    public byte[] ComandoCambiarFrecuenciaVfoB(long frecuenciaHz)
    {
        // Seleccionar VFO B (0x07 0x01), cambiar frecuencia (0x05), volver a VFO A (0x07 0x00)
        // Para simplificar, usamos el comando 0x05 con selección previa de VFO B
        // En la práctica el ClienteCatSerial enviará la secuencia completa
        byte[] bcd = FrecuenciaABcdInvertido(frecuenciaHz);
        byte[] datos = new byte[6];
        datos[0] = 0x05;
        Array.Copy(bcd, 0, datos, 1, 5);
        return ConstruirFrame(datos);
    }

    /// <summary>
    /// Construye un frame CI-V completo con preámbulo, direcciones, datos y terminador.
    /// </summary>
    /// <param name="datos">Bytes de comando y datos a enviar.</param>
    /// <returns>Frame CI-V completo listo para enviar por puerto serie.</returns>
    public byte[] ConstruirFrame(byte[] datos)
    {
        // Formato: FE FE [dir radio] [dir PC] [datos...] FD
        byte[] frame = new byte[4 + datos.Length + 1];
        frame[0] = Preambulo;
        frame[1] = Preambulo;
        frame[2] = _direccionRadio;
        frame[3] = DireccionPc;
        Array.Copy(datos, 0, frame, 4, datos.Length);
        frame[^1] = Terminador;
        return frame;
    }

    /// <summary>
    /// Convierte una frecuencia en Hz a formato BCD invertido de 5 bytes (formato Icom).
    /// </summary>
    /// <param name="frecuenciaHz">Frecuencia en hercios.</param>
    /// <returns>Array de 5 bytes en formato BCD invertido.</returns>
    private static byte[] FrecuenciaABcdInvertido(long frecuenciaHz)
    {
        byte[] bcd = new byte[5];

        for (int i = 0; i < 5; i++)
        {
            int digitoBajo = (int)(frecuenciaHz % 10);
            frecuenciaHz /= 10;
            int digitoAlto = (int)(frecuenciaHz % 10);
            frecuenciaHz /= 10;
            bcd[i] = (byte)((digitoAlto << 4) | digitoBajo);
        }

        return bcd;
    }

    /// <summary>
    /// Convierte un array BCD invertido de 5 bytes a frecuencia en Hz.
    /// </summary>
    /// <param name="bcd">Array de 5 bytes en formato BCD invertido.</param>
    /// <returns>Frecuencia en hercios.</returns>
    private static long BcdInvertidoAFrecuencia(byte[] bcd)
    {
        long frecuencia = 0;
        long multiplicador = 1;

        for (int i = 0; i < 5; i++)
        {
            int digitoBajo = bcd[i] & 0x0F;
            int digitoAlto = (bcd[i] >> 4) & 0x0F;
            frecuencia += digitoBajo * multiplicador;
            multiplicador *= 10;
            frecuencia += digitoAlto * multiplicador;
            multiplicador *= 10;
        }

        return frecuencia;
    }

    /// <summary>
    /// Verifica que un frame CI-V tenga el formato correcto (preámbulo y terminador).
    /// </summary>
    /// <param name="frame">Frame CI-V a verificar.</param>
    /// <exception cref="InvalidOperationException">Si el frame no tiene el formato correcto.</exception>
    private static void VerificarFrame(byte[] frame)
    {
        if (frame[0] != Preambulo || frame[1] != Preambulo || frame[^1] != Terminador)
        {
            throw new InvalidOperationException("Frame CI-V inválido: preámbulo o terminador incorrecto.");
        }
    }

    /// <summary>
    /// Mapea un modo de operación del dominio al código Icom correspondiente.
    /// </summary>
    /// <param name="modo">Modo de operación a mapear.</param>
    /// <returns>Código de modo Icom.</returns>
    private static byte MapearModoAIcom(ModoOperacion modo)
    {
        return modo switch
        {
            ModoOperacion.SSB => 0x01,  // USB por defecto
            ModoOperacion.CW => 0x03,
            ModoOperacion.RTTY => 0x04,
            ModoOperacion.AM => 0x05,
            ModoOperacion.FM => 0x06,
            ModoOperacion.FT8 or ModoOperacion.FT4 or ModoOperacion.PSK or
            ModoOperacion.PKT or ModoOperacion.MFSK or ModoOperacion.OLIVIA or
            ModoOperacion.JT65 or ModoOperacion.JT9 or ModoOperacion.WSPR or
            ModoOperacion.JS8 or ModoOperacion.MSK144 or ModoOperacion.Q65 or
            ModoOperacion.FST4 or ModoOperacion.FST4W => 0x08, // DATA
            _ => 0x01 // USB por defecto
        };
    }
}
