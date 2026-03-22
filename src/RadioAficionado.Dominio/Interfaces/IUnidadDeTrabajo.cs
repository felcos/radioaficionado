namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Unidad de trabajo que coordina la persistencia de cambios en el repositorio.
/// </summary>
public interface IUnidadDeTrabajo
{
    /// <summary>
    /// Persiste todos los cambios pendientes en el almacenamiento.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Número de entidades afectadas.</returns>
    Task<int> GuardarCambiosAsync(CancellationToken ct);
}
