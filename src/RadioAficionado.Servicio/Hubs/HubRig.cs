using Microsoft.AspNetCore.SignalR;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Servicio.Dtos;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Hubs;

/// <summary>
/// Hub de SignalR para control del rig.
/// Permite conectar/desconectar, cambiar frecuencia, modo, banda, PTT y VFO.
/// </summary>
public sealed class HubRig : Hub<IClienteHubRig>
{
    private readonly ServicioEstadoOperacion _estado;

    /// <summary>
    /// Crea el hub de rig.
    /// </summary>
    public HubRig(ServicioEstadoOperacion estado)
    {
        _estado = estado ?? throw new ArgumentNullException(nameof(estado));
    }

    /// <summary>Conecta al radio con la configuracion proporcionada.</summary>
    public async Task ConectarRig(ConfiguracionConexionDto config)
    {
        await _estado.ConectarAsync(config);
        await Clients.All.RecibirConexionCambiada(_estado.Conectado, _estado.EstadoConexionDetalle);
        await Clients.All.RecibirEstadoRig(_estado.ObtenerEstadoActual());
    }

    /// <summary>Desconecta del radio.</summary>
    public async Task DesconectarRig()
    {
        await _estado.DesconectarAsync();
        await Clients.All.RecibirConexionCambiada(false, "Desconectado");
    }

    /// <summary>Cambia la frecuencia del radio.</summary>
    public async Task CambiarFrecuencia(long frecuenciaHz)
    {
        await _estado.CambiarFrecuenciaAsync(frecuenciaHz);
    }

    /// <summary>Cambia el modo de operacion.</summary>
    public async Task CambiarModo(string modo)
    {
        await _estado.CambiarModoAsync(modo);
    }

    /// <summary>Cambia la banda.</summary>
    public async Task CambiarBanda(string banda)
    {
        await _estado.CambiarBandaAsync(banda);
    }

    /// <summary>Activa o desactiva el PTT.</summary>
    public async Task CambiarPtt(bool activar)
    {
        await _estado.CambiarPttAsync(activar);
    }

    /// <summary>Cambia el VFO activo.</summary>
    public async Task CambiarVfo()
    {
        await _estado.CambiarVfoAsync();
    }

    /// <summary>Obtiene los puertos serie disponibles.</summary>
    public IReadOnlyList<string> ObtenerPuertos()
    {
        return _estado.ObtenerPuertosDisponibles();
    }

    /// <summary>Obtiene los dispositivos de audio disponibles.</summary>
    public async Task<IReadOnlyList<DispositivoAudioDto>> ObtenerDispositivosAudio()
    {
        IReadOnlyList<DispositivoAudio> dispositivos = await _estado.ObtenerDispositivosAudioAsync();
        List<DispositivoAudioDto> resultado = new(dispositivos.Count);
        foreach (DispositivoAudio dispositivo in dispositivos)
        {
            resultado.Add(new DispositivoAudioDto(
                dispositivo.Id,
                dispositivo.Nombre,
                dispositivo.EsEntrada,
                dispositivo.EsSalida));
        }
        return resultado;
    }

    /// <summary>Obtiene el estado actual del rig.</summary>
    public EstadoRigDto ObtenerEstado()
    {
        return _estado.ObtenerEstadoActual();
    }

    /// <summary>Envia estado inicial al conectar el cliente SignalR.</summary>
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.RecibirEstadoRig(_estado.ObtenerEstadoActual());
        await Clients.Caller.RecibirConexionCambiada(_estado.Conectado, _estado.EstadoConexionDetalle);
        await base.OnConnectedAsync();
    }
}

/// <summary>
/// DTO de dispositivo de audio para enviar al cliente.
/// </summary>
public sealed record DispositivoAudioDto(
    string Id,
    string Nombre,
    bool EsEntrada,
    bool EsSalida);
