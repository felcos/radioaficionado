using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Infraestructura.Confirmaciones;

/// <summary>
/// Cliente HTTP para interactuar con el servicio Logbook of the World (LoTW) de la ARRL.
/// Sube archivos ADIF y descarga confirmaciones mediante la API web de LoTW.
/// </summary>
public sealed class ClienteLoTW : IClienteLoTW
{
    private readonly HttpClient _httpClient;
    private readonly ConfiguracionLoTW _configuracion;
    private readonly ILogger<ClienteLoTW> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ClienteLoTW"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP inyectado por la fábrica.</param>
    /// <param name="configuracion">Configuración de conexión a LoTW.</param>
    /// <param name="logger">Logger para registrar operaciones.</param>
    public ClienteLoTW(HttpClient httpClient, ConfiguracionLoTW configuracion, ILogger<ClienteLoTW> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _configuracion = configuracion;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResultadoSubida> SubirAdifAsync(string contenidoAdif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contenidoAdif))
        {
            return new ResultadoSubida(false, 0, 0, "El contenido ADIF no puede estar vacío.", ServicioExterno.LoTW);
        }

        try
        {
            _logger.LogInformation("Iniciando subida de ADIF a LoTW para el indicativo {Indicativo}.", _configuracion.IndicativoPropio);

            string urlSubida = $"{_configuracion.UrlBase.TrimEnd('/')}/lotwuser/upload";

            using MultipartFormDataContent contenidoMultiparte = new();
            byte[] bytesAdif = Encoding.UTF8.GetBytes(contenidoAdif);
            ByteArrayContent contenidoArchivo = new(bytesAdif);
            contenidoArchivo.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            contenidoMultiparte.Add(contenidoArchivo, "upfile", "upload.adi");

            HttpResponseMessage respuesta = await _httpClient.PostAsync(urlSubida, contenidoMultiparte, ct).ConfigureAwait(false);
            string cuerpoRespuesta = await respuesta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("LoTW respondió con código {Codigo}: {Cuerpo}", (int)respuesta.StatusCode, cuerpoRespuesta);
                return new ResultadoSubida(false, 0, 0, $"Error HTTP {(int)respuesta.StatusCode}: {cuerpoRespuesta}", ServicioExterno.LoTW);
            }

            ResultadoSubida resultado = ParsearRespuestaSubida(cuerpoRespuesta);

            _logger.LogInformation(
                "Subida a LoTW completada: {QsosSubidos} subidos, {QsosRechazados} rechazados.",
                resultado.QsosSubidos,
                resultado.QsosRechazados);

            return resultado;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de conexión al subir ADIF a LoTW.");
            return new ResultadoSubida(false, 0, 0, $"Error de conexión: {ex.Message}", ServicioExterno.LoTW);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout al subir ADIF a LoTW.");
            return new ResultadoSubida(false, 0, 0, "La conexión con LoTW ha expirado.", ServicioExterno.LoTW);
        }
    }

    /// <inheritdoc />
    public async Task<string> DescargarConfirmacionesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Descargando confirmaciones de LoTW para {Indicativo}.", _configuracion.IndicativoPropio);

            string urlDescarga = $"{_configuracion.UrlBase.TrimEnd('/')}/lotwuser/lotwreport.adi" +
                                 $"?login={Uri.EscapeDataString(_configuracion.Usuario)}" +
                                 $"&password={Uri.EscapeDataString(_configuracion.Password)}" +
                                 "&qso_query=1&qso_qsl=yes&qso_qsldetail=yes&qso_withown=yes";

            HttpResponseMessage respuesta = await _httpClient.GetAsync(urlDescarga, ct).ConfigureAwait(false);
            respuesta.EnsureSuccessStatusCode();

            string contenido = await respuesta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Confirmaciones de LoTW descargadas exitosamente ({Longitud} caracteres).", contenido.Length);

            return contenido;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de conexión al descargar confirmaciones de LoTW.");
            throw;
        }
    }

    /// <summary>
    /// Parsea la respuesta HTML/texto de LoTW tras una subida para extraer los conteos de QSOs.
    /// </summary>
    /// <param name="cuerpoRespuesta">Texto de la respuesta de LoTW.</param>
    /// <returns>Resultado de la subida con los conteos extraídos.</returns>
    public static ResultadoSubida ParsearRespuestaSubida(string cuerpoRespuesta)
    {
        if (string.IsNullOrWhiteSpace(cuerpoRespuesta))
        {
            return new ResultadoSubida(false, 0, 0, "Respuesta vacía de LoTW.", ServicioExterno.LoTW);
        }

        string respuestaLower = cuerpoRespuesta.ToLowerInvariant();

        // LoTW responde con texto que incluye conteos como:
        // "Your file has been queued for processing."
        // o mensajes de error como "incorrect password"
        if (respuestaLower.Contains("error") || respuestaLower.Contains("incorrect") || respuestaLower.Contains("failed"))
        {
            return new ResultadoSubida(false, 0, 0, cuerpoRespuesta.Trim(), ServicioExterno.LoTW);
        }

        // Intentar extraer cantidad de QSOs del mensaje
        int qsosSubidos = ExtraerNumeroDeRespuesta(cuerpoRespuesta, "accepted");
        int qsosRechazados = ExtraerNumeroDeRespuesta(cuerpoRespuesta, "rejected");

        // Si no se pudieron extraer números pero no hay error, asumir éxito
        if (qsosSubidos == 0 && qsosRechazados == 0 &&
            (respuestaLower.Contains("queued") || respuestaLower.Contains("processed") || respuestaLower.Contains("success")))
        {
            return new ResultadoSubida(true, 0, 0, "Archivo encolado para procesamiento en LoTW.", ServicioExterno.LoTW);
        }

        bool exitoso = qsosRechazados == 0 || qsosSubidos > 0;
        return new ResultadoSubida(exitoso, qsosSubidos, qsosRechazados, cuerpoRespuesta.Trim(), ServicioExterno.LoTW);
    }

    /// <summary>
    /// Extrae un número que precede a una palabra clave en la respuesta.
    /// Por ejemplo, de "5 accepted" extrae 5.
    /// </summary>
    private static int ExtraerNumeroDeRespuesta(string respuesta, string palabraClave)
    {
        int indice = respuesta.IndexOf(palabraClave, StringComparison.OrdinalIgnoreCase);
        if (indice <= 0)
        {
            return 0;
        }

        // Retroceder desde la palabra clave para encontrar el número
        int finNumero = indice - 1;
        while (finNumero >= 0 && char.IsWhiteSpace(respuesta[finNumero]))
        {
            finNumero--;
        }

        if (finNumero < 0 || !char.IsDigit(respuesta[finNumero]))
        {
            return 0;
        }

        int inicioNumero = finNumero;
        while (inicioNumero > 0 && char.IsDigit(respuesta[inicioNumero - 1]))
        {
            inicioNumero--;
        }

        string textoNumero = respuesta[inicioNumero..(finNumero + 1)];
        return int.TryParse(textoNumero, out int numero) ? numero : 0;
    }
}
