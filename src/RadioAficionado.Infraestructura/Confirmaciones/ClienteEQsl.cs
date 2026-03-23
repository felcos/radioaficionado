using System.Text;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Infraestructura.Confirmaciones;

/// <summary>
/// Cliente HTTP para interactuar con el servicio eQSL.cc.
/// Permite subir QSOs en formato ADIF y descargar confirmaciones electrónicas.
/// </summary>
public sealed class ClienteEQsl : IClienteEQsl
{
    private readonly HttpClient _httpClient;
    private readonly ConfiguracionEQsl _configuracion;
    private readonly ILogger<ClienteEQsl> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ClienteEQsl"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP inyectado por la fábrica.</param>
    /// <param name="configuracion">Configuración de conexión a eQSL.</param>
    /// <param name="logger">Logger para registrar operaciones.</param>
    public ClienteEQsl(HttpClient httpClient, ConfiguracionEQsl configuracion, ILogger<ClienteEQsl> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _configuracion = configuracion;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResultadoSubida> SubirAdifAsync(string contenidoAdif, string usuario, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contenidoAdif))
        {
            return new ResultadoSubida(false, 0, 0, "El contenido ADIF no puede estar vacío.", ServicioExterno.EQsl);
        }

        if (string.IsNullOrWhiteSpace(usuario))
        {
            return new ResultadoSubida(false, 0, 0, "El nombre de usuario de eQSL es obligatorio.", ServicioExterno.EQsl);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new ResultadoSubida(false, 0, 0, "La contraseña de eQSL es obligatoria.", ServicioExterno.EQsl);
        }

        try
        {
            _logger.LogInformation("Iniciando subida de ADIF a eQSL para el usuario {Usuario}.", usuario);

            string urlSubida = $"{_configuracion.UrlBase.TrimEnd('/')}/qslcard/ImportADIF.cfm";

            // eQSL acepta ADIF via POST con campos de formulario
            Dictionary<string, string> campos = new()
            {
                ["eQSLUser"] = usuario,
                ["eQSLPassword"] = password,
                ["Ession"] = string.Empty,
                ["ADIFData"] = contenidoAdif
            };

            using FormUrlEncodedContent contenidoFormulario = new(campos);

            HttpResponseMessage respuesta = await _httpClient.PostAsync(urlSubida, contenidoFormulario, ct).ConfigureAwait(false);
            string cuerpoRespuesta = await respuesta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("eQSL respondió con código {Codigo}: {Cuerpo}", (int)respuesta.StatusCode, cuerpoRespuesta);
                return new ResultadoSubida(false, 0, 0, $"Error HTTP {(int)respuesta.StatusCode}: {cuerpoRespuesta}", ServicioExterno.EQsl);
            }

            ResultadoSubida resultado = ParsearRespuestaSubida(cuerpoRespuesta);

            _logger.LogInformation(
                "Subida a eQSL completada: {QsosSubidos} subidos, {QsosRechazados} rechazados.",
                resultado.QsosSubidos,
                resultado.QsosRechazados);

            return resultado;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de conexión al subir ADIF a eQSL.");
            return new ResultadoSubida(false, 0, 0, $"Error de conexión: {ex.Message}", ServicioExterno.EQsl);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout al subir ADIF a eQSL.");
            return new ResultadoSubida(false, 0, 0, "La conexión con eQSL ha expirado.", ServicioExterno.EQsl);
        }
    }

    /// <inheritdoc />
    public async Task<string> DescargarConfirmacionesAsync(string usuario, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new ArgumentException("El nombre de usuario de eQSL es obligatorio.", nameof(usuario));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("La contraseña de eQSL es obligatoria.", nameof(password));
        }

        try
        {
            _logger.LogInformation("Descargando confirmaciones de eQSL para {Usuario}.", usuario);

            string urlDescarga = $"{_configuracion.UrlBase.TrimEnd('/')}/qslcard/DownloadInBox.cfm" +
                                 $"?UserName={Uri.EscapeDataString(usuario)}" +
                                 $"&Password={Uri.EscapeDataString(password)}" +
                                 "&Archive=0";

            HttpResponseMessage respuesta = await _httpClient.GetAsync(urlDescarga, ct).ConfigureAwait(false);
            respuesta.EnsureSuccessStatusCode();

            string contenido = await respuesta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Confirmaciones de eQSL descargadas exitosamente ({Longitud} caracteres).", contenido.Length);

            return contenido;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de conexión al descargar confirmaciones de eQSL.");
            throw;
        }
    }

    /// <summary>
    /// Parsea la respuesta de eQSL tras una subida para extraer los conteos de QSOs.
    /// </summary>
    /// <param name="cuerpoRespuesta">Texto de la respuesta de eQSL.</param>
    /// <returns>Resultado de la subida con los conteos extraídos.</returns>
    public static ResultadoSubida ParsearRespuestaSubida(string cuerpoRespuesta)
    {
        if (string.IsNullOrWhiteSpace(cuerpoRespuesta))
        {
            return new ResultadoSubida(false, 0, 0, "Respuesta vacía de eQSL.", ServicioExterno.EQsl);
        }

        string respuestaLower = cuerpoRespuesta.ToLowerInvariant();

        // eQSL responde con mensajes como:
        // "Result: N records added" o "Error: ..."
        if (respuestaLower.Contains("error"))
        {
            string mensajeError = ExtraerMensajeDespuesDe(cuerpoRespuesta, "Error:");
            return new ResultadoSubida(false, 0, 0, string.IsNullOrWhiteSpace(mensajeError) ? cuerpoRespuesta.Trim() : mensajeError, ServicioExterno.EQsl);
        }

        int registrosAgregados = ExtraerNumeroAntesDe(cuerpoRespuesta, "records added");
        if (registrosAgregados == 0)
        {
            registrosAgregados = ExtraerNumeroAntesDe(cuerpoRespuesta, "record added");
        }

        int registrosDuplicados = ExtraerNumeroAntesDe(cuerpoRespuesta, "duplicates");
        if (registrosDuplicados == 0)
        {
            registrosDuplicados = ExtraerNumeroAntesDe(cuerpoRespuesta, "duplicate");
        }

        bool exitoso = registrosAgregados > 0 || (!respuestaLower.Contains("error") && !respuestaLower.Contains("fail"));
        return new ResultadoSubida(exitoso, registrosAgregados, registrosDuplicados, cuerpoRespuesta.Trim(), ServicioExterno.EQsl);
    }

    /// <summary>
    /// Extrae un número que precede a una frase en el texto.
    /// </summary>
    private static int ExtraerNumeroAntesDe(string texto, string frase)
    {
        int indice = texto.IndexOf(frase, StringComparison.OrdinalIgnoreCase);
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

    /// <summary>
    /// Extrae el texto después de un prefijo, hasta el fin de línea.
    /// </summary>
    private static string ExtraerMensajeDespuesDe(string texto, string prefijo)
    {
        int indice = texto.IndexOf(prefijo, StringComparison.OrdinalIgnoreCase);
        if (indice < 0)
        {
            return string.Empty;
        }

        int inicio = indice + prefijo.Length;
        int fin = texto.IndexOf('\n', inicio);
        if (fin < 0)
        {
            fin = texto.Length;
        }

        return texto[inicio..fin].Trim();
    }
}
