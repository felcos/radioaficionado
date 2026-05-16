using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Servicio.Protocolo;

namespace RadioAficionado.Servicio.Servicios;

/// <summary>
/// Servidor UDP compatible con el protocolo WSJT-X.
/// Emite mensajes Heartbeat, Status, Decode y QSOLogged para que
/// aplicaciones como JTAlert, GridTracker y N1MM+ puedan conectarse.
/// Escucha mensajes HaltTx, Clear y HighlightCallsign.
/// </summary>
public sealed class ServidorUdpWsjtx : BackgroundService
{
    private readonly ServicioEstadoOperacion _estadoOperacion;
    private readonly IServicioOperacionDigital _operacionDigital;
    private readonly IRegistroDecodificadores _registroDecodificadores;
    private readonly ILogger<ServidorUdpWsjtx> _logger;

    private UdpClient? _udpCliente;
    private IPEndPoint? _endpointDestino;
    private PeriodicTimer? _timerHeartbeat;
    private bool _suscrito;

    /// <summary>Puerto UDP en el que escucha/emite.</summary>
    public int Puerto { get; }

    /// <summary>
    /// Crea el servidor UDP WSJT-X.
    /// </summary>
    public ServidorUdpWsjtx(
        ServicioEstadoOperacion estadoOperacion,
        IServicioOperacionDigital operacionDigital,
        IRegistroDecodificadores registroDecodificadores,
        ILogger<ServidorUdpWsjtx> logger,
        int puerto = ProtocoloWsjtx.PuertoDefecto)
    {
        _estadoOperacion = estadoOperacion ?? throw new ArgumentNullException(nameof(estadoOperacion));
        _operacionDigital = operacionDigital ?? throw new ArgumentNullException(nameof(operacionDigital));
        _registroDecodificadores = registroDecodificadores ?? throw new ArgumentNullException(nameof(registroDecodificadores));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Puerto = puerto;
    }

    /// <summary>Inicia el servidor UDP y el timer de heartbeat.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _udpCliente = new UdpClient(Puerto);
            _endpointDestino = new IPEndPoint(IPAddress.Broadcast, Puerto);

            SuscribirEventos();

            _logger.LogInformation("Servidor UDP WSJT-X iniciado en puerto {Puerto}.", Puerto);

            // Heartbeat cada 15 segundos
            _timerHeartbeat = new PeriodicTimer(TimeSpan.FromSeconds(15));

            Task tareaEscucha = EscucharAsync(stoppingToken);
            Task tareaHeartbeat = EmitirHeartbeatsAsync(stoppingToken);

            await Task.WhenAny(tareaEscucha, tareaHeartbeat);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en el servidor UDP WSJT-X.");
        }
        finally
        {
            _udpCliente?.Close();
            _udpCliente?.Dispose();
            _timerHeartbeat?.Dispose();
        }
    }

    private async Task EmitirHeartbeatsAsync(CancellationToken ct)
    {
        while (_timerHeartbeat is not null && await _timerHeartbeat.WaitForNextTickAsync(ct))
        {
            MensajeHeartbeat heartbeat = new(
                ProtocoloWsjtx.IdAplicacion,
                ProtocoloWsjtx.SchemaVersion,
                "1.0.0",
                "");

            await EmitirAsync(EscritorMensajeWsjtx.SerializarHeartbeat(heartbeat), ct);
        }
    }

    private async Task EscucharAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _udpCliente is not null)
        {
            try
            {
                UdpReceiveResult resultado = await _udpCliente.ReceiveAsync(ct);
                ProcesarMensajeRecibido(resultado.Buffer);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al recibir mensaje UDP.");
            }
        }
    }

    private void ProcesarMensajeRecibido(byte[] datos)
    {
        LectorMensajeWsjtx lector = new(datos);
        TipoMensajeWsjtx? tipo = lector.LeerHeader();

        if (tipo is null) { return; }

        switch (tipo.Value)
        {
            case TipoMensajeWsjtx.Clear:
                _logger.LogInformation("Recibido Clear desde cliente UDP.");
                break;

            case TipoMensajeWsjtx.HaltTx:
                _logger.LogInformation("Recibido HaltTx desde cliente UDP.");
                _ = _operacionDigital.DetenerTxAsync();
                break;

            case TipoMensajeWsjtx.HighlightCallsign:
                string? indicativo = lector.LeerString();
                _logger.LogInformation("Recibido HighlightCallsign: {Indicativo}", indicativo);
                break;

            default:
                _logger.LogDebug("Mensaje UDP tipo {Tipo} no procesado.", tipo.Value);
                break;
        }
    }

    private void SuscribirEventos()
    {
        if (_suscrito) { return; }

        // Suscribir a decodificaciones
        foreach (IDecodificadorDigital decodificador in _registroDecodificadores.ObtenerTodos())
        {
            decodificador.MensajeDecodificadoRecibido += async (_, mensaje) =>
            {
                MensajeDecode decode = new(
                    ProtocoloWsjtx.IdAplicacion,
                    true,
                    (uint)(mensaje.MarcaDeTiempo.TimeOfDay.TotalMilliseconds),
                    mensaje.Snr,
                    mensaje.DeltaTiempo,
                    (uint)mensaje.FrecuenciaAudioHz,
                    mensaje.Modo.ToString(),
                    mensaje.Texto,
                    false,
                    false);

                await EmitirAsync(EscritorMensajeWsjtx.SerializarDecode(decode));
            };
        }

        // Suscribir a cambios de estado
        _estadoOperacion.EstadoCambiado += async (_, estado) =>
        {
            MensajeStatus status = new(
                ProtocoloWsjtx.IdAplicacion,
                (ulong)estado.FrecuenciaHz,
                estado.Modo,
                "",
                "",
                estado.Modo,
                _operacionDigital.EstadoActual.TxHabilitado,
                estado.Transmitiendo,
                false,
                1500,
                1500,
                "",
                "",
                "",
                false,
                "",
                false,
                0,
                0,
                15,
                "Default");

            await EmitirAsync(EscritorMensajeWsjtx.SerializarStatus(status));
        };

        _suscrito = true;
    }

    private async Task EmitirAsync(byte[] datos, CancellationToken ct = default)
    {
        if (_udpCliente is null || _endpointDestino is null) { return; }

        try
        {
            await _udpCliente.SendAsync(datos, datos.Length, _endpointDestino);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al emitir mensaje UDP.");
        }
    }
}
