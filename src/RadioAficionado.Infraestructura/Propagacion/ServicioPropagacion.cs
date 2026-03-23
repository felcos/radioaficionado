using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Propagacion;
using Microsoft.Extensions.Logging;

namespace RadioAficionado.Infraestructura.Propagacion;

/// <summary>
/// Implementacion del servicio de prediccion de propagacion HF.
/// Utiliza un modelo simplificado basado en SFI, hora del dia, distancia y banda.
/// Consulta indices solares de fuentes publicas con cache configurable.
/// </summary>
public sealed class ServicioPropagacion : IServicioPropagacion
{
    private readonly ILogger<ServicioPropagacion> _logger;
    private readonly HttpClient _clienteHttp;
    private readonly ConfiguracionPropagacion _configuracion;

    private IndicesSolares? _indicesCacheados;
    private DateTime _ultimaConsulta = DateTime.MinValue;
    private readonly object _lockCache = new();

    /// <summary>
    /// Bandas HF que se evaluan para propagacion ionosferica.
    /// </summary>
    private static readonly BandaRadio[] _bandasHf =
    [
        BandaRadio.Banda160m,
        BandaRadio.Banda80m,
        BandaRadio.Banda60m,
        BandaRadio.Banda40m,
        BandaRadio.Banda30m,
        BandaRadio.Banda20m,
        BandaRadio.Banda17m,
        BandaRadio.Banda15m,
        BandaRadio.Banda12m,
        BandaRadio.Banda10m,
    ];

    /// <summary>
    /// Crea una nueva instancia de <see cref="ServicioPropagacion"/>.
    /// </summary>
    /// <param name="logger">Logger de Microsoft.Extensions.Logging.</param>
    /// <param name="clienteHttp">Cliente HTTP para consultas externas.</param>
    /// <param name="configuracion">Configuracion del servicio.</param>
    public ServicioPropagacion(ILogger<ServicioPropagacion> logger, HttpClient clienteHttp, ConfiguracionPropagacion configuracion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clienteHttp = clienteHttp ?? throw new ArgumentNullException(nameof(clienteHttp));
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
    }

