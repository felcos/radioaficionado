using System.IO.Ports;
using System.Text.Json;
using System.Text.Json.Serialization;
using RadioAficionado.Nativo.Rig;

namespace RadioAficionado.Escritorio;

/// <summary>
/// Configuracion de conexion del rig persistida en disco como JSON.
/// Se guarda automaticamente al cambiar cualquier parametro.
/// </summary>
public sealed class ConfiguracionConexionRig
{
    private static readonly string _rutaArchivo = Path.Combine(
        AppContext.BaseDirectory, "configuracion-rig.json");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Usa CAT serial (true) o rigctld TCP (false).</summary>
    public bool UsarCatSerial { get; set; } = true;

    // --- CAT Serial ---
    /// <summary>Puerto serie (ej: "COM3").</summary>
    public string PuertoSerie { get; set; } = "";

    /// <summary>Velocidad de baudios.</summary>
    public int VelocidadBaudios { get; set; } = 38400;

    /// <summary>Modelo de radio.</summary>
    public ModeloRadio ModeloRadio { get; set; } = ModeloRadio.Automatico;

    /// <summary>Bits de datos.</summary>
    public int BitsDeDatos { get; set; } = 8;

    /// <summary>Bits de parada.</summary>
    public StopBits BitsDeParada { get; set; } = StopBits.One;

    /// <summary>Paridad.</summary>
    public Parity Paridad { get; set; } = Parity.None;

    /// <summary>RTS habilitado.</summary>
    public bool RtsEnable { get; set; } = true;

    /// <summary>DTR habilitado.</summary>
    public bool DtrEnable { get; set; } = false;

    /// <summary>Metodo de PTT (CAT, DTR, RTS, VOX).</summary>
    public string MetodoPtt { get; set; } = "CAT";

    /// <summary>Intervalo de polling en ms.</summary>
    public int IntervaloPollingMs { get; set; } = 200;

    // --- rigctld TCP ---
    /// <summary>Host de rigctld.</summary>
    public string HostRigctld { get; set; } = "localhost";

    /// <summary>Puerto de rigctld.</summary>
    public int PuertoRigctld { get; set; } = 4532;

    // --- Audio ---
    /// <summary>ID del dispositivo de audio de entrada (ej: "in:0" para el radio USB).</summary>
    public string DispositivoAudioEntrada { get; set; } = "";

    /// <summary>Tasa de muestreo en Hz (12000 para FT8, 48000 para waterfall).</summary>
    public int TasaDeMuestreoHz { get; set; } = 48000;

    /// <summary>
    /// Carga la configuracion desde disco. Si no existe, devuelve valores por defecto.
    /// </summary>
    public static ConfiguracionConexionRig Cargar()
    {
        try
        {
            if (File.Exists(_rutaArchivo))
            {
                string json = File.ReadAllText(_rutaArchivo);
                ConfiguracionConexionRig? config = JsonSerializer.Deserialize<ConfiguracionConexionRig>(json, _jsonOptions);
                return config ?? new ConfiguracionConexionRig();
            }
        }
        catch
        {
            // Si falla la lectura, devolver valores por defecto.
        }

        return new ConfiguracionConexionRig();
    }

    /// <summary>
    /// Guarda la configuracion actual en disco como JSON.
    /// </summary>
    public void Guardar()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, _jsonOptions);
            File.WriteAllText(_rutaArchivo, json);
        }
        catch
        {
            // Fallo silencioso — no bloquear la app por un error de persistencia.
        }
    }
}
