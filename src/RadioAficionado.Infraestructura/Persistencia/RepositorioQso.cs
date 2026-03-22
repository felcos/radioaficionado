using Microsoft.EntityFrameworkCore;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using Frecuencia = RadioAficionado.Dominio.ObjetosDeValor.Frecuencia;

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

    /// <inheritdoc />
    public async Task<ResultadoPaginado<Qso>> ObtenerPaginadoAsync(
        int pagina, int tamano, FiltroQso? filtro, CancellationToken ct)
    {
        IQueryable<Qso> consulta = AplicarFiltro(_contexto.Qsos.AsQueryable(), filtro);

        int total = await consulta.CountAsync(ct);

        List<Qso> elementos = await consulta
            .OrderByDescending(q => q.FechaHoraInicio)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        return new ResultadoPaginado<Qso>(elementos.AsReadOnly(), total);
    }

    /// <inheritdoc />
    public async Task<int> ContarConFiltroAsync(FiltroQso? filtro, CancellationToken ct)
    {
        IQueryable<Qso> consulta = AplicarFiltro(_contexto.Qsos.AsQueryable(), filtro);
        return await consulta.CountAsync(ct);
    }

    /// <summary>
    /// Aplica los filtros de FiltroQso a una consulta IQueryable de QSOs.
    /// </summary>
    private static IQueryable<Qso> AplicarFiltro(IQueryable<Qso> consulta, FiltroQso? filtro)
    {
        if (filtro is null)
        {
            return consulta;
        }

        if (!string.IsNullOrWhiteSpace(filtro.Indicativo))
        {
            string indicativoBusqueda = filtro.Indicativo.ToUpperInvariant();
            consulta = consulta.Where(q =>
                q.IndicativoContacto.Valor.ToUpper().Contains(indicativoBusqueda) ||
                q.IndicativoPropio.Valor.ToUpper().Contains(indicativoBusqueda));
        }

        if (filtro.Modo.HasValue)
        {
            ModoOperacion modo = filtro.Modo.Value;
            consulta = consulta.Where(q => q.Modo == modo);
        }

        if (filtro.FechaDesde.HasValue)
        {
            DateTimeOffset fechaDesde = filtro.FechaDesde.Value;
            consulta = consulta.Where(q => q.FechaHoraInicio >= fechaDesde);
        }

        if (filtro.FechaHasta.HasValue)
        {
            DateTimeOffset fechaHasta = filtro.FechaHasta.Value;
            consulta = consulta.Where(q => q.FechaHoraInicio <= fechaHasta);
        }

        if (filtro.Banda.HasValue)
        {
            BandaRadio banda = filtro.Banda.Value;
            (Frecuencia inicio, Frecuencia fin) = banda.ObtenerRangoFrecuencia();
            consulta = consulta.Where(q =>
                q.Frecuencia.Hz >= inicio.Hz && q.Frecuencia.Hz <= fin.Hz);
        }

        return consulta;
    }
}