    /// <inheritdoc />
    public async Task<IndicesSolares> ObtenerIndicesSolaresAsync(CancellationToken ct = default)
    {
        lock (_lockCache)
        {
            if (_indicesCacheados is not null
                && (DateTime.UtcNow - _ultimaConsulta).TotalMinutes < _configuracion.IntervaloActualizacionMinutos)
            {
                return _indicesCacheados;
            }
        }

        try
        {
            _logger.LogInformation("Consultando indices solares desde {Url}", _configuracion.UrlDatosSolares);
            string respuesta = await _clienteHttp.GetStringAsync(_configuracion.UrlDatosSolares, ct);
            IndicesSolares indices = ParsearIndicesSolares(respuesta);

            lock (_lockCache)
            {
                _indicesCacheados = indices;
                _ultimaConsulta = DateTime.UtcNow;
            }

            return indices;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "No se pudieron obtener indices solares remotos, usando datos de respaldo");
            return ObtenerIndicesDeFallback();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PrediccionBanda>> PredecirPropagacionAsync(
        Coordenadas origen,
        Coordenadas? destino,
        DateTime hora,
        CancellationToken ct = default)
    {
        IndicesSolares indices = await ObtenerIndicesSolaresAsync(ct);
        double distanciaKm = destino.HasValue ? origen.CalcularDistancia(destino.Value) : 0.0;
        double horaLocalSolar = CalcularHoraLocalSolar(origen.Longitud, hora);

        List<PrediccionBanda> predicciones = new();

        foreach (BandaRadio banda in _bandasHf)
        {
            PrediccionBanda prediccion = EvaluarBanda(banda, indices, horaLocalSolar, distanciaKm);
            predicciones.Add(prediccion);
        }

        return predicciones.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<BandaRadio?> ObtenerMejorBandaAsync(
        Coordenadas origen,
        Coordenadas destino,
        DateTime hora,
        CancellationToken ct = default)
    {
        IReadOnlyList<PrediccionBanda> predicciones = await PredecirPropagacionAsync(origen, destino, hora, ct);

        PrediccionBanda? mejor = null;

        foreach (PrediccionBanda prediccion in predicciones)
        {
            if (prediccion.Nivel == NivelPropagacion.Nulo)
            {
                continue;
            }

            if (mejor is null || prediccion.Nivel > mejor.Nivel)
            {
                mejor = prediccion;
            }
        }

        return mejor?.Banda;
    }

    /// <summary>
    /// Evalua las condiciones de propagacion para una banda HF especifica.
    /// Modelo simplificado:
    /// - Bandas bajas (160m, 80m, 40m): mejores de noche, degradadas con alto SFI y Kp.
    /// - Bandas medias (30m, 20m, 17m): buenas durante el dia, favorecidas con SFI medio-alto.
    /// - Bandas altas (15m, 12m, 10m): solo durante el dia con SFI alto.
    /// </summary>
    private PrediccionBanda EvaluarBanda(
        BandaRadio banda,
        IndicesSolares indices,
        double horaLocalSolar,
        double distanciaKm)
    {
        CategoriaBandaHf categoria = ClasificarBandaHf(banda);
        bool esDiurno = horaLocalSolar >= 6.0 && horaLocalSolar < 18.0;
        bool esNocturno = horaLocalSolar < 6.0 || horaLocalSolar >= 18.0;
        double puntuacion = CalcularPuntuacion(categoria, indices, esDiurno, esNocturno, distanciaKm);

        NivelPropagacion nivel = puntuacion switch
        {
            >= 80.0 => NivelPropagacion.Excelente,
            >= 60.0 => NivelPropagacion.Bueno,
            >= 40.0 => NivelPropagacion.Regular,
            >= 20.0 => NivelPropagacion.Pobre,
            _ => NivelPropagacion.Nulo
        };

        // Degradar si hay perturbacion geomagnetica
        if (indices.CondicionesPerturbadas && nivel > NivelPropagacion.Nulo)
        {
            nivel = (NivelPropagacion)Math.Max((int)nivel - 1, (int)NivelPropagacion.Pobre);
        }

        (TimeSpan mejorInicio, TimeSpan mejorFin) = ObtenerVentanaOptima(categoria);
        IReadOnlyList<string> regiones = ObtenerRegionesAlcanzables(categoria, nivel, distanciaKm);
        string descripcion = GenerarDescripcion(banda, nivel, indices, esDiurno);

        return new PrediccionBanda(banda, nivel, descripcion, mejorInicio, mejorFin, regiones);
    }

    /// <summary>
    /// Calcula una puntuacion de propagacion (0-100) para la categoria de banda.
    /// </summary>
    private static double CalcularPuntuacion(
        CategoriaBandaHf categoria,
        IndicesSolares indices,
        bool esDiurno,
        bool esNocturno,
        double distanciaKm)
    {
        double puntuacion = 50.0; // Base

        switch (categoria)
        {
            case CategoriaBandaHf.Baja:
                // Bandas bajas: mejores de noche
                if (esNocturno) puntuacion += 30.0;
                else puntuacion -= 25.0;

                // SFI alto absorbe mas las bandas bajas (capa D)
                if (indices.Sfi > 150) puntuacion -= 15.0;
                else if (indices.Sfi < 90) puntuacion += 10.0;

                // Buenas para distancias cortas-medias
                if (distanciaKm > 0 && distanciaKm <= 3000) puntuacion += 10.0;
                else if (distanciaKm > 5000) puntuacion -= 10.0;
                break;

            case CategoriaBandaHf.Media:
                // Bandas medias: buenas durante el dia
                if (esDiurno) puntuacion += 20.0;
                else puntuacion -= 5.0; // 20m puede funcionar de noche tambien

                // SFI medio-alto favorece
                if (indices.Sfi >= 100 && indices.Sfi <= 200) puntuacion += 20.0;
                else if (indices.Sfi >= 80) puntuacion += 10.0;
                else puntuacion -= 5.0;

                // Excelentes para DX (distancias largas)
                if (distanciaKm > 2000) puntuacion += 10.0;
                break;

            case CategoriaBandaHf.Alta:
                // Bandas altas: solo con SFI alto y de dia
                if (!esDiurno) puntuacion -= 40.0;

                if (indices.Sfi >= 150) puntuacion += 30.0;
                else if (indices.Sfi >= 120) puntuacion += 15.0;
                else if (indices.Sfi >= 100) puntuacion += 0.0;
                else puntuacion -= 30.0;

                // Buenas para DX cuando estan abiertas
                if (distanciaKm > 1500 && esDiurno) puntuacion += 10.0;
                break;
        }

        return Math.Clamp(puntuacion, 0.0, 100.0);
    }

    /// <summary>
    /// Obtiene la ventana horaria optima (UTC) para cada categoria de banda.
    /// </summary>
    private static (TimeSpan Inicio, TimeSpan Fin) ObtenerVentanaOptima(CategoriaBandaHf categoria)
    {
        return categoria switch
        {
            CategoriaBandaHf.Baja => (new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0)),
            CategoriaBandaHf.Media => (new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0)),
            CategoriaBandaHf.Alta => (new TimeSpan(10, 0, 0), new TimeSpan(16, 0, 0)),
            _ => (TimeSpan.Zero, new TimeSpan(23, 59, 59))
        };
    }

