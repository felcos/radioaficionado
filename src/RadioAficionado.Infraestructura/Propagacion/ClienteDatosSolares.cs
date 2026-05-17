using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Propagacion;
using Serilog;

namespace RadioAficionado.Infraestructura.Propagacion;

/// <summary>
/// Cliente que consume datos solares reales de NOAA SWPC (JSON) y N0NBH HAMQSL (XML).
/// Implementa caché en memoria para minimizar peticiones a las fuentes externas.
/// </summary>
public sealed class ClienteDatosSolares : IClienteDatosSolares
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    // URLs de NOAA SWPC
    private const string UrlFlujo10cm = "https://services.swpc.noaa.gov/products/summary/10cm-flux.json";
    private const string UrlVientoSolar = "https://services.swpc.noaa.gov/products/summary/solar-wind-speed.json";
    private const string UrlCampoMagnetico = "https://services.swpc.noaa.gov/products/summary/solar-wind-mag-field.json";
    private const string UrlIndicePlanetarioKp = "https://services.swpc.noaa.gov/products/noaa-planetary-k-index.json";
    private const string UrlFlujo30Dias = "https://services.swpc.noaa.gov/products/10cm-flux-30-day.json";
    private const string UrlEscalas = "https://services.swpc.noaa.gov/products/noaa-scales.json";
    private const string UrlAlertas = "https://services.swpc.noaa.gov/products/alerts.json";

    // URL de N0NBH
    private const string UrlN0nbh = "https://hamqsl.com/solarxml.php";

    // Caché en memoria
    private DatosSolaresCompletos? _cacheDatosCompletos;
    private DateTime _ultimaActualizacionDatosCompletos = DateTime.MinValue;
    private readonly TimeSpan _ttlDatosCompletos = TimeSpan.FromMinutes(5);

    private IReadOnlyList<PuntoHistoricoSfi>? _cacheHistoricoSfi;
    private DateTime _ultimaActualizacionHistoricoSfi = DateTime.MinValue;

    private IReadOnlyList<PuntoHistoricoKp>? _cacheHistoricoKp;
    private DateTime _ultimaActualizacionHistoricoKp = DateTime.MinValue;
    private readonly TimeSpan _ttlHistorico = TimeSpan.FromMinutes(30);

    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Crea una nueva instancia del cliente de datos solares.
    /// </summary>
    /// <param name="httpClient">HttpClient configurado para peticiones externas.</param>
    /// <param name="logger">Logger de Serilog.</param>
    public ClienteDatosSolares(HttpClient httpClient, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DatosSolaresCompletos> ObtenerDatosSolaresCompletosAsync(CancellationToken ct = default)
    {
        if (_cacheDatosCompletos is not null && DateTime.UtcNow - _ultimaActualizacionDatosCompletos < _ttlDatosCompletos)
        {
            _logger.Debug("Retornando datos solares completos desde caché");
            return _cacheDatosCompletos;
        }

        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Doble comprobación tras adquirir el semáforo
            if (_cacheDatosCompletos is not null && DateTime.UtcNow - _ultimaActualizacionDatosCompletos < _ttlDatosCompletos)
            {
                return _cacheDatosCompletos;
            }

            _logger.Information("Consultando datos solares de NOAA y N0NBH");

            DatosSolaresCompletos datos = await ConsultarYCombinarFuentesAsync(ct).ConfigureAwait(false);
            _cacheDatosCompletos = datos;
            _ultimaActualizacionDatosCompletos = DateTime.UtcNow;
            return datos;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PuntoHistoricoSfi>> ObtenerHistoricoSfiAsync(CancellationToken ct = default)
    {
        if (_cacheHistoricoSfi is not null && DateTime.UtcNow - _ultimaActualizacionHistoricoSfi < _ttlHistorico)
        {
            _logger.Debug("Retornando histórico SFI desde caché");
            return _cacheHistoricoSfi;
        }

        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cacheHistoricoSfi is not null && DateTime.UtcNow - _ultimaActualizacionHistoricoSfi < _ttlHistorico)
            {
                return _cacheHistoricoSfi;
            }

            _logger.Information("Consultando histórico SFI de 30 días de NOAA");

            try
            {
                string json = await _httpClient.GetStringAsync(UrlFlujo30Dias, ct).ConfigureAwait(false);
                IReadOnlyList<PuntoHistoricoSfi> historico = ParsearHistoricoSfi(json);
                _cacheHistoricoSfi = historico;
                _ultimaActualizacionHistoricoSfi = DateTime.UtcNow;
                return historico;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Error(ex, "Error al obtener histórico SFI de NOAA, retornando lista vacía");
                return Array.Empty<PuntoHistoricoSfi>();
            }
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PuntoHistoricoKp>> ObtenerHistoricoKpAsync(CancellationToken ct = default)
    {
        if (_cacheHistoricoKp is not null && DateTime.UtcNow - _ultimaActualizacionHistoricoKp < _ttlHistorico)
        {
            _logger.Debug("Retornando histórico Kp desde caché");
            return _cacheHistoricoKp;
        }

        await _semaforo.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cacheHistoricoKp is not null && DateTime.UtcNow - _ultimaActualizacionHistoricoKp < _ttlHistorico)
            {
                return _cacheHistoricoKp;
            }

            _logger.Information("Consultando histórico Kp de 7 días de NOAA");

            try
            {
                string json = await _httpClient.GetStringAsync(UrlIndicePlanetarioKp, ct).ConfigureAwait(false);
                IReadOnlyList<PuntoHistoricoKp> historico = ParsearHistoricoKp(json);
                _cacheHistoricoKp = historico;
                _ultimaActualizacionHistoricoKp = DateTime.UtcNow;
                return historico;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Error(ex, "Error al obtener histórico Kp de NOAA, retornando lista vacía");
                return Array.Empty<PuntoHistoricoKp>();
            }
        }
        finally
        {
            _semaforo.Release();
        }
    }

    private async Task<DatosSolaresCompletos> ConsultarYCombinarFuentesAsync(CancellationToken ct)
    {
        // Intentar obtener datos de ambas fuentes en paralelo
        Task<DatosNoaa?> tareaNoaa = ObtenerDatosNoaaAsync(ct);
        Task<DatosN0nbh?> tareaN0nbh = ObtenerDatosN0nbhAsync(ct);

        await Task.WhenAll(tareaNoaa, tareaN0nbh).ConfigureAwait(false);

        DatosNoaa? datosNoaa = await tareaNoaa.ConfigureAwait(false);
        DatosN0nbh? datosN0nbh = await tareaN0nbh.ConfigureAwait(false);

        if (datosNoaa is null && datosN0nbh is null)
        {
            _logger.Warning("Ambas fuentes (NOAA y N0NBH) fallaron, retornando datos de respaldo");
            return CrearDatosDeRespaldo();
        }

        return CombinarFuentes(datosNoaa, datosN0nbh);
    }

    private async Task<DatosNoaa?> ObtenerDatosNoaaAsync(CancellationToken ct)
    {
        try
        {
            // Lanzar todas las peticiones NOAA en paralelo
            Task<string> tareaFlujo = _httpClient.GetStringAsync(UrlFlujo10cm, ct);
            Task<string> tareaViento = _httpClient.GetStringAsync(UrlVientoSolar, ct);
            Task<string> tareaCampo = _httpClient.GetStringAsync(UrlCampoMagnetico, ct);
            Task<string> tareaKp = _httpClient.GetStringAsync(UrlIndicePlanetarioKp, ct);
            Task<string> tareaEscalas = _httpClient.GetStringAsync(UrlEscalas, ct);
            Task<string> tareaAlertas = _httpClient.GetStringAsync(UrlAlertas, ct);

            await Task.WhenAll(tareaFlujo, tareaViento, tareaCampo, tareaKp, tareaEscalas, tareaAlertas)
                .ConfigureAwait(false);

            string jsonFlujo = await tareaFlujo.ConfigureAwait(false);
            string jsonViento = await tareaViento.ConfigureAwait(false);
            string jsonCampo = await tareaCampo.ConfigureAwait(false);
            string jsonKp = await tareaKp.ConfigureAwait(false);
            string jsonEscalas = await tareaEscalas.ConfigureAwait(false);
            string jsonAlertas = await tareaAlertas.ConfigureAwait(false);

            return new DatosNoaa
            {
                Sfi = ParsearFlujo10cm(jsonFlujo),
                VelocidadVientoSolar = ParsearVelocidadVientoSolar(jsonViento),
                Bt = ParsearBt(jsonCampo),
                BzGsm = ParsearBzGsm(jsonCampo),
                Kp = ParsearUltimoKp(jsonKp),
                Escalas = ParsearEscalas(jsonEscalas),
                Alertas = ParsearAlertas(jsonAlertas)
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error(ex, "Error al obtener datos de NOAA SWPC");
            return null;
        }
    }

    private async Task<DatosN0nbh?> ObtenerDatosN0nbhAsync(CancellationToken ct)
    {
        try
        {
            string xml = await _httpClient.GetStringAsync(UrlN0nbh, ct).ConfigureAwait(false);
            return ParsearXmlN0nbh(xml);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error(ex, "Error al obtener datos de N0NBH HAMQSL");
            return null;
        }
    }

    // ─── Parseo de JSON de NOAA ─────────────────────────────────────────

    /// <summary>
    /// Parsea la respuesta JSON de flujo solar de 10 cm de NOAA.
    /// </summary>
    internal static int ParsearFlujo10cm(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;

        if (raiz.ValueKind == JsonValueKind.Array && raiz.GetArrayLength() > 0)
        {
            JsonElement primer = raiz[0];
            if (primer.TryGetProperty("flux", out JsonElement flujo))
            {
                return flujo.ValueKind == JsonValueKind.Number
                    ? flujo.GetInt32()
                    : int.TryParse(flujo.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int valor) ? valor : 0;
            }
        }

        // Formato objeto directo
        if (raiz.TryGetProperty("flux", out JsonElement flujoDirecto))
        {
            return flujoDirecto.ValueKind == JsonValueKind.Number
                ? flujoDirecto.GetInt32()
                : int.TryParse(flujoDirecto.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int valor) ? valor : 0;
        }

        return 0;
    }

    /// <summary>
    /// Parsea la velocidad del viento solar desde la respuesta JSON de NOAA.
    /// </summary>
    internal static double ParsearVelocidadVientoSolar(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;

        JsonElement objetivo = raiz.ValueKind == JsonValueKind.Array && raiz.GetArrayLength() > 0
            ? raiz[0]
            : raiz;

        if (objetivo.TryGetProperty("proton_speed", out JsonElement velocidad))
        {
            return velocidad.ValueKind == JsonValueKind.Number
                ? velocidad.GetDouble()
                : double.TryParse(velocidad.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) ? valor : 0;
        }

        // Alternativa: WindSpeed
        if (objetivo.TryGetProperty("WindSpeed", out JsonElement windSpeed))
        {
            return windSpeed.ValueKind == JsonValueKind.Number
                ? windSpeed.GetDouble()
                : double.TryParse(windSpeed.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) ? valor : 0;
        }

        return 0;
    }

    /// <summary>
    /// Parsea el campo magnético total Bt desde la respuesta JSON de NOAA.
    /// </summary>
    internal static double ParsearBt(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;

        JsonElement objetivo = raiz.ValueKind == JsonValueKind.Array && raiz.GetArrayLength() > 0
            ? raiz[0]
            : raiz;

        if (objetivo.TryGetProperty("bt", out JsonElement bt))
        {
            return bt.ValueKind == JsonValueKind.Number
                ? bt.GetDouble()
                : double.TryParse(bt.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) ? valor : 0;
        }

        return 0;
    }

    /// <summary>
    /// Parsea la componente Bz GSM del campo magnético interplanetario.
    /// </summary>
    internal static double ParsearBzGsm(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;

        JsonElement objetivo = raiz.ValueKind == JsonValueKind.Array && raiz.GetArrayLength() > 0
            ? raiz[0]
            : raiz;

        if (objetivo.TryGetProperty("bz_gsm", out JsonElement bz))
        {
            return bz.ValueKind == JsonValueKind.Number
                ? bz.GetDouble()
                : double.TryParse(bz.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) ? valor : 0;
        }

        return 0;
    }

    /// <summary>
    /// Parsea el último valor de Kp del array de índice planetario de NOAA.
    /// </summary>
    internal static double ParsearUltimoKp(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;

        if (raiz.ValueKind == JsonValueKind.Array && raiz.GetArrayLength() > 1)
        {
            // El primer elemento es el header, los datos empiezan desde el índice 1
            // Tomamos el último elemento (más reciente)
            JsonElement ultimo = raiz[raiz.GetArrayLength() - 1];

            if (ultimo.ValueKind == JsonValueKind.Array && ultimo.GetArrayLength() > 1)
            {
                // Formato array: [time_tag, Kp, a_running, station_count]
                JsonElement valorKp = ultimo[1];
                return valorKp.ValueKind == JsonValueKind.Number
                    ? valorKp.GetDouble()
                    : double.TryParse(valorKp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) ? valor : 0;
            }

            if (ultimo.ValueKind == JsonValueKind.Object && ultimo.TryGetProperty("Kp", out JsonElement kp))
            {
                return kp.ValueKind == JsonValueKind.Number
                    ? kp.GetDouble()
                    : double.TryParse(kp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) ? valor : 0;
            }
        }

        return 0;
    }

    /// <summary>
    /// Parsea las escalas NOAA de clima espacial.
    /// </summary>
    internal static EscalasEspaciales ParsearEscalas(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;

        JsonElement objetivo = raiz.ValueKind == JsonValueKind.Array && raiz.GetArrayLength() > 0
            ? raiz[0]
            : raiz;

        string escalaR = "0";
        string escalaS = "0";
        string escalaG = "0";
        int probMenor = 0;
        int probMayor = 0;
        int probTormenta = 0;

        if (objetivo.TryGetProperty("R", out JsonElement r))
        {
            escalaR = ObtenerValorTexto(r, "Scale") ?? "0";
            int.TryParse(ObtenerValorTexto(r, "MinorProb"), NumberStyles.Any, CultureInfo.InvariantCulture, out probMenor);
            int.TryParse(ObtenerValorTexto(r, "MajorProb"), NumberStyles.Any, CultureInfo.InvariantCulture, out probMayor);
        }

        if (objetivo.TryGetProperty("S", out JsonElement s))
        {
            escalaS = ObtenerValorTexto(s, "Scale") ?? "0";
            int.TryParse(ObtenerValorTexto(s, "Prob"), NumberStyles.Any, CultureInfo.InvariantCulture, out probTormenta);
        }

        if (objetivo.TryGetProperty("G", out JsonElement g))
        {
            escalaG = ObtenerValorTexto(g, "Scale") ?? "0";
        }

        return new EscalasEspaciales(escalaR, escalaS, escalaG, probMenor, probMayor, probTormenta);
    }

    /// <summary>
    /// Parsea las alertas solares activas de NOAA.
    /// </summary>
    internal static IReadOnlyList<AlertaSolar> ParsearAlertas(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;

        List<AlertaSolar> alertas = new();

        if (raiz.ValueKind != JsonValueKind.Array)
        {
            return alertas.AsReadOnly();
        }

        foreach (JsonElement elemento in raiz.EnumerateArray())
        {
            string codigo = ObtenerValorTexto(elemento, "product_id") ?? "UNKNOWN";
            string mensaje = ObtenerValorTexto(elemento, "message") ?? "";
            string? fechaTexto = ObtenerValorTexto(elemento, "issue_datetime");

            DateTime fechaEmision = DateTime.TryParse(fechaTexto, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime fecha)
                ? fecha
                : DateTime.UtcNow;

            alertas.Add(new AlertaSolar(codigo, mensaje, fechaEmision));
        }

        return alertas.AsReadOnly();
    }

    /// <summary>
    /// Parsea el histórico de flujo solar de 30 días.
    /// Formato esperado: array de arrays donde el primer elemento es el header.
    /// </summary>
    internal static IReadOnlyList<PuntoHistoricoSfi> ParsearHistoricoSfi(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;
        List<PuntoHistoricoSfi> puntos = new();

        if (raiz.ValueKind != JsonValueKind.Array)
        {
            return puntos.AsReadOnly();
        }

        // Saltamos el header (primer elemento)
        bool esHeader = true;
        foreach (JsonElement elemento in raiz.EnumerateArray())
        {
            if (esHeader)
            {
                esHeader = false;
                continue;
            }

            if (elemento.ValueKind == JsonValueKind.Array && elemento.GetArrayLength() >= 2)
            {
                string? fechaTexto = elemento[0].GetString();
                DateTime fecha = DateTime.TryParse(fechaTexto, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime f) ? f : DateTime.UtcNow;

                int sfi = elemento[1].ValueKind == JsonValueKind.Number
                    ? elemento[1].GetInt32()
                    : int.TryParse(elemento[1].GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;

                puntos.Add(new PuntoHistoricoSfi(fecha, sfi));
            }
            else if (elemento.ValueKind == JsonValueKind.Object)
            {
                string? fechaTexto = ObtenerValorTexto(elemento, "time_tag");
                DateTime fecha = DateTime.TryParse(fechaTexto, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime f) ? f : DateTime.UtcNow;

                int sfi = 0;
                if (elemento.TryGetProperty("flux", out JsonElement flujo))
                {
                    sfi = flujo.ValueKind == JsonValueKind.Number
                        ? flujo.GetInt32()
                        : int.TryParse(flujo.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
                }

                puntos.Add(new PuntoHistoricoSfi(fecha, sfi));
            }
        }

        return puntos.AsReadOnly();
    }

    /// <summary>
    /// Parsea el histórico del índice planetario Kp.
    /// Formato esperado: array de arrays donde el primer elemento es el header.
    /// </summary>
    internal static IReadOnlyList<PuntoHistoricoKp> ParsearHistoricoKp(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;
        List<PuntoHistoricoKp> puntos = new();

        if (raiz.ValueKind != JsonValueKind.Array)
        {
            return puntos.AsReadOnly();
        }

        bool esHeader = true;
        foreach (JsonElement elemento in raiz.EnumerateArray())
        {
            if (esHeader)
            {
                esHeader = false;
                continue;
            }

            if (elemento.ValueKind == JsonValueKind.Array && elemento.GetArrayLength() >= 2)
            {
                string? fechaTexto = elemento[0].GetString();
                DateTime fecha = DateTime.TryParse(fechaTexto, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime f) ? f : DateTime.UtcNow;

                double kp = elemento[1].ValueKind == JsonValueKind.Number
                    ? elemento[1].GetDouble()
                    : double.TryParse(elemento[1].GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : 0;

                puntos.Add(new PuntoHistoricoKp(fecha, kp));
            }
            else if (elemento.ValueKind == JsonValueKind.Object)
            {
                string? fechaTexto = ObtenerValorTexto(elemento, "time_tag");
                DateTime fecha = DateTime.TryParse(fechaTexto, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime f) ? f : DateTime.UtcNow;

                double kp = 0;
                if (elemento.TryGetProperty("Kp", out JsonElement kpElement))
                {
                    kp = kpElement.ValueKind == JsonValueKind.Number
                        ? kpElement.GetDouble()
                        : double.TryParse(kpElement.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : 0;
                }

                puntos.Add(new PuntoHistoricoKp(fecha, kp));
            }
        }

        return puntos.AsReadOnly();
    }

    // ─── Parseo de XML de N0NBH ─────────────────────────────────────────

    /// <summary>
    /// Parsea el XML de N0NBH HAMQSL para extraer datos solares y condiciones de banda.
    /// </summary>
    internal static DatosN0nbh ParsearXmlN0nbh(string xml)
    {
        XDocument doc = XDocument.Parse(xml);
        XElement? raiz = doc.Root?.Element("solardata");

        if (raiz is null)
        {
            return new DatosN0nbh();
        }

        DatosN0nbh datos = new()
        {
            Sfi = ObtenerEnteroXml(raiz, "solarflux"),
            Ap = ObtenerEnteroXml(raiz, "aindex"),
            Kp = ObtenerEnteroXml(raiz, "kindex"),
            RayosX = raiz.Element("xray")?.Value ?? "N/A",
            NumeroManchasSolares = ObtenerDoubleXml(raiz, "sunspots"),
            VelocidadVientoSolar = ObtenerDoubleXml(raiz, "solarwind"),
            FlujoProtones = ObtenerDoubleXml(raiz, "protonflux"),
            FlujoElectrones = ObtenerDoubleXml(raiz, "electflux"),
            CampoGeomagnetico = raiz.Element("geomagfield")?.Value ?? "N/A",
            RuidoSenal = raiz.Element("signalnoise")?.Value ?? "N/A",
            CampoMagnetico = ObtenerDoubleXml(raiz, "magneticfield")
        };

        // Parsear condiciones HF por banda
        List<CondicionBandaHf> condicionesHf = new();
        IEnumerable<XElement> calculatedConditions = raiz.Elements("calculatedconditions");
        foreach (XElement cc in calculatedConditions)
        {
            foreach (XElement banda in cc.Elements("band"))
            {
                string nombreBanda = banda.Attribute("name")?.Value ?? "unknown";
                string tiempo = banda.Attribute("time")?.Value ?? "";

                // N0NBH da condiciones separadas para día y noche
                CondicionBandaHf? existente = condicionesHf.Find(c => c.Banda == nombreBanda);
                if (existente is not null)
                {
                    // Ya existe, actualizar el campo que falta
                    int indice = condicionesHf.IndexOf(existente);
                    if (string.Equals(tiempo, "day", StringComparison.OrdinalIgnoreCase))
                    {
                        condicionesHf[indice] = existente with { Dia = banda.Value };
                    }
                    else
                    {
                        condicionesHf[indice] = existente with { Noche = banda.Value };
                    }
                }
                else
                {
                    if (string.Equals(tiempo, "day", StringComparison.OrdinalIgnoreCase))
                    {
                        condicionesHf.Add(new CondicionBandaHf(nombreBanda, banda.Value, "N/A"));
                    }
                    else
                    {
                        condicionesHf.Add(new CondicionBandaHf(nombreBanda, "N/A", banda.Value));
                    }
                }
            }
        }

        datos.CondicionesHf = condicionesHf;

        // Parsear condiciones VHF
        IEnumerable<XElement> calculatedVhfConditions = raiz.Elements("calculatedvhfconditions");
        string auroraVhf = "N/A";
        string eSkipEuropa = "N/A";
        string eSkipNa = "N/A";

        foreach (XElement vhf in calculatedVhfConditions)
        {
            foreach (XElement fenomeno in vhf.Elements("phenomenon"))
            {
                string nombre = fenomeno.Attribute("name")?.Value ?? "";
                string ubicacion = fenomeno.Attribute("location")?.Value ?? "";

                if (string.Equals(nombre, "vhf_aurora", StringComparison.OrdinalIgnoreCase))
                {
                    auroraVhf = fenomeno.Value;
                }
                else if (string.Equals(nombre, "E-Skip", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(ubicacion, "europe", StringComparison.OrdinalIgnoreCase))
                    {
                        eSkipEuropa = fenomeno.Value;
                    }
                    else if (string.Equals(ubicacion, "north_america", StringComparison.OrdinalIgnoreCase))
                    {
                        eSkipNa = fenomeno.Value;
                    }
                }
            }
        }

        datos.CondicionesVhf = new CondicionesVhf(auroraVhf, eSkipEuropa, eSkipNa);

        return datos;
    }

    // ─── Combinación de fuentes ─────────────────────────────────────────

    private DatosSolaresCompletos CombinarFuentes(DatosNoaa? noaa, DatosN0nbh? n0nbh)
    {
        // NOAA tiene prioridad para datos numéricos precisos; N0NBH complementa con condiciones de banda
        int sfi = noaa?.Sfi ?? n0nbh?.Sfi ?? 0;
        double kp = noaa?.Kp ?? n0nbh?.Kp ?? 0;
        int ap = n0nbh?.Ap ?? 0;
        double manchas = n0nbh?.NumeroManchasSolares ?? 0;
        string rayosX = n0nbh?.RayosX ?? "N/A";
        double velocidadViento = noaa?.VelocidadVientoSolar ?? n0nbh?.VelocidadVientoSolar ?? 0;
        double bt = noaa?.Bt ?? n0nbh?.CampoMagnetico ?? 0;
        double bzGsm = noaa?.BzGsm ?? 0;
        double flujoProtones = n0nbh?.FlujoProtones ?? 0;
        double flujoElectrones = n0nbh?.FlujoElectrones ?? 0;
        string campoGeomagnetico = n0nbh?.CampoGeomagnetico ?? "N/A";
        string ruidoSenal = n0nbh?.RuidoSenal ?? "N/A";

        IReadOnlyList<CondicionBandaHf> condicionesHf = n0nbh?.CondicionesHf?.AsReadOnly()
            ?? (IReadOnlyList<CondicionBandaHf>)Array.Empty<CondicionBandaHf>();

        CondicionesVhf condicionesVhf = n0nbh?.CondicionesVhf ?? new CondicionesVhf("N/A", "N/A", "N/A");

        EscalasEspaciales escalas = noaa?.Escalas ?? new EscalasEspaciales("0", "0", "0", 0, 0, 0);

        IReadOnlyList<AlertaSolar> alertas = noaa?.Alertas
            ?? (IReadOnlyList<AlertaSolar>)Array.Empty<AlertaSolar>();

        return new DatosSolaresCompletos(
            sfi,
            (int)kp,
            ap,
            manchas,
            rayosX,
            velocidadViento,
            bt,
            bzGsm,
            flujoProtones,
            flujoElectrones,
            campoGeomagnetico,
            ruidoSenal,
            condicionesHf,
            condicionesVhf,
            escalas,
            alertas,
            DateTime.UtcNow);
    }

    private static DatosSolaresCompletos CrearDatosDeRespaldo()
    {
        return new DatosSolaresCompletos(
            Sfi: 0,
            Kp: 0,
            Ap: 0,
            NumeroManchasSolares: 0,
            RayosX: "N/A",
            VelocidadVientoSolar: 0,
            Bt: 0,
            BzGsm: 0,
            FlujoProtones: 0,
            FlujoElectrones: 0,
            CampoGeomagnetico: "N/A",
            RuidoSenal: "N/A",
            CondicionesHf: Array.Empty<CondicionBandaHf>(),
            CondicionesVhf: new CondicionesVhf("N/A", "N/A", "N/A"),
            Escalas: new EscalasEspaciales("0", "0", "0", 0, 0, 0),
            AlertasActivas: Array.Empty<AlertaSolar>(),
            FechaActualizacion: DateTime.UtcNow);
    }

    // ─── Utilidades ─────────────────────────────────────────────────────

    private static string? ObtenerValorTexto(JsonElement elemento, string propiedad)
    {
        if (elemento.TryGetProperty(propiedad, out JsonElement valor))
        {
            return valor.ValueKind == JsonValueKind.Null ? null : valor.ToString();
        }

        return null;
    }

    private static int ObtenerEnteroXml(XElement padre, string nombre)
    {
        string? texto = padre.Element(nombre)?.Value;
        return int.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out int valor) ? valor : 0;
    }

    private static double ObtenerDoubleXml(XElement padre, string nombre)
    {
        string? texto = padre.Element(nombre)?.Value;
        return double.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) ? valor : 0;
    }

    // ─── DTOs internos para parseo ──────────────────────────────────────

    internal sealed class DatosNoaa
    {
        public int Sfi { get; set; }
        public double VelocidadVientoSolar { get; set; }
        public double Bt { get; set; }
        public double BzGsm { get; set; }
        public double Kp { get; set; }
        public EscalasEspaciales? Escalas { get; set; }
        public IReadOnlyList<AlertaSolar>? Alertas { get; set; }
    }

    internal sealed class DatosN0nbh
    {
        public int Sfi { get; set; }
        public int Ap { get; set; }
        public int Kp { get; set; }
        public string RayosX { get; set; } = "N/A";
        public double NumeroManchasSolares { get; set; }
        public double VelocidadVientoSolar { get; set; }
        public double FlujoProtones { get; set; }
        public double FlujoElectrones { get; set; }
        public double CampoMagnetico { get; set; }
        public string CampoGeomagnetico { get; set; } = "N/A";
        public string RuidoSenal { get; set; } = "N/A";
        public List<CondicionBandaHf>? CondicionesHf { get; set; }
        public CondicionesVhf? CondicionesVhf { get; set; }
    }
}
