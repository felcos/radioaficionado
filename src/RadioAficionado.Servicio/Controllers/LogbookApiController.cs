using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Adif;
using RadioAficionado.Servicio.Dtos;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// API REST para acceso al logbook local desde la UI de operacion.
/// </summary>
[Route("api/logbook")]
[ApiController]
public sealed class LogbookApiController : ControllerBase
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ILogger<LogbookApiController> _logger;

    /// <summary>
    /// Tamanio maximo permitido para archivos ADIF (5 MB).
    /// </summary>
    private const long TamanoMaximoArchivo = 5 * 1024 * 1024;

    /// <summary>
    /// Crea el controlador API de logbook.
    /// </summary>
    public LogbookApiController(
        IRepositorioQso repositorioQso,
        IUnidadDeTrabajo unidadDeTrabajo,
        ILogger<LogbookApiController> logger)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene QSOs paginados con filtro de busqueda opcional.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerQsos(
        [FromQuery] int pagina = 1,
        [FromQuery] int porPagina = 50,
        [FromQuery] string? busqueda = null,
        CancellationToken ct = default)
    {
        if (pagina < 1) { pagina = 1; }
        if (porPagina < 1 || porPagina > 200) { porPagina = 50; }

        FiltroQso? filtro = null;
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            filtro = new FiltroQso(Indicativo: busqueda.Trim().ToUpperInvariant());
        }

        ResultadoPaginado<Qso> resultado = await _repositorioQso
            .ObtenerPaginadoAsync(pagina, porPagina, filtro, ct)
            .ConfigureAwait(false);

        object respuesta = new
        {
            total = resultado.TotalElementos,
            qsos = resultado.Elementos.Select(q => new
            {
                id = q.Id,
                fechaHora = q.FechaHoraInicio.ToString("yyyy-MM-dd HH:mm"),
                indicativo = q.IndicativoContacto.Valor,
                banda = q.Frecuencia.ObtenerBanda()?.ToString() ?? "",
                modo = q.Modo.ToString(),
                rstEnviado = q.SenalEnviada,
                rstRecibido = q.SenalRecibida,
                grid = q.LocalizadorContacto?.ToString() ?? "",
                dxcc = "",
                confirmado = q.Sincronizado
            })
        };

        return Ok(respuesta);
    }

    /// <summary>
    /// Registra un QSO desde el panel de operacion. Los campos minimos son
    /// indicativo, frecuencia, modo, RST enviado y RST recibido.
    /// </summary>
    /// <param name="registro">Datos del QSO a registrar.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>OK con el id del QSO creado, BadRequest si hay datos invalidos, o Conflict si es duplicado.</returns>
    [HttpPost("registrar")]
    public async Task<IActionResult> RegistrarQso([FromBody] RegistroQsoDto registro, CancellationToken ct)
    {
        if (registro is null)
        {
            return BadRequest(new { mensaje = "El cuerpo de la peticion no puede ser nulo." });
        }

        if (string.IsNullOrWhiteSpace(registro.Indicativo))
        {
            return BadRequest(new { mensaje = "El indicativo es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(registro.RstEnviado))
        {
            return BadRequest(new { mensaje = "El RST enviado es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(registro.RstRecibido))
        {
            return BadRequest(new { mensaje = "El RST recibido es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(registro.Modo))
        {
            return BadRequest(new { mensaje = "El modo de operacion es obligatorio." });
        }

        if (!Enum.TryParse<ModoOperacion>(registro.Modo, ignoreCase: true, out ModoOperacion modo))
        {
            return BadRequest(new { mensaje = $"El modo '{registro.Modo}' no es valido." });
        }

        Indicativo indicativoContacto;
        try
        {
            indicativoContacto = new Indicativo(registro.Indicativo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }

        Frecuencia frecuencia;
        try
        {
            frecuencia = Frecuencia.DesdeHz(registro.FrecuenciaHz);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }

        Localizador? localizador = null;
        if (!string.IsNullOrWhiteSpace(registro.Grid))
        {
            try
            {
                localizador = new Localizador(registro.Grid);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        Indicativo indicativoPropio = new Indicativo("MI0AAA");

        string? notas = null;
        List<string> partesNotas = new();
        if (!string.IsNullOrWhiteSpace(registro.Nombre))
        {
            partesNotas.Add($"Nombre: {registro.Nombre.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(registro.Comentario))
        {
            partesNotas.Add(registro.Comentario.Trim());
        }
        if (partesNotas.Count > 0)
        {
            notas = string.Join(" | ", partesNotas);
        }

        Qso qso = Qso.Crear(
            indicativoPropio,
            indicativoContacto,
            DateTimeOffset.UtcNow,
            frecuencia,
            modo,
            registro.RstEnviado,
            potencia: null,
            localizadorContacto: localizador,
            notas: notas);

        qso.Completar(DateTimeOffset.UtcNow, registro.RstRecibido);

        bool esDuplicado = await _repositorioQso.ExisteDuplicadoAsync(
            qso.IndicativoContacto,
            qso.FechaHoraInicio,
            qso.Frecuencia,
            qso.Modo,
            ct).ConfigureAwait(false);

        if (esDuplicado)
        {
            return Conflict(new { mensaje = $"Ya existe un QSO con {registro.Indicativo} en la misma frecuencia, modo y horario." });
        }

        await _repositorioQso.AgregarAsync(qso, ct).ConfigureAwait(false);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "QSO registrado: {Indicativo} en {Frecuencia} modo {Modo}",
            registro.Indicativo, frecuencia, modo);

        return Ok(new { id = qso.Id });
    }

    /// <summary>
    /// Importa QSOs desde un archivo ADIF. Parsea el archivo, convierte a entidades,
    /// detecta duplicados y persiste los QSOs nuevos.
    /// </summary>
    /// <param name="archivo">Archivo ADIF (.adi o .adif) enviado como form-data.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Resultado de la importacion con contadores detallados.</returns>
    [HttpPost("importar-adif")]
    [RequestSizeLimit(TamanoMaximoArchivo)]
    public async Task<IActionResult> ImportarAdif(IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { mensaje = "No se recibio ningun archivo o esta vacio." });
        }

        if (archivo.Length > TamanoMaximoArchivo)
        {
            return BadRequest(new { mensaje = "El archivo excede el tamanio maximo permitido de 5 MB." });
        }

        string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not ".adi" and not ".adif")
        {
            return BadRequest(new { mensaje = "Solo se aceptan archivos con extension .adi o .adif." });
        }

        List<string> detallesErrores = new();
        int importados = 0;
        int errores = 0;
        int duplicados = 0;

        try
        {
            using Stream flujo = archivo.OpenReadStream();
            ResultadoParserAdif resultadoParseo = await ParserAdif.ParsearDesdeStreamAsync(flujo, ct)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Archivo ADIF parseado: {TotalRegistros} registros, {Advertencias} advertencias",
                resultadoParseo.TotalRegistros, resultadoParseo.Advertencias.Count);

            // Agregar advertencias del parser como detalles informativos
            foreach (string advertencia in resultadoParseo.Advertencias)
            {
                detallesErrores.Add($"Advertencia ADIF: {advertencia}");
            }

            int indiceRegistro = 0;
            foreach (RegistroAdif registro in resultadoParseo.Registros)
            {
                indiceRegistro++;

                Qso? qso = ConvertidorAdifQso.ConvertirAQso(registro);
                if (qso is null)
                {
                    errores++;
                    string indicativo = registro.Indicativo ?? "(sin indicativo)";
                    detallesErrores.Add($"Registro #{indiceRegistro}: no se pudo convertir el QSO con {indicativo}.");
                    continue;
                }

                bool esDuplicado = await _repositorioQso.ExisteDuplicadoAsync(
                    qso.IndicativoContacto,
                    qso.FechaHoraInicio,
                    qso.Frecuencia,
                    qso.Modo,
                    ct).ConfigureAwait(false);

                if (esDuplicado)
                {
                    duplicados++;
                    detallesErrores.Add(
                        $"Registro #{indiceRegistro}: duplicado ({qso.IndicativoContacto.Valor} " +
                        $"{qso.FechaHoraInicio:yyyy-MM-dd HH:mm}).");
                    continue;
                }

                await _repositorioQso.AgregarAsync(qso, ct).ConfigureAwait(false);
                importados++;
            }

            if (importados > 0)
            {
                await _unidadDeTrabajo.GuardarCambiosAsync(ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Importacion ADIF completada: {Importados} nuevos, {Duplicados} duplicados, {Errores} errores",
                importados, duplicados, errores);

            return Ok(new
            {
                importados,
                errores = errores + duplicados,
                detalles = detallesErrores
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al importar archivo ADIF: {NombreArchivo}", archivo.FileName);
            return BadRequest(new { mensaje = $"Error al procesar el archivo ADIF: {ex.Message}" });
        }
    }
}
