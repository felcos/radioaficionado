using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Adif;
using RadioAficionado.Web.Api.Dtos;

namespace RadioAficionado.Web.Api;

/// <summary>
/// Controlador API REST para importación y exportación de archivos ADIF.
/// Permite la sincronización de QSOs mediante el formato estándar ADIF 3.1.4.
/// </summary>
[ApiController]
[Route("api/adif")]
[Authorize]
public sealed class AdifApiController : ControllerBase
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ILogger<AdifApiController> _logger;

    /// <summary>
    /// Tamaño máximo permitido para archivos ADIF (5 MB).
    /// </summary>
    private const long TamanoMaximoArchivo = 5 * 1024 * 1024;

    /// <summary>
    /// Crea una nueva instancia del controlador API de ADIF.
    /// </summary>
    /// <param name="repositorioQso">Repositorio de QSOs.</param>
    /// <param name="unidadDeTrabajo">Unidad de trabajo para persistir cambios.</param>
    /// <param name="logger">Logger para registro de eventos.</param>
    public AdifApiController(
        IRepositorioQso repositorioQso,
        IUnidadDeTrabajo unidadDeTrabajo,
        ILogger<AdifApiController> logger)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Importa QSOs desde un archivo ADIF. Parsea el archivo, convierte a entidades,
    /// detecta duplicados y persiste los QSOs nuevos.
    /// </summary>
    /// <param name="archivo">Archivo ADIF (.adi o .adif) enviado como form-data.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado de la importación con contadores detallados.</returns>
    [HttpPost("importar")]
    [ProducesResponseType(typeof(ResultadoSincronizacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(TamanoMaximoArchivo)]
    public async Task<IActionResult> Importar(IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { Mensaje = "No se recibió ningún archivo o está vacío." });
        }

        if (archivo.Length > TamanoMaximoArchivo)
        {
            return BadRequest(new { Mensaje = "El archivo excede el tamaño máximo permitido de 5 MB." });
        }

        string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not ".adi" and not ".adif")
        {
            return BadRequest(new { Mensaje = "Solo se aceptan archivos con extensión .adi o .adif." });
        }

        ResultadoSincronizacionDto resultado = new();

        try
        {
            using Stream flujo = archivo.OpenReadStream();
            ResultadoParserAdif resultadoParseo = await ParserAdif.ParsearDesdeStreamAsync(flujo, ct);

            _logger.LogInformation(
                "Archivo ADIF parseado: {TotalRegistros} registros, {Advertencias} advertencias",
                resultadoParseo.TotalRegistros, resultadoParseo.Advertencias.Count);

            (IReadOnlyList<Qso> qsos, int descartados) = ConvertidorAdifQso.ConvertirListaAQsos(resultadoParseo.Registros);

            resultado.QsosRecibidos = resultadoParseo.TotalRegistros;

            if (descartados > 0)
            {
                resultado.Errores.Add($"{descartados} registro(s) ADIF no pudieron ser convertidos a QSO.");
            }

            foreach (Qso qso in qsos)
            {
                bool esDuplicado = await _repositorioQso.ExisteDuplicadoAsync(
                    qso.IndicativoContacto,
                    qso.FechaHoraInicio,
                    qso.Frecuencia,
                    qso.Modo,
                    ct);

                if (esDuplicado)
                {
                    resultado.QsosDuplicados++;
                    continue;
                }

                await _repositorioQso.AgregarAsync(qso, ct);
                resultado.QsosNuevos++;
            }

            if (resultado.QsosNuevos > 0)
            {
                await _unidadDeTrabajo.GuardarCambiosAsync(ct);
            }

            // Añadir advertencias del parser como errores informativos
            foreach (string advertencia in resultadoParseo.Advertencias)
            {
                resultado.Errores.Add($"Advertencia ADIF: {advertencia}");
            }

            _logger.LogInformation(
                "Importación ADIF completada: {Nuevos} nuevos, {Duplicados} duplicados, {Errores} errores",
                resultado.QsosNuevos, resultado.QsosDuplicados, resultado.Errores.Count);

            return Ok(resultado);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al importar archivo ADIF: {NombreArchivo}", archivo.FileName);
            return BadRequest(new { Mensaje = $"Error al procesar el archivo ADIF: {ex.Message}" });
        }
    }

    /// <summary>
    /// Exporta todos los QSOs del usuario en formato ADIF 3.1.4.
    /// Retorna el archivo como descarga con tipo MIME adecuado.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Archivo ADIF con todos los QSOs.</returns>
    [HttpGet("exportar")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Exportar(CancellationToken ct)
    {
        IReadOnlyList<Qso> qsos = await _repositorioQso.ObtenerTodosAsync(ct);

        IReadOnlyList<RegistroAdif> registrosAdif = ConvertidorAdifQso.ConvertirListaAAdif(qsos);

        string contenidoAdif = GeneradorAdif.Generar(registrosAdif);

        byte[] bytes = Encoding.UTF8.GetBytes(contenidoAdif);

        string nombreArchivo = $"radioaficionado-export-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.adi";

        _logger.LogInformation(
            "Exportación ADIF completada: {TotalQsos} QSOs exportados",
            qsos.Count);

        return File(bytes, "application/octet-stream", nombreArchivo);
    }
}
