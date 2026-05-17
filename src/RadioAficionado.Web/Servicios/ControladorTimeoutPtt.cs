using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using RadioAficionado.Compartido.Contratos;
using RadioAficionado.Web.Hubs;

namespace RadioAficionado.Web.Servicios;

/// <summary>
/// Singleton que monitorea el estado del PTT por usuario.
/// Si un PTT lleva mas de 180 segundos activo, envia automaticamente
/// un comando CambiarPtt(false) al servicio local para proteger el equipo.
/// </summary>
public sealed class ControladorTimeoutPtt : IDisposable
{
    private readonly ConcurrentDictionary<string, DateTime> _pttActivos = new();
    private readonly IServiceProvider _proveedor;
    private readonly ILogger<ControladorTimeoutPtt> _logger;
    private readonly Timer _timerVerificacion;
    private bool _disposed;

    private const int TimeoutPttSegundos = 180;
    private const int IntervaloVerificacionSegundos = 5;

    /// <summary>
    /// Crea una nueva instancia del controlador de timeout de PTT.
    /// </summary>
    /// <param name="proveedor">Proveedor de servicios para resolver el hub context.</param>
    /// <param name="logger">Logger para registrar eventos del controlador.</param>
    public ControladorTimeoutPtt(
        IServiceProvider proveedor,
        ILogger<ControladorTimeoutPtt> logger)
    {
        _proveedor = proveedor ?? throw new ArgumentNullException(nameof(proveedor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _timerVerificacion = new Timer(
            VerificarTimeoutsCallback,
            null,
            TimeSpan.FromSeconds(IntervaloVerificacionSegundos),
            TimeSpan.FromSeconds(IntervaloVerificacionSegundos));
    }

    /// <summary>
    /// Registra que un usuario ha activado el PTT.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario.</param>
    public void RegistrarPttActivo(string usuarioId)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return;
        }

        _pttActivos[usuarioId] = DateTime.UtcNow;
        _logger.LogDebug("PTT activo registrado para usuario {UsuarioId}", usuarioId);
    }

    /// <summary>
    /// Registra que un usuario ha desactivado el PTT.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario.</param>
    public void RegistrarPttInactivo(string usuarioId)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return;
        }

        if (_pttActivos.TryRemove(usuarioId, out _))
        {
            _logger.LogDebug("PTT inactivo registrado para usuario {UsuarioId}", usuarioId);
        }
    }

    /// <summary>
    /// Verifica si hay PTTs que han excedido el timeout maximo y los desactiva.
    /// </summary>
    public void VerificarTimeouts()
    {
        DateTime ahora = DateTime.UtcNow;
        List<string> usuariosExcedidos = new();

        foreach (KeyValuePair<string, DateTime> entrada in _pttActivos)
        {
            double segundosActivo = (ahora - entrada.Value).TotalSeconds;
            if (segundosActivo >= TimeoutPttSegundos)
            {
                usuariosExcedidos.Add(entrada.Key);
            }
        }

        foreach (string usuarioId in usuariosExcedidos)
        {
            if (_pttActivos.TryRemove(usuarioId, out DateTime inicio))
            {
                double duracion = (ahora - inicio).TotalSeconds;
                _logger.LogWarning(
                    "PTT timeout para usuario {UsuarioId}: activo durante {Duracion:F1} segundos. " +
                    "Enviando comando para desactivar PTT.",
                    usuarioId,
                    duracion);

                _ = EnviarDesactivarPttAsync(usuarioId);
            }
        }
    }

    /// <summary>
    /// Obtiene la cantidad de PTTs activos actualmente.
    /// </summary>
    public int CantidadPttActivos => _pttActivos.Count;

    /// <summary>
    /// Callback del timer que invoca la verificacion de timeouts.
    /// </summary>
    private void VerificarTimeoutsCallback(object? estado)
    {
        try
        {
            VerificarTimeouts();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar timeouts de PTT");
        }
    }

    /// <summary>
    /// Envia el comando de desactivacion de PTT al servicio local del usuario a traves del hub de tunel.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario cuyo PTT se debe desactivar.</param>
    private async Task EnviarDesactivarPttAsync(string usuarioId)
    {
        try
        {
            using IServiceScope scope = _proveedor.CreateScope();
            IHubContext<HubTunelServicio, IClienteHubTunel> hubTunel =
                scope.ServiceProvider.GetRequiredService<IHubContext<HubTunelServicio, IClienteHubTunel>>();

            RegistroServiciosConectados registro =
                scope.ServiceProvider.GetRequiredService<RegistroServiciosConectados>();

            string? connectionId = registro.ObtenerConnectionId(usuarioId);
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                _logger.LogWarning(
                    "No se pudo enviar desactivacion de PTT para usuario {UsuarioId}: servicio local no conectado.",
                    usuarioId);
                return;
            }

            ComandoRemotoRig comando = ComandoRemotoRig.Crear(
                TipoComandoRig.CambiarPtt,
                usuarioId,
                new Dictionary<string, string> { ["Activar"] = "false" });

            await hubTunel.Clients.Client(connectionId).EjecutarComandoRig(comando);
            _logger.LogInformation(
                "Comando de desactivacion de PTT enviado al servicio local del usuario {UsuarioId} por timeout.",
                usuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar comando de desactivacion de PTT por timeout para usuario {UsuarioId}", usuarioId);
        }
    }

    /// <summary>
    /// Libera los recursos del controlador (timer de verificacion).
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _timerVerificacion.Dispose();
        _disposed = true;
    }
}
