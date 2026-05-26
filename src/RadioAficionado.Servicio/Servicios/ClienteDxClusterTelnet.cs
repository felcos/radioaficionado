using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using RadioAficionado.Dominio.Alertas;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Servicio.Dtos;
using RadioAficionado.Servicio.Hubs;

namespace RadioAficionado.Servicio.Servicios;

/// <summary>
/// Servicio en segundo plano que se conecta a un servidor DX Cluster via Telnet,
/// parsea los spots DX recibidos y los emite a los clientes via SignalR.
/// Implementa reconexion automatica con backoff exponencial.
/// </summary>
public sealed class ClienteDxClusterTelnet : BackgroundService
{
    /// <summary>
    /// Patron regex para parsear lineas de spots DX con formato estandar.
    /// Ejemplo: "DX de EA1ABC:     14074.0  JA1XYZ       FT8 -12dB         1234Z"
    /// </summary>
    private static readonly Regex _patronSpotDx = new(
        @"^DX\s+de\s+(\S+?):\s+([\d.]+)\s+(\S+)\s+(.*?)\s+(\d{4})Z\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly int[] _retrasosReconexionSegundos = [5, 10, 30, 60];

    private readonly IHubContext<HubEstado, IClienteHubEstado> _contextoHub;
    private readonly IServicioAlertas _servicioAlertas;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<ClienteDxClusterTelnet> _logger;

    private string _servidor = "dxc.ve7cc.net";
    private int _puerto = 23;
    private string _indicativoPropio = string.Empty;

    /// <summary>
    /// Crea el cliente DX Cluster por Telnet.
    /// </summary>
    /// <param name="contextoHub">Contexto del hub SignalR para emitir spots.</param>
    /// <param name="servicioAlertas">Servicio de alertas para evaluar spots.</param>
    /// <param name="configuracion">Configuracion de la aplicacion.</param>
    /// <param name="logger">Logger para registrar eventos.</param>
    public ClienteDxClusterTelnet(
        IHubContext<HubEstado, IClienteHubEstado> contextoHub,
        IServicioAlertas servicioAlertas,
        IConfiguration configuracion,
        ILogger<ClienteDxClusterTelnet> logger)
    {
        _contextoHub = contextoHub ?? throw new ArgumentNullException(nameof(contextoHub));
        _servicioAlertas = servicioAlertas ?? throw new ArgumentNullException(nameof(servicioAlertas));
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Bucle principal: conecta al servidor DX Cluster, lee spots y reconecta si se pierde la conexion.
    /// </summary>
    /// <param name="stoppingToken">Token de cancelacion del host.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CargarConfiguracion();

        if (string.IsNullOrWhiteSpace(_indicativoPropio))
        {
            _logger.LogWarning(
                "No se ha configurado un indicativo para el DX Cluster (DxCluster:IndicativoPropio). " +
                "El servicio no se iniciara hasta que se configure.");
            return;
        }

        int intentoReconexion = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConectarYProcesarAsync(stoppingToken);

                // Si sale normalmente (sin excepcion), resetear contador de reintentos
                intentoReconexion = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cliente DX Cluster detenido por solicitud de cancelacion.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la conexion al DX Cluster {Servidor}:{Puerto}.", _servidor, _puerto);
            }

            if (stoppingToken.IsCancellationRequested) { break; }

            // Backoff exponencial: 5s, 10s, 30s, 60s (y se queda en 60s)
            int indiceRetraso = Math.Min(intentoReconexion, _retrasosReconexionSegundos.Length - 1);
            int segundosEspera = _retrasosReconexionSegundos[indiceRetraso];
            intentoReconexion++;

            _logger.LogInformation(
                "Reintentando conexion al DX Cluster en {Segundos} segundos (intento #{Intento})...",
                segundosEspera, intentoReconexion);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(segundosEspera), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Establece la conexion TCP, realiza el login y procesa lineas de spots.
    /// </summary>
    private async Task ConectarYProcesarAsync(CancellationToken ct)
    {
        using TcpClient clienteTcp = new();
        clienteTcp.ReceiveTimeout = 120_000; // 2 minutos sin datos = timeout

        _logger.LogInformation("Conectando al DX Cluster {Servidor}:{Puerto}...", _servidor, _puerto);

        using CancellationTokenSource ctsConexion = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ctsConexion.CancelAfter(TimeSpan.FromSeconds(30));

        await clienteTcp.ConnectAsync(_servidor, _puerto, ctsConexion.Token);

        _logger.LogInformation("Conectado al DX Cluster {Servidor}:{Puerto}.", _servidor, _puerto);

        await using NetworkStream stream = clienteTcp.GetStream();
        using StreamReader lector = new(stream, Encoding.ASCII);
        await using StreamWriter escritor = new(stream, Encoding.ASCII) { AutoFlush = true };

        // Fase de login: esperar prompt y enviar indicativo
        await RealizarLoginAsync(lector, escritor, ct);

        _logger.LogInformation("Login exitoso en DX Cluster como {Indicativo}.", _indicativoPropio);

        // Notificar a los clientes que estamos conectados
        await _contextoHub.Clients.All.RecibirNotificacion("dxcluster-conexion", "conectado");

        // Bucle principal: leer lineas y parsear spots
        await LeerSpotsAsync(lector, ct);
    }

    /// <summary>
    /// Realiza el login en el servidor DX Cluster esperando el prompt y enviando el indicativo.
    /// </summary>
    private async Task RealizarLoginAsync(StreamReader lector, StreamWriter escritor, CancellationToken ct)
    {
        // Leer lineas hasta encontrar el prompt de login
        // Los servidores DX Cluster suelen enviar "login:" o "Please enter your call:" o similar
        StringBuilder bufferLogin = new();

        while (!ct.IsCancellationRequested)
        {
            string? linea = await LeerLineaConTimeoutAsync(lector, TimeSpan.FromSeconds(30), ct);

            if (linea is null)
            {
                throw new InvalidOperationException("El servidor cerro la conexion antes del login.");
            }

            bufferLogin.AppendLine(linea);
            _logger.LogDebug("DX Cluster login: {Linea}", linea);

            string lineaLower = linea.ToLowerInvariant();

            if (lineaLower.Contains("login:") ||
                lineaLower.Contains("please enter your call") ||
                lineaLower.Contains("your callsign") ||
                lineaLower.Contains("call:") ||
                lineaLower.Contains("callsign"))
            {
                await escritor.WriteLineAsync(_indicativoPropio.AsMemory(), ct);
                _logger.LogDebug("Indicativo enviado: {Indicativo}", _indicativoPropio);
                return;
            }
        }
    }

    /// <summary>
    /// Lee lineas del stream y parsea los spots DX encontrados.
    /// </summary>
    private async Task LeerSpotsAsync(StreamReader lector, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string? linea = await LeerLineaConTimeoutAsync(lector, TimeSpan.FromMinutes(5), ct);

            if (linea is null)
            {
                _logger.LogWarning("El servidor DX Cluster cerro la conexion.");
                await _contextoHub.Clients.All.RecibirNotificacion("dxcluster-conexion", "desconectado");
                return;
            }

            if (string.IsNullOrWhiteSpace(linea)) { continue; }

            SpotDxDto? spot = ParsearLineaSpot(linea);

            if (spot is not null)
            {
                await _contextoHub.Clients.All.RecibirSpotDx(spot);

                // Evaluar alertas configuradas
                IReadOnlyList<ResultadoAlerta> alertas = _servicioAlertas.EvaluarSpot(
                    spot.Spotteador, spot.Dx, spot.FrecuenciaHz, spot.Comentario, spot.HoraUtc);

                foreach (ResultadoAlerta alerta in alertas)
                {
                    await _contextoHub.Clients.All.RecibirAlerta(
                        alerta.Regla.Nombre,
                        alerta.Mensaje,
                        alerta.Dx,
                        alerta.Frecuencia.Hz,
                        alerta.Regla.ConSonido);
                }
            }
        }

        await _contextoHub.Clients.All.RecibirNotificacion("dxcluster-conexion", "desconectado");
    }

