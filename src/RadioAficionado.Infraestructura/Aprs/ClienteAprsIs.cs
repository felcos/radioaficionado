using System.Net.Sockets;
using System.Text;
using RadioAficionado.Dominio.Aprs;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using Microsoft.Extensions.Logging;

namespace RadioAficionado.Infraestructura.Aprs;

/// <summary>
/// Cliente para la conexión al servidor APRS-IS (Internet Service).
/// Gestiona la conexión TCP, autenticación, envío y recepción de paquetes APRS.
/// </summary>
public class ClienteAprsIs : IServicioAprs, IDisposable
{
    /// <summary>
    /// Versión del software enviada en el login APRS-IS.
    /// </summary>
    private const string NombreSoftware = "RadioAficionado";

    /// <summary>
    /// Versión del software enviada en el login APRS-IS.
    /// </summary>
    private const string VersionSoftware = "1.0";

    private readonly ILogger<ClienteAprsIs> _logger;
    private TcpClient? _clienteTcp;
    private StreamWriter? _escritor;
    private StreamReader? _lector;
    private CancellationTokenSource? _tokenCancelacionRecepcion;
    private Task? _tareaRecepcion;
    private ConfiguracionAprs? _configuracionActual;
    private int _contadorMensajes;
    private bool _disposed;

    /// <inheritdoc />
    public event Action<PaqueteAprs>? PaqueteRecibido;

    /// <inheritdoc />
    public bool EstaConectado => _clienteTcp?.Connected ?? false;

    /// <summary>
    /// Crea una nueva instancia de <see cref="ClienteAprsIs"/>.
    /// </summary>
    /// <param name="logger">Logger para registrar eventos de conexión y errores.</param>
    public ClienteAprsIs(ILogger<ClienteAprsIs> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ConectarAsync(ConfiguracionAprs configuracion, CancellationToken tokenCancelacion = default)
    {
        if (configuracion is null)
        {
            throw new ArgumentNullException(nameof(configuracion));
        }

        if (EstaConectado)
        {
            await DesconectarAsync().ConfigureAwait(false);
        }

        _configuracionActual = configuracion;
        _contadorMensajes = 0;

        _logger.LogInformation(
            "Conectando a APRS-IS: {Servidor}:{Puerto} como {Indicativo}",
            configuracion.Servidor,
            configuracion.Puerto,
            configuracion.Indicativo);

        _clienteTcp = new TcpClient();
        await _clienteTcp.ConnectAsync(configuracion.Servidor, configuracion.Puerto, tokenCancelacion).ConfigureAwait(false);

        NetworkStream flujoRed = _clienteTcp.GetStream();
        _escritor = new StreamWriter(flujoRed, Encoding.ASCII) { AutoFlush = true };
        _lector = new StreamReader(flujoRed, Encoding.ASCII);

        // Leer la línea de bienvenida del servidor
        string? lineaBienvenida = await _lector.ReadLineAsync(tokenCancelacion).ConfigureAwait(false);
        _logger.LogInformation("APRS-IS bienvenida: {Bienvenida}", lineaBienvenida);

        // Enviar login
        string comandoLogin = ConstruirComandoLogin(configuracion);
        await _escritor.WriteLineAsync(comandoLogin.AsMemory(), tokenCancelacion).ConfigureAwait(false);
        _logger.LogInformation("Login enviado a APRS-IS");

        // Leer respuesta de login
        string? respuestaLogin = await _lector.ReadLineAsync(tokenCancelacion).ConfigureAwait(false);
        _logger.LogInformation("APRS-IS respuesta login: {Respuesta}", respuestaLogin);

        // Iniciar recepción continua de paquetes
        _tokenCancelacionRecepcion = new CancellationTokenSource();
        _tareaRecepcion = RecibirPaquetesAsync(_tokenCancelacionRecepcion.Token);
    }

    /// <inheritdoc />
    public async Task DesconectarAsync()
    {
        _logger.LogInformation("Desconectando de APRS-IS");

        if (_tokenCancelacionRecepcion is not null)
        {
            await _tokenCancelacionRecepcion.CancelAsync().ConfigureAwait(false);

            if (_tareaRecepcion is not null)
            {
                try
                {
                    await _tareaRecepcion.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Esperado al cancelar
                }
            }

            _tokenCancelacionRecepcion.Dispose();
            _tokenCancelacionRecepcion = null;
        }

        _escritor?.Dispose();
        _escritor = null;

        _lector?.Dispose();
        _lector = null;

        _clienteTcp?.Dispose();
        _clienteTcp = null;

        _configuracionActual = null;

        _logger.LogInformation("Desconectado de APRS-IS");
    }

    /// <inheritdoc />
    public async Task EnviarPosicionAsync(Coordenadas coordenadas, string comentario, CancellationToken tokenCancelacion = default)
    {
        if (_escritor is null || _configuracionActual is null)
        {
            throw new InvalidOperationException("No hay conexión activa con APRS-IS. Llame a ConectarAsync primero.");
        }

        if (string.IsNullOrWhiteSpace(comentario))
        {
            comentario = "RadioAficionado";
        }

        string posicionFormateada = FormatearPosicion(coordenadas);
        string paquete = $"{_configuracionActual.Indicativo}>APRS,TCPIP*:={posicionFormateada}-{comentario}";

        await _escritor.WriteLineAsync(paquete.AsMemory(), tokenCancelacion).ConfigureAwait(false);
        _logger.LogInformation("Posición APRS enviada: {Lat}, {Lon}", coordenadas.Latitud, coordenadas.Longitud);
    }

    /// <inheritdoc />
    public async Task EnviarMensajeAsync(Indicativo destinatario, string texto, CancellationToken tokenCancelacion = default)
    {
        if (_escritor is null || _configuracionActual is null)
        {
            throw new InvalidOperationException("No hay conexión activa con APRS-IS. Llame a ConectarAsync primero.");
        }

        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new ArgumentException("El texto del mensaje no puede estar vacío.", nameof(texto));
        }

        _contadorMensajes++;
        string numeroMensaje = _contadorMensajes.ToString("D3");
        string destinatarioFormateado = destinatario.Valor.PadRight(9);

        string paquete = $"{_configuracionActual.Indicativo}>APRS,TCPIP*::{destinatarioFormateado}:{texto}{{{numeroMensaje}";

        await _escritor.WriteLineAsync(paquete.AsMemory(), tokenCancelacion).ConfigureAwait(false);
        _logger.LogInformation("Mensaje APRS enviado a {Destinatario}: {Texto}", destinatario.Valor, texto);
    }

