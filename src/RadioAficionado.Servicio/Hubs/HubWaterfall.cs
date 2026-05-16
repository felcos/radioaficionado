using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Servicio.Dtos;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Hubs;

/// <summary>
/// Hub de SignalR para el waterfall.
/// Se suscribe al IServicioWaterfall y emite lineas de espectro comprimidas (byte[])
/// con throttle a 30 fps para no saturar el cliente.
/// </summary>
public sealed class HubWaterfall : Hub<IClienteHubWaterfall>
{
    private readonly ServicioEstadoOperacion _estado;
    private readonly IHubContext<HubWaterfall, IClienteHubWaterfall> _contextoHub;
    private readonly ILogger<HubWaterfall> _logger;

    private static bool _suscrito;
    private static readonly object _lockSuscripcion = new();
    private static long _ultimoEnvioTicks;

    /// <summary>Intervalo minimo entre envios en ticks (30 fps = ~33ms).</summary>
    private static readonly long _intervaloMinimoTicks = TimeSpan.FromMilliseconds(33).Ticks;

    /// <summary>
    /// Crea el hub de waterfall.
    /// </summary>
    public HubWaterfall(
        ServicioEstadoOperacion estado,
        IHubContext<HubWaterfall, IClienteHubWaterfall> contextoHub,
        ILogger<HubWaterfall> logger)
    {
        _estado = estado ?? throw new ArgumentNullException(nameof(estado));
        _contextoHub = contextoHub ?? throw new ArgumentNullException(nameof(contextoHub));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Al conectar el primer cliente, se suscribe al waterfall.</summary>
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

            _estado.ServicioWaterfall.LineaEspectroGenerada += AlRecibirLineaEspectro;
            _suscrito = true;
            _logger.LogInformation("Suscrito al waterfall para distribucion SignalR.");
        }
    }

    private void AlRecibirLineaEspectro(object? sender, LineaEspectroEventArgs e)
    {
        // Throttle a 30 fps
        long ahora = Stopwatch.GetTimestamp();
        if (ahora - Interlocked.Read(ref _ultimoEnvioTicks) < _intervaloMinimoTicks)
        {
            return;
        }
        Interlocked.Exchange(ref _ultimoEnvioTicks, ahora);

        byte[] magnitudesBytes = ServicioEstadoOperacion.ConvertirMagnitudesABytes(e.MagnitudesDb);

        LineaEspectroDto dto = new(
            magnitudesBytes,
            e.ResolucionHz,
            e.FrecuenciaMinHz);

        // Enviar a todos los clientes sin bloquear
        _ = _contextoHub.Clients.All.RecibirLineaEspectro(dto);
    }

}
