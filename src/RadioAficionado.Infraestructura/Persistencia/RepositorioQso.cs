using Microsoft.EntityFrameworkCore;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Persistencia;

/// <summary>
/// Implementación del repositorio de QSOs usando EF Core.
/// </summary>
public sealed class RepositorioQso : IRepositorioQso
{
    private readonly ContextoRadioAficionado _contexto;

    /// <summary>
    /// Crea una nueva instancia del repositorio de QSOs.
    /// </summary>
    /// <param name="contexto">Contexto de base de datos.</param>
    public RepositorioQso(ContextoRadioAficionado contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    /// <inheritdoc />
    public async Task<Qso?> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        return await _contexto.Qsos.FindAsync(new object[] { id }, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Qso>> ObtenerTodosAsync(CancellationToken ct)
    {
        List<Qso> qsos = await _contexto.Qsos
            .OrderByDescending(q => q.FechaHoraInicio)
            .ToListAsync(ct);

        return qsos.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Qso>> BuscarPorIndicativoAsync(Indicativo indicativo, CancellationToken ct)
    {
        List<Qso> qsos = await _contexto.Qsos
            .Where(q => q.IndicativoPropio == indicativo || q.IndicativoContacto == indicativo)
            .OrderByDescending(q => q.FechaHoraInicio)
            .ToListAsync(ct);

        return qsos.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Qso qso, CancellationToken ct)
    {
        await _contexto.Qsos.AddAsync(qso, ct);
    }

    /// <inheritdoc />
    public Task ActualizarAsync(Qso qso, CancellationToken ct)
    {
        _contexto.Qsos.Update(qso);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> ContarAsync(CancellationToken ct)
    {
        return await _contexto.Qsos.CountAsync(ct);
    }
}
