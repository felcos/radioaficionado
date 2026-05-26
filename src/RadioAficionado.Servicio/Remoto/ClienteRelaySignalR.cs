using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using RadioAficionado.Compartido.Contratos;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Servicio.Remoto;

/// <summary>
/// Cliente SignalR que conecta al hub de tunel del servidor web para recibir
/// comandos remotos de control del rig y reportar estado.
/// Implementa reconexion automatica con backoff exponencial.
/// </summary>
public sealed class ClienteRelaySignalR : IHostedService, IAsyncDisposable
{
    private readonly IControlRig _controlRig;
    private readonly IServicioWaterfall? _servicioWaterfall;
    private readonly IRegistroDecodificadores? _registroDecodificadores;
    private readonly AdaptadorWebRtcAudio _adaptadorWebRtc;
    private readonly ConfiguracionRemoto _configuracion;
    private readonly ILogger<ClienteRelaySignalR> _logger;
    private HubConnection? _conexion;
    private bool _desechado;
    private long _ultimoEnvioWaterfallTicks;

    /// <summary>
    /// Intervalos de reconexion con backoff exponencial.
    /// </summary>
    private static readonly TimeSpan[] _intervalosReconexion =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60)
    ];

    /// <summary>
    /// Crea una nueva instancia del cliente relay SignalR.
    /// </summary>
    /// <param name="controlRig">Servicio de control del rig local.</param>
    /// <param name="opciones">Configuracion de conexion remota.</param>
    /// <param name="logger">Logger para registro de eventos.</param>
    public ClienteRelaySignalR(
        IControlRig controlRig,
        IOptions<ConfiguracionRemoto> opciones,
        ILogger<ClienteRelaySignalR> logger,
        IServicioWaterfall? servicioWaterfall = null,
        IRegistroDecodificadores? registroDecodificadores = null)
    {
        ArgumentNullException.ThrowIfNull(controlRig);
        ArgumentNullException.ThrowIfNull(opciones);
        ArgumentNullException.ThrowIfNull(logger);

        _controlRig = controlRig;
        _servicioWaterfall = servicioWaterfall;
        _registroDecodificadores = registroDecodificadores;
        _adaptadorWebRtc = new AdaptadorWebRtcAudio(logger);
        _configuracion = opciones.Value;
        _logger = logger;
    }

    /// <summary>
    /// Inicia la conexion al hub del servidor si la funcionalidad remota esta habilitada.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuracion.Habilitado)
        {
            _logger.LogInformation("Conexion remota deshabilitada. No se conectara al servidor");
            return;
        }

        if (string.IsNullOrWhiteSpace(_configuracion.UrlServidor))
        {
            _logger.LogWarning("URL del servidor remoto no configurada. No se conectara");
            return;
        }

        if (string.IsNullOrWhiteSpace(_configuracion.ClaveApi))
        {
            _logger.LogWarning("Clave API no configurada. No se conectara al servidor remoto");
            return;
        }

        _logger.LogInformation(
            "Iniciando conexion remota al servidor {UrlServidor}",
            _configuracion.UrlServidor);

        ConstruirConexion();
        RegistrarHandlers();
        SuscribirseACambiosDeEstado();
        SuscribirseAEventosWebRtc();

        try
        {
            await _conexion!.StartAsync(cancellationToken);
            _logger.LogInformation("Conexion remota establecida correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al conectar con el servidor remoto {UrlServidor}. Se reintentara automaticamente",
                _configuracion.UrlServidor);
        }
    }

    /// <summary>
    /// Detiene la conexion al hub del servidor.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_conexion is null)
        {
            return;
        }

        _logger.LogInformation("Deteniendo conexion remota");

        try
        {
            await _conexion.StopAsync(cancellationToken);
            _logger.LogInformation("Conexion remota detenida correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al detener la conexion remota");
        }
    }

    /// <summary>
    /// Libera los recursos de la conexion SignalR.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_desechado)
        {
            return;
        }

        _desechado = true;

        if (_conexion is not null)
        {
            await _conexion.DisposeAsync();
            _conexion = null;
        }
    }

    /// <summary>
    /// Construye la conexion SignalR con reconexion automatica y headers de autenticacion.
    /// </summary>
    private void ConstruirConexion()
    {
        string urlHub = $"{_configuracion.UrlServidor.TrimEnd('/')}/hubs/tunel-servicio";

        _conexion = new HubConnectionBuilder()
            .WithUrl(urlHub, opciones =>
            {
                opciones.Headers.Add("X-Api-Key", _configuracion.ClaveApi);
            })
            .WithAutomaticReconnect(_intervalosReconexion)
            .Build();

        _conexion.Closed += OnConexionCerrada;
        _conexion.Reconnecting += OnReconectando;
        _conexion.Reconnected += OnReconectado;
    }

    /// <summary>
    /// Registra los handlers para los metodos que el servidor puede invocar.
    /// </summary>
    private void RegistrarHandlers()
    {
        if (_conexion is null)
        {
            return;
        }

        _conexion.On<ComandoRemotoRig>("EjecutarComandoRig", async (ComandoRemotoRig comando) =>
        {
            await ProcesarComandoAsync(comando);
        });

        _conexion.On<SenalizacionWebRtc>("RecibirSenalizacion", async (SenalizacionWebRtc senalizacion) =>
        {
            await ProcesarSenalizacionAsync(senalizacion);
        });
    }

    /// <summary>
    /// Suscribe al evento EstadoCambiado del IControlRig para enviar
    /// actualizaciones de estado automaticamente al servidor.
    /// </summary>
    private void SuscribirseACambiosDeEstado()
    {
        _controlRig.EstadoCambiado += async (object? remitente, EstadoRig estado) =>
        {
            await EnviarEstadoActualAsync(estado);
        };

        // Suscribir waterfall con throttle a ~8fps
        if (_servicioWaterfall is not null)
        {
            _servicioWaterfall.LineaEspectroGenerada += (object? sender, LineaEspectroEventArgs e) =>
            {
                if (_conexion?.State != HubConnectionState.Connected) { return; }

                long ahora = Stopwatch.GetTimestamp();
                long ultimo = Interlocked.Read(ref _ultimoEnvioWaterfallTicks);
                long intervaloMinimo = Stopwatch.Frequency / 8;
                if (ahora - ultimo < intervaloMinimo) { return; }
                Interlocked.Exchange(ref _ultimoEnvioWaterfallTicks, ahora);

                byte[] magnitudes = ConvertirMagnitudesABytes(e.MagnitudesDb);
                LineaEspectroRemotaDto dto = new(magnitudes, e.ResolucionHz, (long)e.FrecuenciaMinHz);
                _ = EnviarSinBloquearAsync("ReportarLineaEspectro", dto);
            };
            _logger.LogInformation("Suscrito al waterfall para retransmision remota");
        }

        // Suscribir decodificadores
        if (_registroDecodificadores is not null)
        {
            foreach (IDecodificadorDigital decodificador in _registroDecodificadores.ObtenerTodos())
            {
                decodificador.MensajeDecodificadoRecibido += (object? sender, MensajeDecodificado mensaje) =>
                {
                    if (_conexion?.State != HubConnectionState.Connected) { return; }

                    string color = mensaje.Texto.StartsWith("CQ ", StringComparison.OrdinalIgnoreCase) ? "#ff4444" : "#ffffff";
                    MensajeDecodificadoRemotoDto dto = new(
                        mensaje.MarcaDeTiempo.UtcDateTime, mensaje.FrecuenciaAudioHz, mensaje.Snr,
                        mensaje.DeltaTiempo, mensaje.Modo.ToString(), mensaje.Texto,
                        mensaje.IndicativoEmisor, mensaje.IndicativoDestinatario,
                        mensaje.Localizador, mensaje.ReporteSenal, color);
                    _ = EnviarSinBloquearAsync("ReportarMensajeDecodificado", dto);
                };
            }
            _logger.LogInformation("Suscrito a {Cantidad} decodificadores para retransmision remota",
                _registroDecodificadores.ObtenerTodos().Count);
        }
    }

    /// <summary>
    /// Suscribe los eventos del adaptador WebRTC para reenviar senalizacion al servidor.
    /// </summary>
    private void SuscribirseAEventosWebRtc()
    {
        _adaptadorWebRtc.RespuestaSdpGenerada += async (object? remitente, string sdpRespuesta) =>
        {
            SenalizacionWebRtc senalizacion = new(
                TipoSenalizacion.Respuesta,
                sdpRespuesta,
                null,
                null,
                null);

            await EnviarSinBloquearAsync("EnviarSenalizacion", senalizacion);
            _logger.LogInformation("Respuesta SDP enviada al servidor via SignalR");
        };

        _adaptadorWebRtc.CandidatoIceGenerado += async (object? remitente, CandidatoIceLocal candidato) =>
        {
            SenalizacionWebRtc senalizacion = new(
                TipoSenalizacion.CandidatoIce,
                null,
                candidato.Candidato,
                candidato.SdpMid,
                candidato.IndiceLinea);

            await EnviarSinBloquearAsync("EnviarSenalizacion", senalizacion);
            _logger.LogDebug(
                "Candidato ICE local enviado al servidor (sdpMid={SdpMid})",
                candidato.SdpMid);
        };
    }

    /// <summary>
    /// Procesa senalizacion WebRTC recibida del servidor.
    /// </summary>
    private async Task ProcesarSenalizacionAsync(SenalizacionWebRtc senalizacion)
    {
        switch (senalizacion.Tipo)
        {
            case TipoSenalizacion.Oferta:
                await _adaptadorWebRtc.ProcesarOfertaAsync(senalizacion.Sdp ?? string.Empty);
                break;
            case TipoSenalizacion.CandidatoIce:
                await _adaptadorWebRtc.ProcesarCandidatoIceAsync(
                    senalizacion.CandidatoIce ?? string.Empty,
                    senalizacion.SdpMid,
                    senalizacion.IndiceLineaSdp);
                break;
            default:
                _logger.LogDebug("Tipo de senalizacion no manejado: {Tipo}", senalizacion.Tipo);
                break;
        }
    }

    /// <summary>
    /// Envia datos al servidor sin bloquear el hilo actual.
    /// </summary>
    private async Task EnviarSinBloquearAsync<T>(string metodo, T datos)
    {
        try
        {
            if (_conexion?.State == HubConnectionState.Connected)
            {
                await _conexion.InvokeAsync(metodo, datos);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error al enviar {Metodo} al servidor", metodo);
        }
    }

    /// <summary>
    /// Convierte magnitudes dB (double[]) a bytes (0-255). Rango: -120dB a 0dB.
    /// </summary>
    private static byte[] ConvertirMagnitudesABytes(double[] magnitudesDb)
    {
        byte[] resultado = new byte[magnitudesDb.Length];
        for (int i = 0; i < magnitudesDb.Length; i++)
        {
            double normalizado = Math.Clamp((magnitudesDb[i] + 120.0) / 120.0, 0.0, 1.0);
            resultado[i] = (byte)(normalizado * 255.0);
        }
        return resultado;
    }

    /// <summary>
    /// Procesa un comando remoto recibido del servidor, ejecutandolo en el rig local
    /// y reportando la respuesta y el estado actualizado.
    /// </summary>
    /// <param name="comando">Comando remoto a ejecutar.</param>
    private async Task ProcesarComandoAsync(ComandoRemotoRig comando)
    {
        _logger.LogInformation(
            "Comando remoto recibido: {TipoComando} (Id: {ComandoId}, Usuario: {UsuarioId})",
            comando.Tipo,
            comando.Id,
            comando.UsuarioId);

        RespuestaRemotoRig respuesta;

        try
        {
            await EjecutarComandoEnRigAsync(comando);
            respuesta = RespuestaRemotoRig.Exito(comando.Id);

            _logger.LogInformation(
                "Comando {ComandoId} ejecutado correctamente",
                comando.Id);
        }
        catch (Exception ex)
        {
            respuesta = RespuestaRemotoRig.Error(comando.Id, ex.Message);

            _logger.LogError(
                ex,
                "Error al ejecutar comando {ComandoId} de tipo {TipoComando}",
                comando.Id,
                comando.Tipo);
        }

        await EnviarRespuestaAsync(respuesta);
        await EnviarEstadoActualDesdeLecturaAsync();
    }

    /// <summary>
    /// Ejecuta un comando especifico en el rig local segun su tipo.
    /// </summary>
    /// <param name="comando">Comando a ejecutar.</param>
    /// <exception cref="ArgumentOutOfRangeException">Si el tipo de comando no es soportado.</exception>
    private async Task EjecutarComandoEnRigAsync(ComandoRemotoRig comando)
    {
        switch (comando.Tipo)
        {
            case TipoComandoRig.CambiarFrecuencia:
                long frecuenciaHz = ObtenerValorDelPayload<long>(comando.Payload, "valor");
                Frecuencia frecuencia = Frecuencia.DesdeHz(frecuenciaHz);
                await _controlRig.CambiarFrecuenciaAsync(frecuencia);
                break;

            case TipoComandoRig.CambiarModo:
                string nombreModo = ObtenerValorDelPayload(comando.Payload, "valor");
                ModoOperacion modo = Enum.Parse<ModoOperacion>(nombreModo, ignoreCase: true);
                await _controlRig.CambiarModoAsync(modo);
                break;

            case TipoComandoRig.CambiarBanda:
                long frecuenciaBandaHz = ObtenerValorDelPayload<long>(comando.Payload, "valor");
                Frecuencia frecuenciaBanda = Frecuencia.DesdeHz(frecuenciaBandaHz);
                await _controlRig.CambiarFrecuenciaAsync(frecuenciaBanda);
                break;

            case TipoComandoRig.CambiarPtt:
                bool activarPtt = ObtenerValorDelPayload<bool>(comando.Payload, "valor");
                await _controlRig.CambiarPttAsync(activarPtt);
                break;

            case TipoComandoRig.CambiarVfo:
                string vfoTexto = ObtenerValorDelPayload(comando.Payload, "valor");
                char vfo = vfoTexto.Length > 0 ? vfoTexto[0] : 'A';
                await _controlRig.CambiarVfoAsync(vfo);
                break;

            case TipoComandoRig.Conectar:
                await _controlRig.ConectarAsync();
                break;

            case TipoComandoRig.Desconectar:
                await _controlRig.DesconectarAsync();
                break;

            case TipoComandoRig.ObtenerEstado:
                break;

            case TipoComandoRig.CambiarPotencia:
                double vatios = ObtenerValorDelPayload<double>(comando.Payload, "valor");
                await _controlRig.CambiarPotenciaAsync(vatios);
                break;

            case TipoComandoRig.CambiarSplit:
                bool activarSplit = ObtenerValorDelPayload<bool>(comando.Payload, "valor");
                await _controlRig.ActivarSplitAsync(activarSplit);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(comando),
                    comando.Tipo,
                    "Tipo de comando no soportado");
        }
    }

    /// <summary>
    /// Envia una respuesta al servidor via el metodo "ReportarRespuesta".
    /// </summary>
    /// <param name="respuesta">Respuesta a enviar.</param>
    private async Task EnviarRespuestaAsync(RespuestaRemotoRig respuesta)
    {
        if (_conexion?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("No se puede enviar respuesta: conexion no establecida");
            return;
        }

        try
        {
            await _conexion.InvokeAsync("ReportarRespuesta", respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar respuesta del comando {ComandoId}", respuesta.ComandoId);
        }
    }

    /// <summary>
    /// Lee el estado actual del rig y lo envia al servidor.
    /// </summary>
    private async Task EnviarEstadoActualDesdeLecturaAsync()
    {
        try
        {
            EstadoRig estadoLocal = await _controlRig.ObtenerEstadoAsync();
            EstadoRigRemotoDto estadoRemoto = ConversorEstadoRemoto.ConvertirARemoto(
                estadoLocal,
                _controlRig.EstaConectado);

            await EnviarEstadoRemotoAsync(estadoRemoto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener y enviar estado actual del rig");
        }
    }

    /// <summary>
    /// Convierte y envia el estado del rig al servidor cuando se detecta un cambio.
    /// </summary>
    /// <param name="estado">Estado del rig que cambio.</param>
    private async Task EnviarEstadoActualAsync(EstadoRig estado)
    {
        EstadoRigRemotoDto estadoRemoto = ConversorEstadoRemoto.ConvertirARemoto(
            estado,
            _controlRig.EstaConectado);

        await EnviarEstadoRemotoAsync(estadoRemoto);
    }

    /// <summary>
    /// Envia un DTO de estado remoto al servidor via el metodo "ReportarEstadoRig".
    /// </summary>
    /// <param name="estadoRemoto">Estado remoto a enviar.</param>
    private async Task EnviarEstadoRemotoAsync(EstadoRigRemotoDto estadoRemoto)
    {
        if (_conexion?.State != HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _conexion.InvokeAsync("ReportarEstadoRig", estadoRemoto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar estado del rig al servidor");
        }
    }

    /// <summary>
    /// Obtiene un valor string del diccionario payload del comando.
    /// </summary>
    /// <param name="payload">Diccionario de payload.</param>
    /// <param name="clave">Clave a buscar.</param>
    /// <returns>Valor como string.</returns>
    private static string ObtenerValorDelPayload(IReadOnlyDictionary<string, string> payload, string clave)
    {
        if (!payload.TryGetValue(clave, out string? valor) || string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                $"El payload no contiene la clave requerida '{clave}'");
        }

        return valor;
    }

    /// <summary>
    /// Obtiene un valor tipado del diccionario payload del comando.
    /// </summary>
    /// <typeparam name="T">Tipo esperado (long, double, bool).</typeparam>
    /// <param name="payload">Diccionario de payload.</param>
    /// <param name="clave">Clave a buscar.</param>
    /// <returns>Valor convertido al tipo solicitado.</returns>
    private static T ObtenerValorDelPayload<T>(IReadOnlyDictionary<string, string> payload, string clave)
        where T : struct
    {
        string textoValor = ObtenerValorDelPayload(payload, clave);
        return (T)Convert.ChangeType(textoValor, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Callback cuando la conexion se cierra inesperadamente.
    /// </summary>
    private Task OnConexionCerrada(Exception? excepcion)
    {
        if (excepcion is not null)
        {
            _logger.LogError(excepcion, "Conexion remota cerrada por error");
        }
        else
        {
            _logger.LogInformation("Conexion remota cerrada");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Callback cuando se inicia un intento de reconexion.
    /// </summary>
    private Task OnReconectando(Exception? excepcion)
    {
        _logger.LogWarning(
            excepcion,
            "Intentando reconectar al servidor remoto {UrlServidor}",
            _configuracion.UrlServidor);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Callback cuando la reconexion tiene exito. Envia el estado actual del rig.
    /// </summary>
    private async Task OnReconectado(string? connectionId)
    {
        _logger.LogInformation(
            "Reconexion exitosa al servidor remoto (ConnectionId: {ConnectionId})",
            connectionId);

        await EnviarEstadoActualDesdeLecturaAsync();
    }
}
