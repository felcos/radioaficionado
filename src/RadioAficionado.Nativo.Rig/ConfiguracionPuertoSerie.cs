using System.IO.Ports;

namespace RadioAficionado.Nativo.Rig;

/// <summary>
/// Configuración para la conexión CAT directa por puerto serie con un radio.
/// </summary>
public sealed class ConfiguracionPuertoSerie
{
    /// <summary>
    /// Nombre del puerto serie (ej: "COM3", "/dev/ttyUSB0").
    /// </summary>
    public string PuertoSerie { get; set; } = "COM3";

    /// <summary>
    /// Velocidad de baudios del puerto serie.
    /// Opciones comunes: 4800, 9600, 19200, 38400, 57600, 115200.
    /// </summary>
    public int VelocidadBaudios { get; set; } = 38400;

    /// <summary>
    /// Número de bits de datos (normalmente 8).
    /// </summary>
    public int BitsDeDatos { get; set; } = 8;

    /// <summary>
    /// Paridad del puerto serie.
    /// </summary>
    public Parity Paridad { get; set; } = Parity.None;

    /// <summary>
    /// Bits de parada del puerto serie.
    /// </summary>
    public StopBits BitsDeParada { get; set; } = StopBits.One;

    /// <summary>
    /// Habilitar señal RTS (Request To Send).
    /// </summary>
    public bool RtsEnable { get; set; } = true;

    /// <summary>
    /// Habilitar señal DTR (Data Terminal Ready).
    /// </summary>
    public bool DtrEnable { get; set; } = false;

    /// <summary>
    /// Modelo de radio para seleccionar el protocolo CAT adecuado.
    /// </summary>
    public ModeloRadio Modelo { get; set; } = ModeloRadio.Automatico;

    /// <summary>
    /// Timeout de lectura del puerto serie en milisegundos.
    /// </summary>
    public int TimeoutLecturaMs { get; set; } = 1000;

    /// <summary>
    /// Timeout de escritura del puerto serie en milisegundos.
    /// </summary>
    public int TimeoutEscrituraMs { get; set; } = 1000;

    /// <summary>
    /// Intervalo entre cada lectura de estado del radio en milisegundos.
    /// </summary>
    public int IntervaloPollingMs { get; set; } = 200;
}
