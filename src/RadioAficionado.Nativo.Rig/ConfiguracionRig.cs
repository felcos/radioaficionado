namespace RadioAficionado.Nativo.Rig;

/// <summary>
/// Configuración de conexión y comportamiento del cliente rigctld.
/// </summary>
public sealed class ConfiguracionRig
{
    /// <summary>Host donde escucha el demonio rigctld.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Puerto TCP del demonio rigctld (por defecto 4532).</summary>
    public int Puerto { get; set; } = 4532;

    /// <summary>Intervalo en milisegundos entre cada lectura de estado del radio.</summary>
    public int IntervaloPollingMs { get; set; } = 500;

    /// <summary>Potencia máxima del radio en vatios, usada para convertir el nivel RFPOWER (0.0-1.0) a vatios reales.</summary>
    public double PotenciaMaximaVatios { get; set; } = 100.0;

    /// <summary>Tiempo máximo de espera en milisegundos para una respuesta del demonio rigctld.</summary>
    public int TimeoutMs { get; set; } = 5000;
}
