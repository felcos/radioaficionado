using Microsoft.AspNetCore.SignalR;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Hubs;

/// <summary>
/// Hub de SignalR para decodificaciones digitales.
/// Se suscribe a los decodificadores activos y emite MensajeDecodificadoDto con colores.
/// </summary>
public sealed class HubDecodificaciones : Hub<IClienteHubDecodificaciones>
{
    private readonly IRegistroDecodificadores _registroDecodificadores;
    private readonly IHubContext<HubDecodificaciones, IClienteHubDecodificaciones> _contextoHub;
    private readonly ILogger<HubDecodificaciones> _logger;

    private static bool _suscrito;
    private static readonly object _lockSuscripcion = new();

    /// <summary>
    /// Crea el hub de decodificaciones.
    /// </summary>
    public HubDecodificaciones(
        IRegistroDecodificadores registroDecodificadores,
        IHubContext<HubDecodificaciones, IClienteHubDecodificaciones> contextoHub,
        ILogger<HubDecodificaciones> logger)
    {
        _registroDecodificadores = registroDecodificadores ?? throw new ArgumentNullException(nameof(registroDecodificadores));
        _contextoHub = contextoHub ?? throw new ArgumentNullException(nameof(contextoHub));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Al conectar, suscribirse a los decodificadores.</summary>
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

            foreach (IDecodificadorDigital decodificador in _registroDecodificadores.ObtenerTodos())
            {
                decodificador.MensajeDecodificadoRecibido += AlRecibirMensajeDecodificado;
            }

            _suscrito = true;
            _logger.LogInformation("Suscrito a {Cantidad} decodificadores para distribucion SignalR.",
                _registroDecodificadores.ObtenerTodos().Count);
        }
    }

    private void AlRecibirMensajeDecodificado(object? sender, MensajeDecodificado mensaje)
    {
        // Color por defecto — el ColoreadorIndicativos se integrara en Fase C
        string color = DeterminarColorBasico(mensaje);

        MensajeDecodificadoDto dto = new(
            mensaje.MarcaDeTiempo,
            mensaje.FrecuenciaAudioHz,
            mensaje.Snr,
            mensaje.DeltaTiempo,
            mensaje.Modo.ToString(),
            mensaje.Texto,
            mensaje.IndicativoEmisor,
            mensaje.IndicativoDestinatario,
            mensaje.Localizador,
            mensaje.ReporteSenal,
            color);

        _ = _contextoHub.Clients.All.RecibirMensaje(dto);
    }

    /// <summary>Determina un color basico segun el tipo de mensaje.</summary>
    private static string DeterminarColorBasico(MensajeDecodificado mensaje)
    {
        if (mensaje.Texto.StartsWith("CQ ", StringComparison.OrdinalIgnoreCase))
        {
            return "#ff4444"; // Rojo para CQ
        }

        return "#ffffff"; // Blanco por defecto
    }
}
