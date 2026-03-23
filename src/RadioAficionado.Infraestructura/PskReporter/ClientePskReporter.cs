using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using Serilog;

namespace RadioAficionado.Infraestructura.PskReporter;

/// <summary>
/// Cliente HTTP para enviar y consultar spots de señales digitales en PSKReporter.
/// Acumula spots internamente y los envía en batch según el intervalo configurado.
/// Implementa el protocolo XML de reporte y la API JSON de consulta de PSKReporter.
/// </summary>
public sealed class ClientePskReporter : IPskReporter
{
    private readonly ILogger _logger;
    private readonly HttpClient _clienteHttp;
    private readonly ConfiguracionPskReporter _configuracion;
    private readonly ConcurrentQueue<SpotPsk> _bufferSpots = new();
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private CancellationTokenSource? _ctsEnvio;
    private Task? _tareaEnvio;
    private bool _disposed;

    /// <inheritdoc />
    public event Action<IReadOnlyList<SpotPsk>>? SpotsConsultados;

    /// <summary>
    /// Crea una nueva instancia del cliente PSKReporter.
    /// </summary>
    /// <param name="logger">Logger de Serilog para registrar la actividad.</param>
    /// <param name="clienteHttp">Cliente HTTP para las peticiones a PSKReporter.</param>
    /// <param name="configuracion">Configuración del cliente PSKReporter.</param>
    /// <exception cref="ArgumentNullException">Si algún parámetro es null.</exception>
    public ClientePskReporter(ILogger logger, HttpClient clienteHttp, ConfiguracionPskReporter configuracion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clienteHttp = clienteHttp ?? throw new ArgumentNullException(nameof(clienteHttp));
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
    }

    /// <summary>
    /// Crea una nueva instancia del cliente PSKReporter con logger por defecto y HttpClient nuevo.
    /// </summary>
    /// <param name="configuracion">Configuración del cliente PSKReporter.</param>
    public ClientePskReporter(ConfiguracionPskReporter configuracion)
        : this(Log.Logger, new HttpClient(), configuracion)
    {
    }

