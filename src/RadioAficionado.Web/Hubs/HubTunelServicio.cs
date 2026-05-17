using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RadioAficionado.Compartido.Contratos;
using RadioAficionado.Web.Autenticacion;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Hubs;

/// <summary>
/// Hub de SignalR por el que se conecta el servicio local (RadioAficionado.Servicio).
/// Autenticado con clave de API. Recibe estado del rig y respuestas de comandos
/// desde el servicio y los reenvia al navegador del usuario via HubRelayRig.
/// </summary>
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.NombreEsquema)]
public class HubTunelServicio(
    RegistroServiciosConectados _registro,
    IHubContext<HubRelayRig, IClienteHubRelay> _hubRelay,
    ILogger<HubTunelServicio> _logger) : Hub<IClienteHubTunel>
{
    /// <summary>Ultimo envio de linea de espectro en ticks para throttle a 10fps.</summary>
    private static long _ultimoEnvioEspectroTicks;

    /// <summary>
    /// Se ejecuta cuando el servicio local se conecta al hub.
    /// Registra la conexion en el registro de servicios y notifica al navegador.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            _logger.LogWarning("Conexion al hub de tunel sin UsuarioId valido. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        _registro.Registrar(usuarioId, Context.ConnectionId);

        _logger.LogInformation(
            "Servicio local conectado para usuario {UsuarioId}. ConnectionId: {ConnectionId}",
            usuarioId,
            Context.ConnectionId);

        // Notificar al navegador del usuario que su servicio se conecto
        await _hubRelay.Clients.User(usuarioId).RecibirConexionServicio(true);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Se ejecuta cuando el servicio local se desconecta del hub.
    /// Elimina el registro y notifica al navegador.
    /// </summary>
    /// <param name="exception">Excepcion que causo la desconexion, null si fue normal.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            _registro.Eliminar(usuarioId);

            _logger.LogInformation(
                "Servicio local desconectado para usuario {UsuarioId}. ConnectionId: {ConnectionId}",
                usuarioId,
                Context.ConnectionId);

            // Notificar al navegador del usuario que su servicio se desconecto
            await _hubRelay.Clients.User(usuarioId).RecibirConexionServicio(false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Recibe el estado actual del rig desde el servicio local y lo reenvia al navegador.
    /// </summary>
    /// <param name="estado">Estado actual del rig.</param>
    public async Task ReportarEstadoRig(EstadoRigRemotoDto estado)
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return;
        }

        await _hubRelay.Clients.User(usuarioId).RecibirEstadoRig(estado);
    }

    /// <summary>
    /// Recibe la respuesta de un comando desde el servicio local y la reenvia al navegador.
    /// </summary>
    /// <param name="respuesta">Respuesta del comando ejecutado.</param>
    public async Task ReportarRespuesta(RespuestaRemotoRig respuesta)
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return;
        }

        await _hubRelay.Clients.User(usuarioId).RecibirRespuestaComando(respuesta);
    }

    /// <summary>
    /// Recibe una linea de espectro (waterfall) desde el servicio local y la reenvia al browser.
    /// Aplica throttle a 10fps (100ms) para no saturar el relay.
    /// </summary>
    /// <param name="linea">Linea de espectro comprimida.</param>
    public async Task ReportarLineaEspectro(LineaEspectroRemotaDto linea)
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(usuarioId)) { return; }

        // Throttle a 10fps
        long ahora = Stopwatch.GetTimestamp();
        long ultimo = Interlocked.Read(ref _ultimoEnvioEspectroTicks);
        long intervaloMinimo = Stopwatch.Frequency / 10;

        if (ahora - ultimo < intervaloMinimo) { return; }

        Interlocked.Exchange(ref _ultimoEnvioEspectroTicks, ahora);
        await _hubRelay.Clients.User(usuarioId).RecibirLineaEspectro(linea);
    }

    /// <summary>
    /// Recibe un mensaje decodificado desde el servicio local y lo reenvia al browser.
    /// </summary>
    /// <param name="mensaje">Mensaje decodificado.</param>
    public async Task ReportarMensajeDecodificado(MensajeDecodificadoRemotoDto mensaje)
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(usuarioId)) { return; }

        await _hubRelay.Clients.User(usuarioId).RecibirMensajeDecodificado(mensaje);
    }

    /// <summary>
    /// Recibe señalizacion WebRTC desde el servicio local y la reenvia al browser.
    /// </summary>
    /// <param name="senalizacion">Mensaje de señalizacion WebRTC.</param>
    public async Task ReportarSenalizacion(SenalizacionWebRtc senalizacion)
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(usuarioId)) { return; }

        await _hubRelay.Clients.User(usuarioId).RecibirSenalizacion(senalizacion);
    }
}