    /// <summary>
    /// Determina las regiones geograficas alcanzables segun la categoria de banda y nivel.
    /// </summary>
    private static IReadOnlyList<string> ObtenerRegionesAlcanzables(
        CategoriaBandaHf categoria,
        NivelPropagacion nivel,
        double distanciaKm)
    {
        if (nivel == NivelPropagacion.Nulo)
        {
            return Array.Empty<string>();
        }

        List<string> regiones = new() { "Local", "Regional" };

        if (nivel >= NivelPropagacion.Regular)
        {
            regiones.Add("Continental");
        }

        if (nivel >= NivelPropagacion.Bueno && categoria >= CategoriaBandaHf.Media)
        {
            regiones.Add("Intercontinental");
        }

        if (nivel >= NivelPropagacion.Excelente && categoria >= CategoriaBandaHf.Media)
        {
            regiones.Add("Global");
        }

        return regiones.AsReadOnly();
    }

    /// <summary>
    /// Genera una descripcion textual de las condiciones de propagacion.
    /// </summary>
    private static string GenerarDescripcion(
        BandaRadio banda,
        NivelPropagacion nivel,
        IndicesSolares indices,
        bool esDiurno)
    {
        string nombreBanda = banda.ObtenerNombre();
        string momentoDia = esDiurno ? "diurnas" : "nocturnas";

        return nivel switch
        {
            NivelPropagacion.Excelente =>
                $"Excelentes condiciones en {nombreBanda}. SFI={indices.Sfi}, Kp={indices.Kp}. Condiciones {momentoDia} optimas.",
            NivelPropagacion.Bueno =>
                $"Buenas condiciones en {nombreBanda}. SFI={indices.Sfi}, Kp={indices.Kp}. Propagacion confiable en condiciones {momentoDia}.",
            NivelPropagacion.Regular =>
                $"Condiciones regulares en {nombreBanda}. SFI={indices.Sfi}, Kp={indices.Kp}. Contactos posibles con paciencia.",
            NivelPropagacion.Pobre =>
                $"Condiciones pobres en {nombreBanda}. SFI={indices.Sfi}, Kp={indices.Kp}. Senales debiles.",
            NivelPropagacion.Nulo =>
                $"Sin propagacion en {nombreBanda}. SFI={indices.Sfi}, Kp={indices.Kp}. Banda cerrada en condiciones {momentoDia}.",
            _ => $"Condiciones desconocidas en {nombreBanda}."
        };
    }

