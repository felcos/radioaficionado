using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.Api.Dtos;
using RadioAficionado.Web.Api.Mapeadores;

namespace RadioAficionado.Web.Api;

/// <summary>
/// Controlador API REST para operaciones CRUD y sincronización de QSOs.
/// Diseñado para la comunicación entre la aplicación de escritorio y el servidor web.
/// </summary>
[ApiController]
[Route("api/qsos")]
[Authorize]
public sealed class QsoApiController : ControllerBase
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ILogger<QsoApiController> _logger;

    /// <summary>
    /// Crea una nueva instancia del controlador API de QSOs.
    /// </summary>
    /// <param name="repositorioQso">Repositorio de QSOs.</param>
    /// <param name="unidadDeTrabajo">Unidad de trabajo para persistir cambios.</param>
    /// <param name="logger">Logger para registro de eventos.</param>
    public QsoApiController(
        IRepositorioQso repositorioQso,
        IUnidadDeTrabajo unidadDeTrabajo,
        ILogger<QsoApiController> logger)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene una lista paginada de QSOs con filtros opcionales.
    /// </summary>
    /// <param name="filtro">Parámetros de filtrado y paginación.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista paginada de QSOs.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerTodos(
        [FromQuery] FiltroQsoDto filtro,
        CancellationToken ct)
    {
        int pagina = Math.Max(1, filtro.Pagina);
        int tamano = Math.Clamp(filtro.Tamano, 1, 200);

        FiltroQso? filtroDominio = filtro.AFiltro();

        ResultadoPaginado<Qso> resultado = await _repositorioQso.ObtenerPaginadoAsync(
            pagina, tamano, filtroDominio, ct);

        IReadOnlyList<QsoDto> dtos = resultado.Elementos.ADtos();

        return Ok(new
        {
            Datos = dtos,
            resultado.TotalElementos,
            Pagina = pagina,
            Tamano = tamano,
            TotalPaginas = (int)Math.Ceiling((double)resultado.TotalElementos / tamano)
        });
    }

    /// <summary>
    /// Obtiene un QSO por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del QSO.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El QSO encontrado o 404 si no existe.</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(QsoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(Guid id, CancellationToken ct)
    {
        Qso? qso = await _repositorioQso.ObtenerPorIdAsync(id, ct);

        if (qso is null)
        {
            return NotFound(new { Mensaje = $"No se encontró un QSO con el identificador '{id}'." });
        }

        return Ok(qso.ADto());
    }

    /// <summary>
    /// Crea un nuevo QSO enviado desde la aplicación de escritorio.
    /// </summary>
    /// <param name="dto">DTO con los datos del QSO a crear.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El QSO creado con su identificador asignado.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(QsoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] QsoDto dto, CancellationToken ct)
    {
        if (dto is null)
        {
            return BadRequest(new { Mensaje = "El cuerpo de la petición no puede estar vacío." });
        }

        try
        {
            Qso qso = dto.AEntidad();

            await _repositorioQso.AgregarAsync(qso, ct);
            await _unidadDeTrabajo.GuardarCambiosAsync(ct);

            _logger.LogInformation(
                "QSO creado: {Id} — {IndicativoPropio} ↔ {IndicativoContacto}",
                qso.Id, qso.IndicativoPropio.Valor, qso.IndicativoContacto.Valor);

            QsoDto resultado = qso.ADto();

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = qso.Id },
                resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Sincroniza un lote de QSOs desde la aplicación de escritorio.
    /// Detecta duplicados comparando indicativo contactado, fecha/hora, frecuencia y modo.
    /// </summary>
    /// <param name="dtos">Lista de QSOs a sincronizar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado de la sincronización con contadores y errores.</returns>
    [HttpPost("sincronizar")]
    [ProducesResponseType(typeof(ResultadoSincronizacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sincronizar(
        [FromBody] List<QsoDto> dtos,
        CancellationToken ct)
    {
        if (dtos is null || dtos.Count == 0)
        {
            return BadRequest(new { Mensaje = "La lista de QSOs no puede estar vacía." });
        }

        ResultadoSincronizacionDto resultado = new()
        {
            QsosRecibidos = dtos.Count
        };

        foreach (QsoDto dto in dtos)
        {
            try
            {
                Qso qso = dto.AEntidad();

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
            catch (ArgumentException ex)
            {
                resultado.Errores.Add(
                    $"QSO con {dto.IndicativoContacto} a las {dto.FechaHoraInicio:u}: {ex.Message}");
            }
        }

        if (resultado.QsosNuevos > 0)
        {
            await _unidadDeTrabajo.GuardarCambiosAsync(ct);
        }

        _logger.LogInformation(
            "Sincronización completada: {Recibidos} recibidos, {Nuevos} nuevos, {Duplicados} duplicados, {Errores} errores",
            resultado.QsosRecibidos, resultado.QsosNuevos, resultado.QsosDuplicados, resultado.Errores.Count);

        return Ok(resultado);
    }

    /// <summary>
    /// Elimina un QSO por su identificador único.
    /// </summary>
    /// <param name="id">Identificador del QSO a eliminar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>204 si se eliminó correctamente, 404 si no existe.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        Qso? qso = await _repositorioQso.ObtenerPorIdAsync(id, ct);

        if (qso is null)
        {
            return NotFound(new { Mensaje = $"No se encontró un QSO con el identificador '{id}'." });
        }

        await _repositorioQso.EliminarAsync(qso, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _logger.LogInformation(
            "QSO eliminado: {Id} — {IndicativoPropio} ↔ {IndicativoContacto}",
            id, qso.IndicativoPropio.Valor, qso.IndicativoContacto.Valor);

        return NoContent();
    }
}
