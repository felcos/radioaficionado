using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.Rig.Cat;

/// <summary>
/// Protocolo CAT (Computer Aided Transceiver) para comunicación serie con un radio.
/// Cada fabricante tiene su propio protocolo de comandos.
/// </summary>
public interface IProtocoloCat
{
    /// <summary>
    /// Nombre del fabricante del protocolo (ej: "Yaesu", "Icom", "Kenwood").
    /// </summary>
    string NombreFabricante { get; }

    /// <summary>
    /// Genera el comando para leer la frecuencia actual del VFO activo.
    /// </summary>
    byte[] ComandoLeerFrecuencia();

    /// <summary>
    /// Genera el comando para leer el modo de operación actual.
    /// </summary>
    byte[] ComandoLeerModo();

    /// <summary>
    /// Genera el comando para leer el estado del PTT.
    /// </summary>
    byte[] ComandoLeerPtt();

    /// <summary>
    /// Genera el comando para leer el nivel de señal (S-meter).
    /// </summary>
    byte[] ComandoLeerNivelSenal();

    /// <summary>
    /// Genera el comando para cambiar la frecuencia del VFO activo.
    /// </summary>
    /// <param name="frecuenciaHz">Frecuencia objetivo en hercios.</param>
    byte[] ComandoCambiarFrecuencia(long frecuenciaHz);

    /// <summary>
    /// Genera el comando para cambiar el modo de operación.
    /// </summary>
    /// <param name="modo">Modo de operación objetivo.</param>
    byte[] ComandoCambiarModo(ModoOperacion modo);

    /// <summary>
    /// Genera el comando para activar o desactivar el PTT.
    /// </summary>
    /// <param name="activar">True para transmitir, false para recibir.</param>
    byte[] ComandoCambiarPtt(bool activar);

    /// <summary>
    /// Parsea la respuesta del radio para extraer la frecuencia en hercios.
    /// </summary>
    /// <param name="respuesta">Bytes de respuesta del radio.</param>
    long ParsearFrecuencia(byte[] respuesta);

    /// <summary>
    /// Parsea la respuesta del radio para extraer el modo y submodo de operación.
    /// </summary>
    /// <param name="respuesta">Bytes de respuesta del radio.</param>
    (ModoOperacion modo, SubModoOperacion? submodo) ParsearModo(byte[] respuesta);

    /// <summary>
    /// Parsea la respuesta del radio para extraer el estado del PTT.
    /// </summary>
    /// <param name="respuesta">Bytes de respuesta del radio.</param>
    bool ParsearPtt(byte[] respuesta);

    /// <summary>
    /// Parsea la respuesta del radio para extraer el nivel de señal (0-255).
    /// </summary>
    /// <param name="respuesta">Bytes de respuesta del radio.</param>
    int ParsearNivelSenal(byte[] respuesta);

    /// <summary>
    /// Tamaño esperado de la respuesta al comando de lectura de frecuencia.
    /// </summary>
    int TamanoRespuestaFrecuencia { get; }

    /// <summary>
    /// Tamaño esperado de la respuesta al comando de lectura de modo.
    /// </summary>
    int TamanoRespuestaModo { get; }

    /// <summary>
    /// Tamaño esperado de la respuesta al comando de lectura de PTT.
    /// </summary>
    int TamanoRespuestaPtt { get; }

    /// <summary>
    /// Tamaño esperado de la respuesta al comando de lectura de nivel de señal.
    /// </summary>
    int TamanoRespuestaNivelSenal { get; }

    /// <summary>
    /// Tamaño esperado de la respuesta al comando de lectura de estado split.
    /// </summary>
    int TamanoRespuestaSplit { get; }

    /// <summary>
    /// Genera el comando para activar o desactivar el modo split.
    /// </summary>
    /// <param name="activar">True para activar split, false para desactivar.</param>
    byte[] ComandoActivarSplit(bool activar);

    /// <summary>
    /// Genera el comando para leer el estado del modo split.
    /// </summary>
    byte[] ComandoLeerSplit();

    /// <summary>
    /// Parsea la respuesta del radio para extraer el estado del split.
    /// </summary>
    /// <param name="respuesta">Bytes de respuesta del radio.</param>
    bool ParsearSplit(byte[] respuesta);

    /// <summary>
    /// Genera el comando para cambiar la frecuencia del VFO B.
    /// </summary>
    /// <param name="frecuenciaHz">Frecuencia objetivo en hercios para el VFO B.</param>
    byte[] ComandoCambiarFrecuenciaVfoB(long frecuenciaHz);
}
