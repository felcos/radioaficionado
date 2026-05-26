using Microsoft.AspNetCore.Mvc;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Servicio.Controllers;

/// <summary>
/// API REST para configuracion general y backup desde la UI web.
/// </summary>
[Route("api/configuracion")]
[ApiController]
public sealed class ConfiguracionApiController : ControllerBase
{
    private readonly IServicioConfiguracion _configuracion;
    private readonly IServicioBackup _backup;
    private readonly ILogger<ConfiguracionApiController> _logger;

    /// <summary>
    /// Crea el controlador API de configuracion.
    /// </summary>
    public ConfiguracionApiController(
        IServicioConfiguracion configuracion,
        IServicioBackup backup,
        ILogger<ConfiguracionApiController> logger)
    {
        _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
        _backup = backup ?? throw new ArgumentNullException(nameof(backup));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene las preferencias generales actuales.
    /// </summary>
    [HttpGet("preferencias")]
    public async Task<IActionResult> ObtenerPreferencias(CancellationToken ct)
    {
        Dominio.Configuracion.ConfiguracionCompleta config = await _configuracion.CargarAsync(ct);

        return Ok(new
        {
            formatoFecha = config.General.FormatoFecha,
            notasEstacion = config.Estacion.NotasEstacion,
            backupAutomatico = config.General.BackupAutomatico,
            maxBackups = config.General.MaxBackups
        });
    }

    /// <summary>
    /// Guarda las preferencias generales.
    /// </summary>
    [HttpPost("preferencias")]
    public async Task<IActionResult> GuardarPreferencias(
        [FromBody] PreferenciasDto dto,
        CancellationToken ct)
    {
        if (dto is null)
        {
            return BadRequest("El cuerpo de la solicitud no puede ser nulo.");
        }

        Dominio.Configuracion.ConfiguracionCompleta config = await _configuracion.CargarAsync(ct);

        if (!string.IsNullOrWhiteSpace(dto.FormatoFecha))
        {
            string[] formatosValidos = ["dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd"];
            if (!formatosValidos.Contains(dto.FormatoFecha))
            {
                return BadRequest($"Formato de fecha no valido: {dto.FormatoFecha}");
            }

            config.General.FormatoFecha = dto.FormatoFecha;
        }

        if (dto.NotasEstacion is not null)
        {
            config.Estacion.NotasEstacion = dto.NotasEstacion.Trim();
        }

        if (dto.BackupAutomatico.HasValue)
        {
            config.General.BackupAutomatico = dto.BackupAutomatico.Value;
        }

        await _configuracion.GuardarAsync(config, ct);

        _logger.LogInformation("Preferencias guardadas exitosamente.");
        return Ok(new { mensaje = "Preferencias guardadas." });
    }

    /// <summary>
    /// Crea un backup manual.
    /// </summary>
    [HttpPost("backup")]
    public async Task<IActionResult> CrearBackup(CancellationToken ct)
    {
        ResultadoBackup resultado = await _backup.CrearBackupAsync(ct);

        if (resultado.Exitoso)
        {
            // Limpiar backups antiguos
            Dominio.Configuracion.ConfiguracionCompleta config = await _configuracion.CargarAsync(ct);
            int eliminados = await _backup.LimpiarBackupsAntiguosAsync(config.General.MaxBackups, ct);

            if (eliminados > 0)
            {
                _logger.LogInformation("Limpieza post-backup: {Eliminados} backup(s) antiguos eliminados.", eliminados);
            }
        }

        return Ok(new { exitoso = resultado.Exitoso, mensaje = resultado.Mensaje });
    }

    /// <summary>
    /// Obtiene la lista de backups disponibles.
    /// </summary>
    [HttpGet("backups")]
    public IActionResult ObtenerBackups()
    {
        IReadOnlyList<string> backups = _backup.ObtenerBackupsDisponibles();
        return Ok(backups.Select(b => Path.GetFileName(b)));
    }
}

/// <summary>
/// DTO para guardar preferencias generales.
/// </summary>
/// <param name="FormatoFecha">Formato de fecha (dd/MM/yyyy, MM/dd/yyyy, yyyy-MM-dd).</param>
/// <param name="NotasEstacion">Notas libres sobre la estacion.</param>
/// <param name="BackupAutomatico">Si se activan los backups automaticos.</param>
public sealed record PreferenciasDto(
    string? FormatoFecha,
    string? NotasEstacion,
    bool? BackupAutomatico);
