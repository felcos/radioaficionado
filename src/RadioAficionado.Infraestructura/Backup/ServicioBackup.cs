using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Infraestructura.Backup;

/// <summary>
/// Implementacion de <see cref="IServicioBackup"/> que copia los archivos de configuracion
/// y base de datos local a una carpeta de backups con marca de tiempo.
/// Ruta: %APPDATA%/RadioAficionado/backups/ (Windows)
///       ~/.config/RadioAficionado/backups/ (Linux/Mac)
/// </summary>
public sealed class ServicioBackup : IServicioBackup
{
    private readonly string _carpetaBackups;
    private readonly string _carpetaDatos;
    private readonly ILogger<ServicioBackup> _logger;

    /// <summary>
    /// Crea una nueva instancia del servicio de backup.
    /// </summary>
    /// <param name="logger">Logger para registrar operaciones.</param>
    public ServicioBackup(ILogger<ServicioBackup> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        string carpetaApp = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RadioAficionado");
        _carpetaDatos = carpetaApp;
        _carpetaBackups = Path.Combine(carpetaApp, "backups");
    }

    /// <summary>
    /// Constructor con ruta personalizada para tests.
    /// </summary>
    /// <param name="carpetaDatos">Carpeta donde estan los archivos originales.</param>
    /// <param name="carpetaBackups">Carpeta destino de los backups.</param>
    /// <param name="logger">Logger.</param>
    public ServicioBackup(string carpetaDatos, string carpetaBackups, ILogger<ServicioBackup> logger)
    {
        if (string.IsNullOrWhiteSpace(carpetaDatos))
        {
            throw new ArgumentException("La carpeta de datos no puede ser nula ni vacia.", nameof(carpetaDatos));
        }

        if (string.IsNullOrWhiteSpace(carpetaBackups))
        {
            throw new ArgumentException("La carpeta de backups no puede ser nula ni vacia.", nameof(carpetaBackups));
        }

        _carpetaDatos = carpetaDatos;
        _carpetaBackups = carpetaBackups;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ResultadoBackup> CrearBackupAsync(CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(_carpetaBackups))
            {
                Directory.CreateDirectory(_carpetaBackups);
                _logger.LogInformation("Carpeta de backups creada: {Carpeta}.", _carpetaBackups);
            }

            string marcaTiempo = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string carpetaDestino = Path.Combine(_carpetaBackups, $"backup_{marcaTiempo}");
            Directory.CreateDirectory(carpetaDestino);

            int archivosCopiados = 0;

            // Copiar configuracion.json
            string rutaConfig = Path.Combine(_carpetaDatos, "configuracion.json");
            if (File.Exists(rutaConfig))
            {
                string destino = Path.Combine(carpetaDestino, "configuracion.json");
                await CopiarArchivoAsync(rutaConfig, destino, ct).ConfigureAwait(false);
                archivosCopiados++;
            }

            // Copiar base de datos SQLite (cualquier .db en la carpeta)
            if (Directory.Exists(_carpetaDatos))
            {
                foreach (string archivoBd in Directory.GetFiles(_carpetaDatos, "*.db"))
                {
                    ct.ThrowIfCancellationRequested();
                    string nombreArchivo = Path.GetFileName(archivoBd);
                    string destino = Path.Combine(carpetaDestino, nombreArchivo);
                    await CopiarArchivoAsync(archivoBd, destino, ct).ConfigureAwait(false);
                    archivosCopiados++;
                }
            }

            if (archivosCopiados == 0)
            {
                _logger.LogWarning("No se encontraron archivos para backup en {Carpeta}.", _carpetaDatos);
                // Limpiar la carpeta vacia
                Directory.Delete(carpetaDestino, true);
                return new ResultadoBackup(false, string.Empty, "No se encontraron archivos para respaldar.");
            }

            _logger.LogInformation("Backup creado exitosamente en {Carpeta} ({Archivos} archivos).", carpetaDestino, archivosCopiados);
            return new ResultadoBackup(true, carpetaDestino, $"Backup creado: {archivosCopiados} archivo(s) respaldados.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear backup.");
            return new ResultadoBackup(false, string.Empty, $"Error al crear backup: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<int> LimpiarBackupsAntiguosAsync(int maxRetener, CancellationToken ct = default)
    {
        if (maxRetener < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetener), "Debe retener al menos 1 backup.");
        }

        if (!Directory.Exists(_carpetaBackups))
        {
            return Task.FromResult(0);
        }

        DirectoryInfo[] carpetas = new DirectoryInfo(_carpetaBackups)
            .GetDirectories("backup_*")
            .OrderByDescending(d => d.Name)
            .ToArray();

        int eliminados = 0;

        for (int i = maxRetener; i < carpetas.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                carpetas[i].Delete(true);
                eliminados++;
                _logger.LogInformation("Backup antiguo eliminado: {Carpeta}.", carpetas[i].Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el backup {Carpeta}.", carpetas[i].Name);
            }
        }

        if (eliminados > 0)
        {
            _logger.LogInformation("Limpieza de backups: {Eliminados} backup(s) antiguos eliminados.", eliminados);
        }

        return Task.FromResult(eliminados);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ObtenerBackupsDisponibles()
    {
        if (!Directory.Exists(_carpetaBackups))
        {
            return Array.Empty<string>();
        }

        return new DirectoryInfo(_carpetaBackups)
            .GetDirectories("backup_*")
            .OrderByDescending(d => d.Name)
            .Select(d => d.FullName)
            .ToList()
            .AsReadOnly();
    }

    private static async Task CopiarArchivoAsync(string origen, string destino, CancellationToken ct)
    {
        await using FileStream fsOrigen = new(origen, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await using FileStream fsDestino = new(destino, FileMode.Create, FileAccess.Write, FileShare.None);
        await fsOrigen.CopyToAsync(fsDestino, ct).ConfigureAwait(false);
    }
}
