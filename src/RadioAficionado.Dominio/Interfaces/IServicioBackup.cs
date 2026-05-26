namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Resultado de una operacion de backup.
/// </summary>
/// <param name="Exitoso">Si el backup se completo correctamente.</param>
/// <param name="RutaArchivo">Ruta del archivo de backup creado.</param>
/// <param name="Mensaje">Mensaje descriptivo del resultado.</param>
public sealed record ResultadoBackup(bool Exitoso, string RutaArchivo, string Mensaje);

/// <summary>
/// Servicio para crear y gestionar backups de la configuracion y base de datos local.
/// </summary>
/// <remarks>
/// <para><b>Para que sirve:</b> Crea copias de seguridad de la configuracion JSON y la base de datos SQLite local.</para>
/// <para><b>Como se usa:</b> Se inyecta por constructor. Se puede invocar manualmente con <see cref="CrearBackupAsync"/> o automaticamente al inicio.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Backup.ServicioBackup</c>.</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.Infraestructura.ConfiguracionServicios</c>.</para>
/// </remarks>
public interface IServicioBackup
{
    /// <summary>
    /// Crea un backup de los archivos especificados.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Resultado del backup con la ruta del archivo creado.</returns>
    Task<ResultadoBackup> CrearBackupAsync(CancellationToken ct = default);

    /// <summary>
    /// Elimina backups antiguos manteniendo solo los N mas recientes.
    /// </summary>
    /// <param name="maxRetener">Numero maximo de backups a retener.</param>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Numero de backups eliminados.</returns>
    Task<int> LimpiarBackupsAntiguosAsync(int maxRetener, CancellationToken ct = default);

    /// <summary>
    /// Obtiene la lista de backups disponibles, ordenados del mas reciente al mas antiguo.
    /// </summary>
    /// <returns>Lista de rutas de archivos de backup.</returns>
    IReadOnlyList<string> ObtenerBackupsDisponibles();
}
