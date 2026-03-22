namespace RadioAficionado.Nativo.Rotador;

/// <summary>
/// Configuración de conexión y comportamiento del cliente rotctld.
/// </summary>
public sealed class ConfiguracionRotador
{
    /// <summary>Host donde escucha el demonio rotctld.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Puerto TCP del demonio rotctld (por defecto 4533).</summary>
    public int Puerto { get; set; } = 4533;

    /// <summary>Intervalo en milisegundos entre cada lectura de posición del rotador.</summary>
    public int IntervaloPollingMs { get; set; } = 1000;

    /// <summary>
    /// Umbral mínimo en grados para considerar que la posición cambió y disparar el evento.
    /// Evita notificaciones por fluctuaciones menores del rotador.
    /// </summary>
    public double UmbralCambioGrados { get; set; } = 0.5;

    /// <summary>Tiempo máximo de espera en milisegundos para una respuesta del demonio rotctld.</summary>
    public int TimeoutMs { get; set; } = 5000;
}
