using Microsoft.EntityFrameworkCore;
using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Persistencia;

/// <summary>
/// Implementación del repositorio de activaciones usando EF Core.
/// </summary>
public sealed class RepositorioActivaciones : IRepositorioActivaciones
{
    private readonly ContextoRadioAficionado _contexto;

    /// <summary>
    /// Crea una nueva instancia del repositorio de activaciones.
    /// </summary>
    /// <param name="contexto">Contexto de base de datos.</param>
    public RepositorioActivaciones(ContextoRadioAficionado contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Activacion activacion, CancellationToken ct)
    {
        await _contexto.Activaciones.AddAsync(activacion, ct);
    }

    /// <inheritdoc />
    public Task ActualizarAsync(Activacion activacion, CancellationToken ct)
    {
        _contexto.Activaciones.Update(activacion);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<Activacion?> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        return await _contexto.Activaciones
            .Include(a => a.Qsos)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Activacion>> ObtenerPorTipoAsync(TipoActivacion tipo, CancellationToken ct)
    {
        List<Activacion> activaciones = await _contexto.Activaciones
            .Include(a => a.Qsos)
            .Where(a => a.TipoActivacion == tipo)
            .OrderByDescending(a => a.FechaInicio)
            .ToListAsync(ct);

        return activaciones.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<Activacion?> ObtenerActivaAsync(CancellationToken ct)
    {
        return await _contexto.Activaciones
            .Include(a => a.Qsos)
            .FirstOrDefaultAsync(a => a.EstadoActivacion == EstadoActivacion.EnCurso, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Activacion>> ObtenerTodasAsync(CancellationToken ct)
    {
        List<Activacion> activaciones = await _contexto.Activaciones
            .Include(a => a.Qsos)
            .OrderByDescending(a => a.FechaInicio)
            .ToListAsync(ct);

        return activaciones.AsReadOnly();
    }

    /// <inheritdoc />
    public Task EliminarAsync(Activacion activacion, CancellationToken ct)
    {
        _contexto.Activaciones.Remove(activacion);
        return Task.CompletedTask;
    }
}
