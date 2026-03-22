using System.Globalization;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using Serilog;

namespace RadioAficionado.Infraestructura.DxCluster;

/// <summary>
/// Cliente TCP para conectarse a servidores de DX Cluster vía Telnet.
/// Recibe spots DX en tiempo real, parsea el formato estándar y emite eventos.
/// Soporta reconexión automática y login automático.
/// </summary>
public sealed class ClienteDxCluster : IDxCluster
{
    /// <summary>
    /// Expresión regular para parsear spots DX en formato estándar.
    /// Formato: "DX de EA4ABC:  14076.0 JA1XYZ    FT8 -15dB          1845Z"
    /// </summary>
    private static readonly Regex _patronSpot = new(
        @"^DX\s+de\s+([A-Z0-9/]+):\s+(\d+\.?\d*)\s+([A-Z0-9/]+)\s*(.*?)\s+(\d{4})Z\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private TcpClient? _clienteTcp;
    private StreamReader? _lector;
    private StreamWriter? _escritor;
    private CancellationTokenSource? _ctsLectura;
    private Task? _tareaLectura;
    private bool _disposed;

    private string _servidor = string.Empty;
    private int _puerto;
    private string _indicativo = string.Empty;

    /// <inheritdoc />
    public bool EstaConectado => _clienteTcp?.Connected == true;

    /// <inheritdoc />
    public event EventHandler<SpotDx>? SpotRecibido;

    /// <inheritdoc />
    public event EventHandler<string>? ConexionPerdida;

    /// <inheritdoc />
    public event EventHandler? Reconectado;

    /// <summary>
    /// Crea una nueva instancia del cliente DX Cluster.
    /// </summary>
    /// <param name="logger">Logger de Serilog para registrar la actividad.</param>
    /// <exception cref="ArgumentNullException">Si el logger es null.</exception>
    public ClienteDxCluster(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Crea una nueva instancia del cliente DX Cluster con un logger por defecto.
    /// </summary>
    public ClienteDxCluster() : this(Log.Logger)
    {
    }

    /// <inheritdoc />
    public async Task ConectarAsync(string servidor, int puerto, string indicativo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(servidor))
        {
            throw new ArgumentException("El servidor no puede estar vacío.", nameof(servidor));
        }

        if (puerto is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(puerto), puerto, "El puerto debe estar entre 1 y 65535.");
        }

        if (string.IsNullOrWhiteSpace(indicativo))
        {
            throw new ArgumentException("El indicativo no puede estar vacío.", nameof(indicativo));
        }

        await DesconectarAsync(ct).ConfigureAwait(false);

        _servidor = servidor;
        _puerto = puerto;
        _indicativo = indicativo.Trim().ToUpperInvariant();

        await ConectarInternoAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DesconectarAsync(CancellationToken ct = default)
    {
        if (_ctsLectura is not null)
        {
            await _ctsLectura.CancelAsync().ConfigureAwait(false);

            if (_tareaLectura is not null)
            {
                try
                {
                    await _tareaLectura.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Esperado al cancelar la lectura.
                }
            }

            _ctsLectura.Dispose();
            _ctsLectura = null;
            _tareaLectura = null;
        }

        await CerrarConexionTcpAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EnviarSpotAsync(SpotDx spot, CancellationToken ct = default)
    {
        if (spot is null)
        {
            throw new ArgumentNullException(nameof(spot));
        }

        VerificarConexion();

        // Formato estándar de envío de spot: DX frecuenciaKHz indicativoDX comentario
        string frecuenciaKHz = spot.Frecuencia.KHz.ToString("F1", CultureInfo.InvariantCulture);
        string comando = $"DX {frecuenciaKHz} {spot.Dx.Valor} {spot.Comentario}".Trim();

        await EnviarLineaAsync(comando, ct).ConfigureAwait(false);

        _logger.Information("Spot DX enviado: {Frecuencia} {Dx} {Comentario}", frecuenciaKHz, spot.Dx.Valor, spot.Comentario);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DesconectarAsync().ConfigureAwait(false);
        _semaforo.Dispose();
    }

    /// <summary>
    /// Parsea una línea de texto recibida del DX Cluster y extrae un spot DX si el formato es válido.
    /// </summary>
    /// <param name="linea">Línea de texto recibida del cluster.</param>
    /// <returns>Un <see cref="SpotDx"/> si la línea tiene formato válido de spot, o null en caso contrario.</returns>
    public static SpotDx? ParsearSpot(string linea)
    {
        if (string.IsNullOrWhiteSpace(linea))
        {
            return null;
        }

        Match coincidencia = _patronSpot.Match(linea.Trim());

        if (!coincidencia.Success)
        {
            return null;
        }

        string spotteadorTexto = coincidencia.Groups[1].Value.Trim().ToUpperInvariant();
        string frecuenciaTexto = coincidencia.Groups[2].Value.Trim();
        string dxTexto = coincidencia.Groups[3].Value.Trim().ToUpperInvariant();
        string comentario = coincidencia.Groups[4].Value.Trim();
        string horaTexto = coincidencia.Groups[5].Value.Trim();

        // Parsear frecuencia (viene en kHz)
        if (!double.TryParse(frecuenciaTexto, NumberStyles.Float, CultureInfo.InvariantCulture, out double frecuenciaKHz) || frecuenciaKHz <= 0)
        {
            return null;
        }

        // Parsear hora (formato HHMM en UTC)
        if (horaTexto.Length != 4
            || !int.TryParse(horaTexto[..2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int horas)
            || !int.TryParse(horaTexto[2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutos)
            || horas is < 0 or > 23
            || minutos is < 0 or > 59)
        {
            return null;
        }

        try
        {
            Indicativo spotteador = new(spotteadorTexto);
            Indicativo dx = new(dxTexto);
            Frecuencia frecuencia = Frecuencia.DesdeKHz(frecuenciaKHz);

            DateTime ahora = DateTime.UtcNow;
            DateTime hora = new(ahora.Year, ahora.Month, ahora.Day, horas, minutos, 0, DateTimeKind.Utc);

            // Si la hora del spot es futura (más de 5 minutos), probablemente es del día anterior
            if (hora > ahora.AddMinutes(5))
            {
                hora = hora.AddDays(-1);
            }

            return new SpotDx(spotteador, dx, frecuencia, comentario, hora);
        }
        catch (ArgumentException)
        {
            // Indicativo o frecuencia inválidos
            return null;
        }
    }

    /// <summary>
    /// Establece la conexión TCP interna y comienza la lectura de datos.
    /// </summary>
    private async Task ConectarInternoAsync(CancellationToken ct)
    {
        _clienteTcp = new TcpClient();

        using CancellationTokenSource ctsConexion = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ctsConexion.CancelAfter(10_000);

        _logger.Information("Conectando al DX Cluster {Servidor}:{Puerto}...", _servidor, _puerto);

        await _clienteTcp.ConnectAsync(_servidor, _puerto, ctsConexion.Token).ConfigureAwait(false);

        NetworkStream flujoRed = _clienteTcp.GetStream();
        _lector = new StreamReader(flujoRed, leaveOpen: true);
        _escritor = new StreamWriter(flujoRed, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };

        _ctsLectura = new CancellationTokenSource();
        _tareaLectura = EjecutarLecturaAsync(_ctsLectura.Token);

        _logger.Information("Conectado al DX Cluster {Servidor}:{Puerto}", _servidor, _puerto);
    }

    /// <summary>
    /// Bucle principal de lectura que recibe líneas del cluster, detecta el prompt de login
    /// y parsea spots DX entrantes.
    /// </summary>
    private async Task EjecutarLecturaAsync(CancellationToken ct)
    {
        bool loginEnviado = false;

        try
        {
            while (!ct.IsCancellationRequested && EstaConectado)
            {
                string? linea = await LeerLineaAsync(ct).ConfigureAwait(false);

                if (linea is null)
                {
                    // Conexión cerrada por el servidor
                    _logger.Warning("El servidor DX Cluster cerró la conexión.");
                    break;
                }

                _logger.Debug("DX Cluster << {Linea}", linea);

                // Detectar prompt de login
                if (!loginEnviado && EsPromptDeLogin(linea))
                {
                    await EnviarLineaAsync(_indicativo, ct).ConfigureAwait(false);
                    loginEnviado = true;
                    _logger.Information("Login enviado al DX Cluster: {Indicativo}", _indicativo);
                    continue;
                }

                // Intentar parsear como spot DX
                SpotDx? spot = ParsearSpot(linea);
                if (spot is not null)
                {
                    _logger.Debug("Spot recibido: {Spotteador} -> {Dx} en {Frecuencia}", spot.Spotteador, spot.Dx, spot.Frecuencia);
                    SpotRecibido?.Invoke(this, spot);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelación esperada.
        }
        catch (IOException ex)
        {
            _logger.Warning(ex, "Conexión perdida con el DX Cluster {Servidor}:{Puerto}", _servidor, _puerto);
        }
        catch (ObjectDisposedException)
        {
            // El socket fue cerrado durante la lectura.
        }

        // Si no fue una desconexión intencional, intentar reconectar
        if (!ct.IsCancellationRequested && !_disposed)
        {
            ConexionPerdida?.Invoke(this, $"Conexión perdida con {_servidor}:{_puerto}");
            await IntentarReconexionAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Intenta reconectarse al servidor DX Cluster con reintentos exponenciales.
    /// </summary>
    private async Task IntentarReconexionAsync(CancellationToken ct)
    {
        int intentos = 0;
        int retrasoMs = 5_000;
        const int MaxRetrasoMs = 60_000;

        while (!ct.IsCancellationRequested && !_disposed)
        {
            intentos++;
            _logger.Information("Intentando reconexión al DX Cluster ({Intento})...", intentos);

            try
            {
                await CerrarConexionTcpAsync().ConfigureAwait(false);
                await Task.Delay(retrasoMs, ct).ConfigureAwait(false);
                await ConectarInternoAsync(ct).ConfigureAwait(false);

                _logger.Information("Reconexión exitosa al DX Cluster {Servidor}:{Puerto}", _servidor, _puerto);
                Reconectado?.Invoke(this, EventArgs.Empty);
                return;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Fallo en el intento de reconexión {Intento}", intentos);
                retrasoMs = Math.Min(retrasoMs * 2, MaxRetrasoMs);
            }
        }
    }

    /// <summary>
    /// Determina si una línea recibida del cluster es un prompt de login.
    /// </summary>
    /// <param name="linea">Línea de texto recibida.</param>
    /// <returns>True si la línea es un prompt que solicita indicativo.</returns>
    private static bool EsPromptDeLogin(string linea)
    {
        string lineaLower = linea.ToLowerInvariant();
        return lineaLower.Contains("login:")
            || lineaLower.Contains("please enter your call")
            || lineaLower.Contains("your callsign")
            || lineaLower.Contains("enter your callsign")
            || lineaLower.Contains("call:");
    }

    /// <summary>
    /// Envía una línea de texto al servidor DX Cluster, protegido por semáforo.
    /// </summary>
    /// <param name="texto">Texto a enviar.</param>
    /// <param name="ct">Token de cancelación.</param>
    private async Task EnviarLineaAsync(string texto, CancellationToken ct)
    {
        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_escritor is null)
            {
                throw new InvalidOperationException("No hay conexión activa con el DX Cluster.");
            }

            await _escritor.WriteLineAsync(texto.AsMemory(), ct).ConfigureAwait(false);
            _logger.Debug("DX Cluster >> {Texto}", texto);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>
    /// Lee una línea de texto del servidor DX Cluster, protegido por semáforo.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Línea leída, o null si la conexión se cerró.</returns>
    private async Task<string?> LeerLineaAsync(CancellationToken ct)
    {
        // No usamos semáforo para lectura: la tarea de lectura es la única que lee.
        if (_lector is null)
        {
            return null;
        }

        return await _lector.ReadLineAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Cierra y libera los recursos de la conexión TCP actual.
    /// </summary>
    private async Task CerrarConexionTcpAsync()
    {
        if (_escritor is not null)
        {
            await _escritor.DisposeAsync().ConfigureAwait(false);
            _escritor = null;
        }

        if (_lector is not null)
        {
            _lector.Dispose();
            _lector = null;
        }

        if (_clienteTcp is not null)
        {
            _clienteTcp.Close();
            _clienteTcp.Dispose();
            _clienteTcp = null;
        }
    }

    /// <summary>
    /// Verifica que haya una conexión activa con el DX Cluster.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si no hay conexión activa.</exception>
    private void VerificarConexion()
    {
        if (!EstaConectado)
        {
            throw new InvalidOperationException("No hay conexión activa con el DX Cluster. Llame a ConectarAsync primero.");
        }
    }
}
