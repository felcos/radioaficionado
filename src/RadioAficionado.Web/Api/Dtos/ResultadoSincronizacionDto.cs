namespace RadioAficionado.Web.Api.Dtos;

/// <summary>
/// DTO con el resultado de una operación de sincronización de QSOs.
/// </summary>
public sealed class ResultadoSincronizacionDto
{
    /// <summary>
    /// Cantidad total de QSOs recibidos en la petición.
    /// </summary>
    public int QsosRecibidos { get; set; }

    /// <summary>
    /// Cantidad de QSOs nuevos que fueron insertados correctamente.
    /// </summary>
    public int QsosNuevos { get; set; }

    /// <summary>
    /// Cantidad de QSOs que ya existían y fueron detectados como duplicados.
    /// </summary>
    public int QsosDuplicados { get; set; }

    /// <summary>
    /// Lista de errores encontrados durante la sincronización.
    /// Cada entrada describe un QSO que no pudo ser procesado y el motivo.
    /// </summary>
    public List<string> Errores { get; set; } = new();
}
