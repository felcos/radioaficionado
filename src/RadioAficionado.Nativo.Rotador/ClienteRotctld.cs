using System.Globalization;
using System.Net.Sockets;
using System.Text;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Nativo.Rotador;

/// <summary>
/// Cliente TCP para el demonio rotctld (Hamlib).
/// Implementa el protocolo de texto de rotctld para controlar rotadores de antena.
/// </summary>
public sealed class ClienteRotctld : IControlRotador
{
    private readonly ConfiguracionRotador _configuracion;
    private readonly SemaphoreSlim _semaforoTcp = new(1, 1);

    private TcpClient? _clienteTcp;
    private NetworkStream? _flujoRed;
    private StreamReader? _lector;
    private StreamWriter? _escritor;

    private CancellationTokenSource? _ctsPolling;
    private Task? _tareaPolling;
    private PosicionRotador? _ultimaPosicion;

    /// <inheritdoc />
    public bool EstaConectado => _clienteTcp?.Connected == true;

    /// <inheritdoc />
    public bool SoportaElevacion { get; private set; }

    /// <inheritdoc />
    public event EventHandler<PosicionRotador>? PosicionCambiada;

    /// <summary>
    /// Crea una nueva instancia del cliente rotctld con la configuración por defecto.
    /// </summary>
    public ClienteRotctld()
        : this(new ConfiguracionRotador())
    {
    }

    /// <summary>
    /// Crea una nueva instancia del cliente rotctld con la configuración especificada.
    /// </summary>
    /// <param name="configuracion">Configuración de conexión y comportamiento.</param>
    public ClienteRotctld(ConfiguracionRotador configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        _configuracion = configuracion;
    }

    /// <inheritdoc />
    public async Task ConectarAsync(string host = "localhost", int puerto = 4533, CancellationToken ct = default)
    {
        if (EstaConectado)
        {
            return;
        }

        _clienteTcp = new TcpClient();
        _clienteTcp.ReceiveTimeout = _configuracion.TimeoutMs;
        _clienteTcp.SendTimeout = _configuracion.TimeoutMs;

        await _clienteTcp.ConnectAsync(host, puerto, ct).ConfigureAwait(false);

        _flujoRed = _clienteTcp.GetStream();
        _lector = new StreamReader(_flujoRed, Encoding.ASCII);
        _escritor = new StreamWriter(_flujoRed, Encoding.ASCII) { AutoFlush = true };

        await DetectarCapacidadesAsync(ct).ConfigureAwait(false);

        _ctsPolling = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _tareaPolling = EjecutarPollingAsync(_ctsPolling.Token);
    }

    /// <inheritdoc />
    public async Task DesconectarAsync(CancellationToken ct = default)
    {
        if (_ctsPolling is not null)
        {
            await _ctsPolling.CancelAsync().ConfigureAwait(false);
        }

        if (_tareaPolling is not null)
        {
            try
            {
                await _tareaPolling.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Esperado al cancelar el polling.
            }
        }

        LiberarConexion();
    }

    /// <inheritdoc />
    public async Task<PosicionRotador> ObtenerPosicionAsync(CancellationToken ct = default)
    {
        VerificarConexion();

        string respuesta = await EnviarComandoAsync("p\n", 2, ct).ConfigureAwait(false);
        return ParsearPosicion(respuesta);
    }

    /// <inheritdoc />
    public async Task MoverAsync(PosicionRotador posicion, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(posicion);
        VerificarConexion();

        string azimutTexto = posicion.Azimut.ToString("F1", CultureInfo.InvariantCulture);
        string elevacionTexto = posicion.Elevacion.ToString("F1", CultureInfo.InvariantCulture);
        string comando = $"P {azimutTexto} {elevacionTexto}\n";

        string respuesta = await EnviarComandoAsync(comando, 1, ct).ConfigureAwait(false);
        VerificarRespuestaRprt(respuesta);
    }

    /// <inheritdoc />
    public async Task DetenerAsync(CancellationToken ct = default)
    {
        VerificarConexion();

        string respuesta = await EnviarComandoAsync("S\n", 1, ct).ConfigureAwait(false);
        VerificarRespuestaRprt(respuesta);
    }

    /// <inheritdoc />
    public async Task EstacionarAsync(CancellationToken ct = default)
    {
        VerificarConexion();

        string respuesta = await EnviarComandoAsync("K 0.0\n", 1, ct).ConfigureAwait(false);
        VerificarRespuestaRprt(respuesta);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DesconectarAsync().ConfigureAwait(false);
        _semaforoTcp.Dispose();
    }

