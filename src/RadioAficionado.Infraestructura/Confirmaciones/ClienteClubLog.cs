using System.Text;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Infraestructura.Confirmaciones;

/// <summary>
/// Cliente HTTP para interactuar con el servicio Club Log.
/// Permite subir QSOs en formato ADIF para análisis DXCC y estadísticas.
/// </summary>
public sealed class ClienteClubLog : IClienteClubLog
{
    private readonly HttpClient _httpClient;
    private readonly ConfiguracionClubLog _configuracion;
    private readonly ILogger<ClienteClubLog> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ClienteClubLog"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP inyectado por la fábrica.</param>
    /// <param name="configuracion">Configuración de conexión a Club Log.</param>
    /// <param name="logger">Logger para registrar operaciones.</param>
    public ClienteClubLog(HttpClient httpClient, ConfiguracionClubLog configuracion, ILogger<ClienteClubLog> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _configuracion = configuracion;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResultadoSubida> SubirAdifAsync(string contenidoAdif, string email, string password, string indicativo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contenidoAdif))
        {
            return new ResultadoSubida(false, 0, 0, "El contenido ADIF no puede estar vacío.", ServicioExterno.ClubLog);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return new ResultadoSubida(false, 0, 0, "El email de Club Log es obligatorio.", ServicioExterno.ClubLog);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new ResultadoSubida(false, 0, 0, "La contraseña de Club Log es obligatoria.", ServicioExterno.ClubLog);
        }

        if (string.IsNullOrWhiteSpace(indicativo))
        {
            return new ResultadoSubida(false, 0, 0, "El indicativo es obligatorio.", ServicioExterno.ClubLog);
        }

        try
        {
            _logger.LogInformation("Iniciando subida de ADIF a Club Log para el indicativo {Indicativo}.", indicativo);

            string urlSubida = $"{_configuracion.UrlBase.TrimEnd('/')}/putlogs.php";

            // Club Log acepta ADIF via POST multipart con parámetros de autenticación
            using MultipartFormDataContent contenidoMultiparte = new();
            contenidoMultiparte.Add(new StringContent(email), "email");
            contenidoMultiparte.Add(new StringContent(password), "password");
            contenidoMultiparte.Add(new StringContent(indicativo), "callsign");
            contenidoMultiparte.Add(new StringContent(_configuracion.ApiKey), "api");

            byte[] bytesAdif = Encoding.UTF8.GetBytes(contenidoAdif);
            ByteArrayContent contenidoArchivo = new(bytesAdif);
            contenidoArchivo.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            contenidoMultiparte.Add(contenidoArchivo, "file", "upload.adi");

            HttpResponseMessage respuesta = await _httpClient.PostAsync(urlSubida, contenidoMultiparte, ct).ConfigureAwait(false);
            string cuerpoRespuesta = await respuesta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("Club Log respondió con código {Codigo}: {Cuerpo}", (int)respuesta.StatusCode, cuerpoRespuesta);
                return new ResultadoSubida(false, 0, 0, $"Error HTTP {(int)respuesta.StatusCode}: {cuerpoRespuesta}", ServicioExterno.ClubLog);
            }

            ResultadoSubida resultado = ParsearRespuestaSubida(cuerpoRespuesta, (int)respuesta.StatusCode);

            _logger.LogInformation(
                "Subida a Club Log completada: {QsosSubidos} subidos, {QsosRechazados} rechazados.",
                resultado.QsosSubidos,
                resultado.QsosRechazados);

            return resultado;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de conexión al subir ADIF a Club Log.");
            return new ResultadoSubida(false, 0, 0, $"Error de conexión: {ex.Message}", ServicioExterno.ClubLog);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout al subir ADIF a Club Log.");
            return new ResultadoSubida(false, 0, 0, "La conexión con Club Log ha expirado.", ServicioExterno.ClubLog);
        }
    }

    /// <summary>
    /// Parsea la respuesta de Club Log tras una subida.
    /// Club Log devuelve códigos HTTP específicos:
    /// 200 = éxito, 401 = credenciales inválidas, 403 = API key inválida, 449 = indicativo no encontrado.
    /// </summary>
    /// <param name="cuerpoRespuesta">Texto de la respuesta de Club Log.</param>
    /// <param name="codigoHttp">Código HTTP de la respuesta.</param>
    /// <returns>Resultado de la subida con los conteos extraídos.</returns>
    public static ResultadoSubida ParsearRespuestaSubida(string cuerpoRespuesta, int codigoHttp)
    {
        if (string.IsNullOrWhiteSpace(cuerpoRespuesta))
        {
            // Club Log a veces responde solo con el código HTTP sin cuerpo
            bool exitosoSinCuerpo = codigoHttp >= 200 && codigoHttp < 300;
            return new ResultadoSubida(exitosoSinCuerpo, 0, 0,
                exitosoSinCuerpo ? "Archivo procesado por Club Log." : $"Error HTTP {codigoHttp}.",
                ServicioExterno.ClubLog);
        }

        string respuestaLower = cuerpoRespuesta.ToLowerInvariant();

        // Verificar errores conocidos
        if (respuestaLower.Contains("unauthorized") || respuestaLower.Contains("invalid password"))
        {
            return new ResultadoSubida(false, 0, 0, "Credenciales inválidas para Club Log.", ServicioExterno.ClubLog);
        }

        if (respuestaLower.Contains("invalid api key") || respuestaLower.Contains("api key"))
        {
            return new ResultadoSubida(false, 0, 0, "Clave de API inválida para Club Log.", ServicioExterno.ClubLog);
        }

        if (respuestaLower.Contains("callsign not found") || respuestaLower.Contains("no callsign"))
        {
            return new ResultadoSubida(false, 0, 0, "Indicativo no encontrado en Club Log.", ServicioExterno.ClubLog);
        }

        // Intentar extraer conteos
        int qsosSubidos = ExtraerNumero(cuerpoRespuesta, "inserted");
        if (qsosSubidos == 0)
        {
            qsosSubidos = ExtraerNumero(cuerpoRespuesta, "added");
        }

        int qsosRechazados = ExtraerNumero(cuerpoRespuesta, "rejected");
        if (qsosRechazados == 0)
        {
            qsosRechazados = ExtraerNumero(cuerpoRespuesta, "errors");
        }

        bool exitoso = codigoHttp >= 200 && codigoHttp < 300 && !respuestaLower.Contains("error");
        return new ResultadoSubida(exitoso, qsosSubidos, qsosRechazados, cuerpoRespuesta.Trim(), ServicioExterno.ClubLog);
    }

    /// <summary>
    /// Extrae un número que precede a una palabra clave en el texto.
    /// </summary>
    private static int ExtraerNumero(string texto, string palabraClave)
    {
        int indice = texto.IndexOf(palabraClave, StringComparison.OrdinalIgnoreCase);
        if (indice <= 0)
        {
            return 0;
        }

        int finNumero = indice - 1;
        while (finNumero >= 0 && char.IsWhiteSpace(texto[finNumero]))
        {
            finNumero--;
        }

        if (finNumero < 0 || !char.IsDigit(texto[finNumero]))
        {
            return 0;
        }

        int inicioNumero = finNumero;
        while (inicioNumero > 0 && char.IsDigit(texto[inicioNumero - 1]))
        {
            inicioNumero--;
        }

        string textoNumero = texto[inicioNumero..(finNumero + 1)];
        return int.TryParse(textoNumero, out int numero) ? numero : 0;
    }
}
