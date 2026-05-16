using Microsoft.AspNetCore.SignalR;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Hubs;

/// <summary>
/// Hub de SignalR para estado general: DX Cluster, notificaciones.
/// </summary>
public sealed class HubEstado : Hub<IClienteHubEstado>
{
    private readonly IDxCluster _dxCluster;
    private readonly IHubContext<HubEstado, IClienteHubEstado> _contextoHub;
    private readonly ILogger<HubEstado> _logger;

    private static bool _suscrito;
    private static readonly object _lockSuscripcion = new();

    /// <summary>
    /// Crea el hub de estado.
    /// </summary>
    public HubEstado(
        IDxCluster dxCluster,
        IHubContext<HubEstado, IClienteHubEstado> contextoHub,
        ILogger<HubEstado> logger)
    {
        _dxCluster = dxCluster ?? throw new ArgumentNullException(nameof(dxCluster));
        _contextoHub = contextoHub ?? throw new ArgumentNullException(nameof(contextoHub));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Al conectar, suscribirse al DX Cluster.</summary>
    public override Task OnConnectedAsync()
    {
        SuscribirSiNecesario();
        return base.OnConnectedAsync();
    }

    private void SuscribirSiNecesario()
    {
        lock (_lockSuscripcion)
        {
            if (_suscrito) { return; }

            _dxCluster.SpotRecibido += AlRecibirSpot;
            _suscrito = true;
            _logger.LogInformation("Suscrito al DX Cluster para distribucion SignalR.");
        }
    }

    private void AlRecibirSpot(object? sender, SpotDx spot)
    {
        SpotDxDto dto = new(
            spot.Spotteador.Valor,
            spot.Dx.Valor,
            spot.Frecuencia.Hz,
            spot.Comentario,
            spot.Hora);

        _ = _contextoHub.Clients.All.RecibirSpotDx(dto);
    }
}