    /// <summary>
    /// Envía un comando al demonio rotctld y lee el número esperado de líneas de respuesta.
    /// Protegido con semáforo para seguridad de hilos.
    /// </summary>
    /// <param name="comando">Comando de texto a enviar (debe terminar en \n).</param>
    /// <param name="lineasEsperadas">Cantidad de líneas que se esperan en la respuesta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Respuesta completa del demonio con líneas separadas por \n.</returns>
    private async Task<string> EnviarComandoAsync(string comando, int lineasEsperadas, CancellationToken ct)
    {
        await _semaforoTcp.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _escritor!.WriteAsync(comando.AsMemory(), ct).ConfigureAwait(false);

            StringBuilder respuesta = new();
            for (int i = 0; i < lineasEsperadas; i++)
            {
                string? linea = await _lector!.ReadLineAsync(ct).ConfigureAwait(false);
                if (linea is null)
                {
                    throw new IOException("La conexión con rotctld se cerró inesperadamente.");
                }

                if (i > 0)
                {
                    respuesta.Append('\n');
                }

                respuesta.Append(linea);
            }

            return respuesta.ToString();
        }
        finally
        {
            _semaforoTcp.Release();
        }
    }

    /// <summary>
    /// Detecta las capacidades del rotador enviando el comando de información.
    /// Determina si el rotador soporta elevación (AZ/EL).
    /// </summary>
    private async Task DetectarCapacidadesAsync(CancellationToken ct)
    {
        try
        {
            // El comando _ devuelve información del modelo del rotador.
            // Leemos líneas hasta encontrar una vacía o un error.
            await _semaforoTcp.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await _escritor!.WriteAsync("_\n".AsMemory(), ct).ConfigureAwait(false);

                StringBuilder informacion = new();
                // Leer líneas de respuesta. rotctld devuelve varias líneas de info.
                // Usamos un timeout corto para no bloquear si hay pocas líneas.
                using CancellationTokenSource ctsLectura = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ctsLectura.CancelAfter(_configuracion.TimeoutMs);

                try
                {
                    while (true)
                    {
                        string? linea = await _lector!.ReadLineAsync(ctsLectura.Token).ConfigureAwait(false);
                        if (linea is null)
                        {
                            break;
                        }

                        informacion.AppendLine(linea);

                        // Si recibimos RPRT, es la última línea.
                        if (linea.StartsWith("RPRT", StringComparison.Ordinal))
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Timeout leyendo info — continuamos con lo que tenemos.
                }

                string infoCompleta = informacion.ToString();
                // Si la info menciona elevación o el modelo soporta AZ/EL, activar.
                SoportaElevacion = infoCompleta.Contains("EL", StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                _semaforoTcp.Release();
            }
        }
        catch (Exception)
        {
            // Si falla la detección de capacidades, asumimos solo azimut.
            SoportaElevacion = false;
        }
    }

    /// <summary>
    /// Bucle de polling que lee la posición del rotador periódicamente
    /// y dispara el evento <see cref="PosicionCambiada"/> cuando hay un cambio significativo.
    /// </summary>
    private async Task EjecutarPollingAsync(CancellationToken ct)
    {
        using PeriodicTimer temporizador = new(TimeSpan.FromMilliseconds(_configuracion.IntervaloPollingMs));

        try
        {
            while (await temporizador.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (!EstaConectado)
                {
                    break;
                }

                try
                {
                    PosicionRotador posicionActual = await ObtenerPosicionAsync(ct).ConfigureAwait(false);
                    NotificarSiCambio(posicionActual);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Error transitorio leyendo posición — se reintenta en el siguiente tick.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Polling cancelado — salida normal.
        }
    }

    /// <summary>
    /// Compara la posición actual con la última conocida y dispara el evento si el cambio
    /// supera el umbral configurado.
    /// </summary>
    /// <param name="posicionActual">Posición recién leída del rotador.</param>
    private void NotificarSiCambio(PosicionRotador posicionActual)
    {
        double umbral = _configuracion.UmbralCambioGrados;

        if (_ultimaPosicion is null
            || Math.Abs(posicionActual.Azimut - _ultimaPosicion.Azimut) > umbral
            || Math.Abs(posicionActual.Elevacion - _ultimaPosicion.Elevacion) > umbral)
        {
            _ultimaPosicion = posicionActual;
            PosicionCambiada?.Invoke(this, posicionActual);
        }
    }

    /// <summary>
    /// Parsea la respuesta del comando 'p' (get position) en dos líneas: azimut y elevación.
    /// </summary>
    /// <param name="respuesta">Respuesta cruda del demonio rotctld (dos valores separados por \n).</param>
    /// <returns>Posición del rotador parseada.</returns>
    private static PosicionRotador ParsearPosicion(string respuesta)
    {
        string[] partes = respuesta.Split('\n');
        if (partes.Length < 2)
        {
            throw new FormatException($"Respuesta de posición inesperada de rotctld: '{respuesta}'");
        }

        if (!double.TryParse(partes[0].Trim(), CultureInfo.InvariantCulture, out double azimut))
        {
            throw new FormatException($"No se pudo parsear el azimut: '{partes[0]}'");
        }

        if (!double.TryParse(partes[1].Trim(), CultureInfo.InvariantCulture, out double elevacion))
        {
            throw new FormatException($"No se pudo parsear la elevación: '{partes[1]}'");
        }

        return new PosicionRotador(azimut, elevacion);
    }

    /// <summary>
    /// Verifica que la respuesta RPRT del demonio sea exitosa (código 0).
    /// </summary>
    /// <param name="respuesta">Línea de respuesta RPRT del demonio.</param>
    private static void VerificarRespuestaRprt(string respuesta)
    {
        if (!respuesta.StartsWith("RPRT 0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"El demonio rotctld devolvió un error: '{respuesta}'");
        }
    }

    /// <summary>
    /// Verifica que la conexión TCP esté activa antes de enviar un comando.
    /// </summary>
    private void VerificarConexion()
    {
        if (!EstaConectado)
        {
            throw new InvalidOperationException("No hay conexión activa con el demonio rotctld. Llame a ConectarAsync primero.");
        }
    }

    /// <summary>
    /// Libera los recursos de la conexión TCP (streams y socket).
    /// </summary>
    private void LiberarConexion()
    {
        _escritor?.Dispose();
        _escritor = null;

        _lector?.Dispose();
        _lector = null;

        _flujoRed?.Dispose();
        _flujoRed = null;

        _clienteTcp?.Dispose();
        _clienteTcp = null;

        _ctsPolling?.Dispose();
        _ctsPolling = null;

        _ultimaPosicion = null;
    }
}