    /// <summary>
    /// Lee una linea del StreamReader con un timeout configurable.
    /// </summary>
    private static async Task<string?> LeerLineaConTimeoutAsync(
        StreamReader lector, TimeSpan timeout, CancellationToken ct)
    {
        using CancellationTokenSource ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ctsTimeout.CancelAfter(timeout);

        try
        {
            return await lector.ReadLineAsync(ctsTimeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout expirado, no cancelacion del host
            return null;
        }
    }

    /// <summary>
    /// Parsea una linea de texto del DX Cluster e intenta extraer un spot DX.
    /// Formato esperado: "DX de EA1ABC:  14074.0  JA1XYZ  FT8 -12dB         1234Z"
    /// </summary>
    /// <param name="linea">Linea de texto recibida del servidor.</param>
    /// <returns>El DTO del spot si la linea es un spot valido; null en caso contrario.</returns>
    public SpotDxDto? ParsearLineaSpot(string linea)
    {
        Match match = _patronSpotDx.Match(linea.Trim());

        if (!match.Success) { return null; }

        string spotteador = match.Groups[1].Value.TrimEnd(':').ToUpperInvariant();
        string frecuenciaTexto = match.Groups[2].Value;
        string dx = match.Groups[3].Value.ToUpperInvariant();
        string comentario = match.Groups[4].Value.Trim();
        string horaTexto = match.Groups[5].Value;

        // Parsear frecuencia (viene en KHz, convertir a Hz)
        if (!double.TryParse(frecuenciaTexto, NumberStyles.Float, CultureInfo.InvariantCulture, out double frecuenciaKHz))
        {
            _logger.LogDebug("No se pudo parsear la frecuencia: {Frecuencia}", frecuenciaTexto);
            return null;
        }

        long frecuenciaHz = (long)(frecuenciaKHz * 1_000.0);

        // Parsear hora (formato HHMM) como hora UTC del dia actual
        DateTime horaUtc = DateTime.UtcNow.Date;
        if (horaTexto.Length == 4 &&
            int.TryParse(horaTexto[..2], out int horas) &&
            int.TryParse(horaTexto[2..], out int minutos))
        {
            horaUtc = horaUtc.AddHours(horas).AddMinutes(minutos);
        }

        SpotDxDto dto = new(
            spotteador,
            dx,
            frecuenciaHz,
            comentario,
            horaUtc);

        _logger.LogDebug(
            "Spot DX: {Spotteador} -> {Dx} en {Frecuencia} KHz: {Comentario}",
            spotteador, dx, frecuenciaKHz, comentario);

        return dto;
    }

    /// <summary>
    /// Carga la configuracion del DX Cluster desde IConfiguration.
    /// Seccion esperada: "DxCluster" con Servidor, Puerto, IndicativoPropio.
    /// </summary>
    private void CargarConfiguracion()
    {
        IConfigurationSection seccion = _configuracion.GetSection("DxCluster");

        _servidor = seccion["Servidor"] ?? _servidor;

        if (int.TryParse(seccion["Puerto"], out int puerto) && puerto > 0)
        {
            _puerto = puerto;
        }

        _indicativoPropio = seccion["IndicativoPropio"] ?? string.Empty;

        _logger.LogInformation(
            "Configuracion DX Cluster: Servidor={Servidor}, Puerto={Puerto}, Indicativo={Indicativo}",
            _servidor, _puerto, _indicativoPropio);
    }
}