    /// <summary>
    /// Bucle de recepción continua de paquetes del servidor APRS-IS.
    /// Lee líneas del stream TCP, las parsea, y dispara el evento <see cref="PaqueteRecibido"/>.
    /// </summary>
    /// <param name="tokenCancelacion">Token para cancelar la recepción.</param>
    private async Task RecibirPaquetesAsync(CancellationToken tokenCancelacion)
    {
        try
        {
            while (!tokenCancelacion.IsCancellationRequested && _lector is not null)
            {
                string? linea = await _lector.ReadLineAsync(tokenCancelacion).ConfigureAwait(false);

                if (linea is null)
                {
                    _logger.LogWarning("APRS-IS: conexión cerrada por el servidor");
                    break;
                }

                if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith('#'))
                {
                    continue;
                }

                PaqueteAprs? paquete = ParserAprs.ParsearPaquete(linea);
                if (paquete is not null)
                {
                    try
                    {
                        PaqueteRecibido?.Invoke(paquete);
                    }
                    catch (Exception excepcion)
                    {
                        _logger.LogError(excepcion, "Error en manejador de PaqueteRecibido");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Esperado al cancelar
        }
        catch (IOException excepcion)
        {
            _logger.LogError(excepcion, "Error de E/S en la recepción APRS-IS");
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "Error inesperado en la recepción APRS-IS");
        }
    }

    /// <summary>
    /// Construye el comando de login para el servidor APRS-IS.
    /// Formato: "user INDICATIVO pass PASSCODE vers SOFTWARE VERSION filter FILTRO"
    /// </summary>
    /// <param name="configuracion">Configuración de conexión.</param>
    /// <returns>Comando de login formateado.</returns>
    private static string ConstruirComandoLogin(ConfiguracionAprs configuracion)
    {
        string comando = $"user {configuracion.Indicativo} pass {configuracion.Passcode} vers {NombreSoftware} {VersionSoftware}";

        if (!string.IsNullOrWhiteSpace(configuracion.Filtro))
        {
            comando += $" filter {configuracion.Filtro}";
        }

        return comando;
    }

    /// <summary>
    /// Formatea coordenadas al formato de posición APRS sin comprimir.
    /// Formato: "DDMM.MMN/DDDMM.MMW"
    /// </summary>
    /// <param name="coordenadas">Coordenadas a formatear.</param>
    /// <returns>Cadena con la posición en formato APRS.</returns>
    private static string FormatearPosicion(Coordenadas coordenadas)
    {
        double latitudAbsoluta = Math.Abs(coordenadas.Latitud);
        int latitudGrados = (int)latitudAbsoluta;
        double latitudMinutos = (latitudAbsoluta - latitudGrados) * 60.0;
        char hemisferioNs = coordenadas.Latitud >= 0 ? 'N' : 'S';

        double longitudAbsoluta = Math.Abs(coordenadas.Longitud);
        int longitudGrados = (int)longitudAbsoluta;
        double longitudMinutos = (longitudAbsoluta - longitudGrados) * 60.0;
        char hemisferioEw = coordenadas.Longitud >= 0 ? 'E' : 'W';

        return $"{latitudGrados:D2}{latitudMinutos:00.00}{hemisferioNs}/{longitudGrados:D3}{longitudMinutos:00.00}{hemisferioEw}";
    }

    /// <summary>
    /// Libera los recursos utilizados por el cliente APRS-IS.
    /// </summary>
    /// <param name="disposing">True si se llama desde Dispose, false si se llama desde el finalizador.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _tokenCancelacionRecepcion?.Cancel();
                _tokenCancelacionRecepcion?.Dispose();
                _escritor?.Dispose();
                _lector?.Dispose();
                _clienteTcp?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
