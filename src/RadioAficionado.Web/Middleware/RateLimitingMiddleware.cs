using System.Collections.Concurrent;
using System.Net;

namespace RadioAficionado.Web.Middleware;

/// <summary>
/// Middleware de rate limiting por usuario para rutas de hubs SignalR.
/// Limita a 20 requests por segundo por usuario autenticado.
/// Usa un ConcurrentDictionary en memoria con limpieza periodica de entradas viejas.
/// </summary>
public sealed class RateLimitingMiddleware : IDisposable
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RegistroRateLimit> _registros = new();
    private readonly Timer _timerLimpieza;

    private const int LimitePorSegundo = 20;
    private const int IntervaloLimpiezaSegundos = 60;

    /// <summary>
    /// Crea una nueva instancia del middleware de rate limiting.
    /// </summary>
    /// <param name="siguiente">Siguiente delegado en el pipeline.</param>
    /// <param name="logger">Logger para registrar eventos de rate limiting.</param>
    public RateLimitingMiddleware(
        RequestDelegate siguiente,
        ILogger<RateLimitingMiddleware> logger)
    {
        _siguiente = siguiente ?? throw new ArgumentNullException(nameof(siguiente));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _timerLimpieza = new Timer(
            LimpiarEntradasViejas,
            null,
            TimeSpan.FromSeconds(IntervaloLimpiezaSegundos),
            TimeSpan.FromSeconds(IntervaloLimpiezaSegundos));
    }

    /// <summary>
    /// Procesa la solicitud HTTP aplicando rate limiting a rutas de hubs.
    /// </summary>
    /// <param name="contexto">Contexto HTTP de la solicitud.</param>
    public async Task InvokeAsync(HttpContext contexto)
    {
        string ruta = contexto.Request.Path.Value ?? string.Empty;

        // Solo aplicar rate limiting a rutas de hubs
        if (!ruta.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase))
        {
            await _siguiente(contexto);
            return;
        }

        string claveUsuario = ObtenerClaveUsuario(contexto);
        DateTime ahora = DateTime.UtcNow;

        RegistroRateLimit registro = _registros.GetOrAdd(claveUsuario, _ => new RegistroRateLimit());

        lock (registro)
        {
            // Si la ventana actual expiro, reiniciar
            if ((ahora - registro.InicioVentana).TotalSeconds >= 1.0)
            {
                registro.Contador = 0;
                registro.InicioVentana = ahora;
            }

            registro.Contador++;

            if (registro.Contador > LimitePorSegundo)
            {
                _logger.LogWarning(
                    "Rate limit excedido para usuario {Usuario}: {Contador} requests en la ventana actual",
                    claveUsuario,
                    registro.Contador);

                contexto.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                contexto.Response.Headers.Append("Retry-After", "1");
                return;
            }
        }

        await _siguiente(contexto);
    }

    /// <summary>
    /// Obtiene la clave de identificacion del usuario para el rate limiting.
    /// Usa el nombre de usuario si esta autenticado, o la IP remota si no.
    /// </summary>
    private static string ObtenerClaveUsuario(HttpContext contexto)
    {
        if (contexto.User.Identity?.IsAuthenticated == true)
        {
            return contexto.User.Identity.Name ?? "anonimo";
        }

        return contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido";
    }

    /// <summary>
    /// Limpia entradas del diccionario que llevan mas de 60 segundos sin actividad.
    /// </summary>
    private void LimpiarEntradasViejas(object? estado)
    {
        DateTime limite = DateTime.UtcNow.AddSeconds(-IntervaloLimpiezaSegundos);
        int eliminadas = 0;

        foreach (KeyValuePair<string, RegistroRateLimit> entrada in _registros)
        {
            if (entrada.Value.InicioVentana < limite)
            {
                if (_registros.TryRemove(entrada.Key, out _))
                {
                    eliminadas++;
                }
            }
        }

        if (eliminadas > 0)
        {
            _logger.LogDebug("Limpieza de rate limiting: {Eliminadas} entradas viejas eliminadas", eliminadas);
        }
    }

    /// <summary>
    /// Libera los recursos del middleware (timer de limpieza).
    /// </summary>
    public void Dispose()
    {
        _timerLimpieza.Dispose();
    }

    /// <summary>
    /// Registro interno de rate limiting por usuario.
    /// </summary>
    private sealed class RegistroRateLimit
    {
        /// <summary>Cantidad de requests en la ventana actual.</summary>
        public int Contador { get; set; }

        /// <summary>Inicio de la ventana de tiempo actual.</summary>
        public DateTime InicioVentana { get; set; } = DateTime.UtcNow;
    }
}
