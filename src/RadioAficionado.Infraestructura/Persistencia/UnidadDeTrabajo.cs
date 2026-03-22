using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Infraestructura.Persistencia;

/// <summary>
/// Implementación de la unidad de trabajo usando EF Core.
/// Coordina la persistencia de cambios en el contexto de base de datos.
/// </summary>
public sealed class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly ContextoRadioAficionado _contexto;

    /// <summary>
    /// Crea una nueva instancia de la unidad de trabajo.
    /// </summary>
    /// <param name="contexto">Contexto de base de datos.</param>
    public UnidadDeTrabajo(ContextoRadioAficionado contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    /// <inheritdoc />
    public async Task<int> GuardarCambiosAsync(CancellationToken ct)
    {
        return await _contexto.SaveChangesAsync(ct);
    }
}
