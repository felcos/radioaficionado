using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using Serilog;

namespace RadioAficionado.Infraestructura.Activaciones;

/// <summary>
/// Implementación del servicio de activaciones de radio.
/// Coordina la creación, inicio, completado y consulta de activaciones usando el repositorio.
/// </summary>
public class ServicioActivaciones : IServicioActivaciones
{
    private readonly IRepositorioActivaciones _repositorioActivaciones;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ILogger _logger;

    /// <summary>
    /// Crea una nueva instancia de <see cref="ServicioActivaciones"/>.
    /// </summary>
    /// <param name="repositorioActivaciones">Repositorio de activaciones.</param>
    /// <param name="unidadDeTrabajo">Unidad de trabajo para persistencia.</param>
    /// <param name="logger">Logger de Serilog.</param>
    public ServicioActivaciones(
        IRepositorioActivaciones repositorioActivaciones,
        IUnidadDeTrabajo unidadDeTrabajo,
        ILogger logger)
    {
        _repositorioActivaciones = repositorioActivaciones ?? throw new ArgumentNullException(nameof(repositorioActivaciones));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Activacion> CrearActivacionAsync(
        TipoActivacion tipoActivacion,
        string referencia,
        Indicativo indicativoActivador,
        Localizador? localizador = null,
        string? notas = null,
        CancellationToken ct = default)
    {
        _logger.Information(
            "Creando activación {TipoActivacion} con referencia {Referencia} para {Indicativo}",
            tipoActivacion, referencia, indicativoActivador.Valor);

        Activacion activacion = Activacion.Crear(
            tipoActivacion,
            referencia,
            indicativoActivador,
            localizador,
            notas);

        await _repositorioActivaciones.AgregarAsync(activacion, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _logger.Information(
            "Activación {IdActivacion} creada exitosamente con referencia {Referencia}",
            activacion.Id, activacion.Referencia);

        return activacion;
    }

    /// <inheritdoc />
    public async Task<Activacion> IniciarAsync(Guid idActivacion, CancellationToken ct = default)
    {
        Activacion? activacion = await _repositorioActivaciones.ObtenerPorIdAsync(idActivacion, ct);

        if (activacion is null)
        {
            throw new InvalidOperationException(
                $"No se encontró la activación con Id '{idActivacion}'.");
        }

        // Verificar que no haya otra activación en curso
        Activacion? activacionActiva = await _repositorioActivaciones.ObtenerActivaAsync(ct);

        if (activacionActiva is not null)
        {
            throw new InvalidOperationException(
                $"Ya existe una activación en curso ({activacionActiva.Referencia}). " +
                "Complétela o cancélela antes de iniciar otra.");
        }

        activacion.IniciarActivacion();

        await _repositorioActivaciones.ActualizarAsync(activacion, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _logger.Information(
            "Activación {IdActivacion} ({Referencia}) iniciada",
            activacion.Id, activacion.Referencia);

        return activacion;
    }

    /// <inheritdoc />
    public async Task<Activacion> CompletarAsync(Guid idActivacion, CancellationToken ct = default)
    {
        Activacion? activacion = await _repositorioActivaciones.ObtenerPorIdAsync(idActivacion, ct);

        if (activacion is null)
        {
            throw new InvalidOperationException(
                $"No se encontró la activación con Id '{idActivacion}'.");
        }

        activacion.CompletarActivacion();

        await _repositorioActivaciones.ActualizarAsync(activacion, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _logger.Information(
            "Activación {IdActivacion} ({Referencia}) completada con {TotalQsos} QSOs",
            activacion.Id, activacion.Referencia, activacion.Qsos.Count);

        return activacion;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Activacion>> ObtenerActivacionesAsync(
        TipoActivacion tipo,
        CancellationToken ct = default)
    {
        return await _repositorioActivaciones.ObtenerPorTipoAsync(tipo, ct);
    }

    /// <inheritdoc />
    public async Task<Activacion?> ObtenerActivacionActualAsync(CancellationToken ct = default)
    {
        return await _repositorioActivaciones.ObtenerActivaAsync(ct);
    }
}