    /// <inheritdoc />
    public async Task EnviarSpotsAsync(IReadOnlyList<SpotPsk> spots, CancellationToken ct = default)
    {
        if (spots is null)
        {
            throw new ArgumentNullException(nameof(spots));
        }

        foreach (SpotPsk spot in spots)
        {
            _bufferSpots.Enqueue(spot);
        }

        _logger.Information("Se añadieron {Cantidad} spots al buffer de PSKReporter. Total en buffer: {Total}",
            spots.Count, _bufferSpots.Count);

        // Si no hay tarea de envío periódico activa, iniciarla
        if (_tareaEnvio is null || _tareaEnvio.IsCompleted)
        {
            IniciarEnvioPeriodico();
        }

        // Si el buffer tiene suficientes spots, forzar un envío inmediato
        if (_bufferSpots.Count >= 50)
        {
            await VaciarBufferAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpotPsk>> ObtenerSpotsAsync(Indicativo indicativo, TimeSpan periodo, CancellationToken ct = default)
    {
        int segundos = (int)periodo.TotalSeconds;

        string url = $"{_configuracion.UrlConsulta}?senderCallsign={indicativo.Valor}" +
                     $"&flowStartSeconds=-{segundos}" +
                     $"&rronly=true" +
                     $"&noactive=true" +
                     $"&callback=";

        _logger.Information("Consultando spots de PSKReporter para {Indicativo} en los últimos {Segundos}s",
            indicativo.Valor, segundos);

        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(url, ct).ConfigureAwait(false);
        respuesta.EnsureSuccessStatusCode();

        string json = await respuesta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        IReadOnlyList<SpotPsk> spots = ParsearRespuestaJson(json);

        _logger.Information("Se obtuvieron {Cantidad} spots de PSKReporter para {Indicativo}",
            spots.Count, indicativo.Valor);

        SpotsConsultados?.Invoke(spots);

        return spots;
    }

    /// <summary>
    /// Genera el XML de reporte para PSKReporter a partir de una lista de spots.
    /// Sigue el formato estándar aceptado por el endpoint HPHpreceiver.cgi.
    /// </summary>
    /// <param name="spots">Lista de spots a incluir en el reporte.</param>
    /// <returns>Cadena XML con el reporte formateado.</returns>
    public string GenerarXml(IReadOnlyList<SpotPsk> spots)
    {
        if (spots is null)
        {
            throw new ArgumentNullException(nameof(spots));
        }

        XElement raiz = new("receptionReports");

        raiz.Add(new XElement("receiverCallsign",
            new XAttribute("value", _configuracion.IndicativoPropio)));

        if (!string.IsNullOrWhiteSpace(_configuracion.Localizador))
        {
            raiz.Add(new XElement("receiverLocator",
                new XAttribute("value", _configuracion.Localizador)));
        }

        raiz.Add(new XElement("receiverDecoderSoftware",
            new XAttribute("value", $"{_configuracion.SoftwareId} {_configuracion.VersionSoftware}")));

        foreach (SpotPsk spot in spots)
        {
            XElement elementoReporte = new("receptionReport",
                new XAttribute("senderCallsign", spot.Transmisor.Valor),
                new XAttribute("frequency", spot.Frecuencia.Hz.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("mode", spot.Modo.ToString()),
                new XAttribute("sNR", spot.Snr.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("flowStartSeconds",
                    ((DateTimeOffset)spot.Hora).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));

            if (spot.LocalizadorTransmisor.HasValue)
            {
                elementoReporte.Add(new XAttribute("senderLocator", spot.LocalizadorTransmisor.Value.Valor));
            }

            raiz.Add(elementoReporte);
        }

        XDocument documento = new(new XDeclaration("1.0", "utf-8", null), raiz);
        StringBuilder sb = new();
        using (System.IO.StringWriter escritor = new(sb))
        {
            documento.Save(escritor);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parsea la respuesta JSON de la API de consulta de PSKReporter y extrae los spots.
    /// </summary>
    /// <param name="json">Cadena JSON de la respuesta de PSKReporter.</param>
    /// <returns>Lista de spots parseados.</returns>
    public IReadOnlyList<SpotPsk> ParsearRespuestaJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<SpotPsk>();
        }

        List<SpotPsk> spots = new();

        try
        {
            using JsonDocument documento = JsonDocument.Parse(json);
            JsonElement raiz = documento.RootElement;

            if (!raiz.TryGetProperty("receptionReport", out JsonElement reportes))
            {
                return Array.Empty<SpotPsk>();
            }

            foreach (JsonElement reporte in reportes.EnumerateArray())
            {
                SpotPsk? spot = ParsearSpotDesdeJson(reporte);
                if (spot is not null)
                {
                    spots.Add(spot);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.Warning(ex, "Error al parsear la respuesta JSON de PSKReporter");
        }

        return spots.AsReadOnly();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_ctsEnvio is not null)
        {
            await _ctsEnvio.CancelAsync().ConfigureAwait(false);

            if (_tareaEnvio is not null)
            {
                try
                {
                    await _tareaEnvio.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Esperado al cancelar.
                }
            }

            _ctsEnvio.Dispose();
        }

        // Intentar enviar los spots restantes del buffer
        try
        {
            await VaciarBufferAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error al vaciar el buffer de PSKReporter durante el dispose");
        }

        _semaforo.Dispose();
    }

    /// <summary>
    /// Inicia la tarea de envío periódico de spots acumulados en el buffer.
    /// </summary>
    private void IniciarEnvioPeriodico()
    {
        _ctsEnvio?.Dispose();
        _ctsEnvio = new CancellationTokenSource();
        CancellationToken ct = _ctsEnvio.Token;

        _tareaEnvio = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_configuracion.IntervaloEnvioSegundos),
                        ct).ConfigureAwait(false);

                    await VaciarBufferAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error en el envío periódico de spots a PSKReporter");
                }
            }
        }, ct);
    }

    /// <summary>
    /// Vacía el buffer de spots acumulados y los envía a PSKReporter via HTTP POST.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    private async Task VaciarBufferAsync(CancellationToken ct)
    {
        if (_bufferSpots.IsEmpty)
        {
            return;
        }

        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            List<SpotPsk> spotsAEnviar = new();

            while (_bufferSpots.TryDequeue(out SpotPsk? spot))
            {
                spotsAEnviar.Add(spot);
            }

            if (spotsAEnviar.Count == 0)
            {
                return;
            }

            string xml = GenerarXml(spotsAEnviar);

            _logger.Debug("Enviando {Cantidad} spots a PSKReporter:\n{Xml}", spotsAEnviar.Count, xml);

            using StringContent contenido = new(xml, Encoding.UTF8, "application/xml");
            HttpResponseMessage respuesta = await _clienteHttp.PostAsync(
                _configuracion.UrlEnvio, contenido, ct).ConfigureAwait(false);

            respuesta.EnsureSuccessStatusCode();

            _logger.Information("Enviados {Cantidad} spots a PSKReporter exitosamente", spotsAEnviar.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error al enviar spots a PSKReporter");
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>
    /// Parsea un elemento JSON individual de la respuesta de PSKReporter y lo convierte en un SpotPsk.
    /// </summary>
    /// <param name="elemento">Elemento JSON del reporte.</param>
    /// <returns>Un SpotPsk si el elemento se pudo parsear correctamente, o null en caso contrario.</returns>
    private SpotPsk? ParsearSpotDesdeJson(JsonElement elemento)
    {
        try
        {
            string indicativoReceptor = elemento.TryGetProperty("receiverCallsign", out JsonElement propReceptor)
                ? propReceptor.GetString() ?? string.Empty
                : string.Empty;

            string indicativoTransmisor = elemento.TryGetProperty("senderCallsign", out JsonElement propTransmisor)
                ? propTransmisor.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(indicativoReceptor) || string.IsNullOrWhiteSpace(indicativoTransmisor))
            {
                return null;
            }

            long frecuenciaHz = 0;
            if (elemento.TryGetProperty("frequency", out JsonElement propFrecuencia))
            {
                if (propFrecuencia.ValueKind == JsonValueKind.Number)
                {
                    frecuenciaHz = propFrecuencia.GetInt64();
                }
                else if (propFrecuencia.ValueKind == JsonValueKind.String)
                {
                    long.TryParse(propFrecuencia.GetString(), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out frecuenciaHz);
                }
            }

            if (frecuenciaHz <= 0)
            {
                return null;
            }

            string modoTexto = elemento.TryGetProperty("mode", out JsonElement propModo)
                ? propModo.GetString() ?? string.Empty
                : string.Empty;

            if (!ModoOperacionExtensiones.IntentarDesdeAdif(modoTexto, out ModoOperacion modo))
            {
                return null;
            }

            int snr = 0;
            if (elemento.TryGetProperty("sNR", out JsonElement propSnr))
            {
                if (propSnr.ValueKind == JsonValueKind.Number)
                {
                    snr = propSnr.GetInt32();
                }
                else if (propSnr.ValueKind == JsonValueKind.String)
                {
                    int.TryParse(propSnr.GetString(), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out snr);
                }
            }

            Localizador? localizadorReceptor = null;
            if (elemento.TryGetProperty("receiverLocator", out JsonElement propLocReceptor))
            {
                string locTexto = propLocReceptor.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(locTexto))
                {
                    try
                    {
                        localizadorReceptor = new Localizador(locTexto);
                    }
                    catch (ArgumentException)
                    {
                        // Localizador inválido, se ignora.
                    }
                }
            }

            Localizador? localizadorTransmisor = null;
            if (elemento.TryGetProperty("senderLocator", out JsonElement propLocTransmisor))
            {
                string locTexto = propLocTransmisor.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(locTexto))
                {
                    try
                    {
                        localizadorTransmisor = new Localizador(locTexto);
                    }
                    catch (ArgumentException)
                    {
                        // Localizador inválido, se ignora.
                    }
                }
            }

            long horaUnix = 0;
            if (elemento.TryGetProperty("flowStartSeconds", out JsonElement propHora))
            {
                if (propHora.ValueKind == JsonValueKind.Number)
                {
                    horaUnix = propHora.GetInt64();
                }
                else if (propHora.ValueKind == JsonValueKind.String)
                {
                    long.TryParse(propHora.GetString(), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out horaUnix);
                }
            }

            DateTime hora = horaUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(horaUnix).UtcDateTime
                : DateTime.UtcNow;

            return new SpotPsk(
                new Indicativo(indicativoReceptor),
                new Indicativo(indicativoTransmisor),
                Frecuencia.DesdeHz(frecuenciaHz),
                modo,
                snr,
                localizadorReceptor,
                localizadorTransmisor,
                hora);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error al parsear un spot individual de PSKReporter");
            return null;
        }
    }
}