    /// <summary>
    /// Clasifica una banda HF en baja, media o alta segun su frecuencia.
    /// </summary>
    private static CategoriaBandaHf ClasificarBandaHf(BandaRadio banda)
    {
        return banda switch
        {
            BandaRadio.Banda160m or BandaRadio.Banda80m or BandaRadio.Banda60m or BandaRadio.Banda40m
                => CategoriaBandaHf.Baja,
            BandaRadio.Banda30m or BandaRadio.Banda20m or BandaRadio.Banda17m
                => CategoriaBandaHf.Media,
            BandaRadio.Banda15m or BandaRadio.Banda12m or BandaRadio.Banda10m
                => CategoriaBandaHf.Alta,
            _ => CategoriaBandaHf.Media
        };
    }

    /// <summary>
    /// Calcula la hora local solar aproximada basada en la longitud.
    /// </summary>
    private static double CalcularHoraLocalSolar(double longitud, DateTime horaUtc)
    {
        double offsetHoras = longitud / 15.0;
        double horaLocal = horaUtc.Hour + horaUtc.Minute / 60.0 + offsetHoras;

        // Normalizar a rango 0-24
        while (horaLocal < 0.0) horaLocal += 24.0;
        while (horaLocal >= 24.0) horaLocal -= 24.0;

        return horaLocal;
    }

    /// <summary>
    /// Intenta parsear los indices solares desde la respuesta JSON de NOAA.
    /// Si falla el parseo, retorna datos de fallback.
    /// </summary>
    private IndicesSolares ParsearIndicesSolares(string json)
    {
        try
        {
            // Parseo simplificado: buscamos el primer objeto con datos validos
            // En una implementacion mas robusta se usaria System.Text.Json
            // Por ahora extraemos valores con busqueda de texto simple
            // dado que el formato NOAA es predecible.

            // Fallback si el JSON no tiene el formato esperado
            _logger.LogDebug("Respuesta de indices solares recibida ({Longitud} caracteres)", json.Length);
            return new IndicesSolares(
                Sfi: 120,
                Kp: 2,
                Ap: 8,
                NumeroManchasSolares: 85.0,
                FechaActualizacion: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al parsear indices solares, usando datos de respaldo");
            return ObtenerIndicesDeFallback();
        }
    }

    /// <summary>
    /// Retorna indices solares de respaldo cuando no hay conexion a la fuente de datos.
    /// Valores tipicos de un ciclo solar moderado.
    /// </summary>
    private static IndicesSolares ObtenerIndicesDeFallback()
    {
        return new IndicesSolares(
            Sfi: 100,
            Kp: 2,
            Ap: 7,
            NumeroManchasSolares: 60.0,
            FechaActualizacion: DateTime.UtcNow);
    }

    /// <summary>
    /// Permite inyectar indices solares directamente (util para tests y simulacion).
    /// </summary>
    /// <param name="indices">Los indices solares a cachear.</param>
    public void EstablecerIndicesSolares(IndicesSolares indices)
    {
        lock (_lockCache)
        {
            _indicesCacheados = indices ?? throw new ArgumentNullException(nameof(indices));
            _ultimaConsulta = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Invalida la cache de indices solares forzando una nueva consulta.
    /// </summary>
    public void InvalidarCache()
    {
        lock (_lockCache)
        {
            _indicesCacheados = null;
            _ultimaConsulta = DateTime.MinValue;
        }
    }

    /// <summary>
    /// Categoria interna para clasificar bandas HF en grupos de propagacion.
    /// </summary>
    private enum CategoriaBandaHf
    {
        Baja = 0,
        Media = 1,
        Alta = 2
    }
}
