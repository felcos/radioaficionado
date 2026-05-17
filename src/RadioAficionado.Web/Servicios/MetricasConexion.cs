using System.Threading;

namespace RadioAficionado.Web.Servicios;

/// <summary>
/// Singleton que registra metricas basicas de conexion y uso del sistema de relay.
/// Mantiene contadores atomicos de servicios conectados, browsers conectados,
/// comandos ejecutados y errores.
/// </summary>
public sealed class MetricasConexion
{
    private long _serviciosConectados;
    private long _browsersConectados;
    private long _comandosEjecutados;
    private long _errores;
    private readonly DateTime _inicioServicio = DateTime.UtcNow;

    /// <summary>Cantidad actual de servicios locales conectados.</summary>
    public long ServiciosConectados => Interlocked.Read(ref _serviciosConectados);

    /// <summary>Cantidad actual de browsers conectados.</summary>
    public long BrowsersConectados => Interlocked.Read(ref _browsersConectados);

    /// <summary>Total de comandos ejecutados desde el inicio del servicio.</summary>
    public long ComandosEjecutados => Interlocked.Read(ref _comandosEjecutados);

    /// <summary>Total de errores registrados desde el inicio del servicio.</summary>
    public long Errores => Interlocked.Read(ref _errores);

    /// <summary>Fecha y hora UTC de inicio del servicio.</summary>
    public DateTime InicioServicio => _inicioServicio;

    /// <summary>
    /// Registra que un servicio local se ha conectado.
    /// </summary>
    public void RegistrarServicioConectado()
    {
        Interlocked.Increment(ref _serviciosConectados);
    }

    /// <summary>
    /// Registra que un servicio local se ha desconectado.
    /// </summary>
    public void RegistrarServicioDesconectado()
    {
        Interlocked.Decrement(ref _serviciosConectados);
    }

    /// <summary>
    /// Registra que un browser se ha conectado.
    /// </summary>
    public void RegistrarBrowserConectado()
    {
        Interlocked.Increment(ref _browsersConectados);
    }

    /// <summary>
    /// Registra que un browser se ha desconectado.
    /// </summary>
    public void RegistrarBrowserDesconectado()
    {
        Interlocked.Decrement(ref _browsersConectados);
    }

    /// <summary>
    /// Registra que se ha ejecutado un comando (relay de browser a servicio).
    /// </summary>
    public void RegistrarComandoEjecutado()
    {
        Interlocked.Increment(ref _comandosEjecutados);
    }

    /// <summary>
    /// Registra que se ha producido un error.
    /// </summary>
    public void RegistrarError()
    {
        Interlocked.Increment(ref _errores);
    }

    /// <summary>
    /// Obtiene un snapshot de las metricas actuales como objeto anonimo serializable.
    /// </summary>
    /// <returns>Objeto con todas las metricas actuales.</returns>
    public object ObtenerSnapshot()
    {
        return new
        {
            serviciosConectados = ServiciosConectados,
            browsersConectados = BrowsersConectados,
            comandosEjecutados = ComandosEjecutados,
            errores = Errores,
            inicioServicio = InicioServicio.ToString("o"),
            tiempoActivoSegundos = (long)(DateTime.UtcNow - _inicioServicio).TotalSeconds
        };
    }
}
