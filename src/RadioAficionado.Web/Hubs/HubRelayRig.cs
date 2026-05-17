using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RadioAficionado.Compartido.Contratos;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Hubs;

/// <summary>
/// Hub de SignalR al que se conecta el navegador del usuario.
/// Autenticado con cookie de Identity. Recibe comandos del navegador
/// y los reenvia al servicio local a traves del HubTunelServicio.
/// </summary>
[Authorize]
public class HubRelayRig(
    RegistroServiciosConectados _registro,
    IHubContext<HubTunelServicio, IClienteHubTunel> _hubTunel,
    ILogger<HubRelayRig> _logger) : Hub<IClienteHubRelay>
{
    /// <summary>
    /// Se ejecuta cuando el navegador del usuario se conecta al hub.
    /// Notifica al navegador si su servicio local esta conectado.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            bool servicioConectado = _registro.EstaConectado(usuarioId);

            _logger.LogInformation(
                "Navegador conectado para usuario {UsuarioId}. Servicio local {Estado}. ConnectionId: {ConnectionId}",
                usuarioId,
                servicioConectado ? "conectado" : "desconectado",
                Context.ConnectionId);

            await Clients.Caller.RecibirConexionServicio(servicioConectado);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Recibe un comando del navegador, valida que el usuario sea el propietario
    /// y lo reenvia al servicio local correspondiente.
    /// </summary>
    /// <param name="comando">Comando a enviar al rig.</param>
    /// <exception cref="HubException">Si el usuario no es el propietario del comando o el servicio no esta conectado.</exception>
    public async Task EnviarComando(ComandoRemotoRig comando)
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            throw new HubException("No se pudo identificar al usuario.");
        }

        // Verificar que el comando pertenece al usuario autenticado
        if (!string.Equals(comando.UsuarioId, usuarioId, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Usuario {UsuarioId} intento enviar comando con UsuarioId {ComandoUsuarioId}",
                usuarioId,
                comando.UsuarioId);
            throw new HubException("No tienes permiso para enviar comandos a este rig.");
        }

        string? connectionIdServicio = _registro.ObtenerConnectionId(usuarioId);

        if (string.IsNullOrWhiteSpace(connectionIdServicio))
        {
            _logger.LogWarning(
                "Usuario {UsuarioId} intento enviar comando pero su servicio local no esta conectado",
                usuarioId);
            throw new HubException("Tu servicio local no esta conectado. Inicia RadioAficionado.Servicio en tu equipo.");
        }

        _logger.LogDebug(
            "Reenviando comando {TipoComando} (Id: {ComandoId}) de usuario {UsuarioId} al servicio local",
            comando.Tipo,
            comando.Id,
            usuarioId);

        await _hubTunel.Clients.Client(connectionIdServicio).EjecutarComandoRig(comando);
    }

    /// <summary>
    /// Recibe señalizacion WebRTC del browser y la reenvia al servicio local.
    /// </summary>
    /// <param name="senalizacion">Mensaje de señalizacion WebRTC (SDP offer/answer o ICE candidate).</param>
    public async Task EnviarSenalizacion(SenalizacionWebRtc senalizacion)
    {
        string? usuarioId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            throw new HubException("No se pudo identificar al usuario.");
        }

        string? connectionIdServicio = _registro.ObtenerConnectionId(usuarioId);
        if (string.IsNullOrWhiteSpace(connectionIdServicio))
        {
            throw new HubException("Tu servicio local no esta conectado.");
        }

        await _hubTunel.Clients.Client(connectionIdServicio).RecibirSenalizacion(senalizacion);
    }
}
