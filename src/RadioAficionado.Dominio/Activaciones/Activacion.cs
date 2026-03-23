using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Activaciones;

/// <summary>
/// Representa una activación de radio en un programa como POTA, SOTA, WWFF o IOTA.
/// Una activación agrupa los QSOs realizados durante la operación desde una ubicación específica.
/// </summary>
public class Activacion
{
    private readonly List<Qso> _qsos = new();

    /// <summary>
    /// Identificador único de la activación.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Tipo de programa de activación (POTA, SOTA, WWFF, IOTA).
    /// </summary>
    public TipoActivacion TipoActivacion { get; private set; }

    /// <summary>
    /// Referencia del lugar activado (formato según el tipo de activación).
    /// </summary>
    public string Referencia { get; private set; } = string.Empty;

    /// <summary>
    /// Indicativo del operador que realiza la activación.
    /// </summary>
    public Indicativo IndicativoActivador { get; private set; }

    /// <summary>
    /// Fecha y hora de inicio de la activación (UTC).
    /// </summary>
    public DateTimeOffset FechaInicio { get; private set; }

    /// <summary>
    /// Fecha y hora de fin de la activación (UTC). Null si aún no ha finalizado.
    /// </summary>
    public DateTimeOffset? FechaFin { get; private set; }

    /// <summary>
    /// Localizador Maidenhead de la ubicación de la activación. Null si no se proporcionó.
    /// </summary>
    public Localizador? Localizador { get; private set; }

    /// <summary>
    /// Notas adicionales sobre la activación.
    /// </summary>
    public string? Notas { get; private set; }

    /// <summary>
    /// Estado actual de la activación.
    /// </summary>
    public EstadoActivacion EstadoActivacion { get; private set; }

    /// <summary>
    /// Fecha y hora de creación del registro.
    /// </summary>
    public DateTimeOffset FechaCreacion { get; private set; }

    /// <summary>
    /// Fecha y hora de la última modificación del registro. Null si nunca se ha modificado.
    /// </summary>
    public DateTimeOffset? FechaModificacion { get; private set; }

    /// <summary>
    /// QSOs realizados durante esta activación (solo lectura).
    /// </summary>
    public IReadOnlyList<Qso> Qsos => _qsos.AsReadOnly();

    /// <summary>
    /// Constructor sin parámetros requerido por Entity Framework Core.
    /// </summary>
    private Activacion()
    {
    }

    /// <summary>
    /// Crea una nueva activación validando la referencia según el tipo de programa.
    /// </summary>
    /// <param name="tipoActivacion">Tipo de programa de activación.</param>
    /// <param name="referencia">Referencia del lugar (se valida según el tipo).</param>
    /// <param name="indicativoActivador">Indicativo del operador activador.</param>
    /// <param name="localizador">Localizador Maidenhead de la ubicación (opcional).</param>
    /// <param name="notas">Notas adicionales (opcional).</param>
    /// <returns>Nueva instancia de <see cref="Activacion"/>.</returns>
    /// <exception cref="ArgumentException">Si la referencia no tiene el formato correcto para el tipo de activación.</exception>
    public static Activacion Crear(
        TipoActivacion tipoActivacion,
        string referencia,
        Indicativo indicativoActivador,
        Localizador? localizador = null,
        string? notas = null)
    {
        if (string.IsNullOrWhiteSpace(referencia))
        {
            throw new ArgumentException(
                "La referencia de activación no puede ser nula ni estar vacía.",
                nameof(referencia));
        }

        string referenciaNormalizada = referencia.Trim().ToUpperInvariant();

        ValidarReferencia(tipoActivacion, referenciaNormalizada);

        Activacion activacion = new()
        {
            Id = Guid.NewGuid(),
            TipoActivacion = tipoActivacion,
            Referencia = referenciaNormalizada,
            IndicativoActivador = indicativoActivador,
            FechaInicio = DateTimeOffset.UtcNow,
            FechaFin = null,
            Localizador = localizador,
            Notas = notas?.Trim(),
            EstadoActivacion = EstadoActivacion.Planificada,
            FechaCreacion = DateTimeOffset.UtcNow,
            FechaModificacion = null
        };

        return activacion;
    }

    /// <summary>
    /// Inicia la activación cambiando su estado a EnCurso.
    /// Solo se puede iniciar una activación en estado Planificada.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la activación no está en estado Planificada.</exception>
    public void IniciarActivacion()
    {
        if (EstadoActivacion != EstadoActivacion.Planificada)
        {
            throw new InvalidOperationException(
                $"No se puede iniciar una activación en estado '{EstadoActivacion}'. " +
                "Solo se pueden iniciar activaciones en estado 'Planificada'.");
        }

        EstadoActivacion = EstadoActivacion.EnCurso;
        FechaInicio = DateTimeOffset.UtcNow;
        FechaModificacion = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Completa la activación cambiando su estado a Completada y estableciendo la fecha de fin.
    /// Solo se puede completar una activación en estado EnCurso.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la activación no está en estado EnCurso.</exception>
    public void CompletarActivacion()
    {
        if (EstadoActivacion != EstadoActivacion.EnCurso)
        {
            throw new InvalidOperationException(
                $"No se puede completar una activación en estado '{EstadoActivacion}'. " +
                "Solo se pueden completar activaciones en estado 'EnCurso'.");
        }

        EstadoActivacion = EstadoActivacion.Completada;
        FechaFin = DateTimeOffset.UtcNow;
        FechaModificacion = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cancela la activación cambiando su estado a Cancelada.
    /// No se puede cancelar una activación ya completada.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la activación está en estado Completada.</exception>
    public void CancelarActivacion()
    {
        if (EstadoActivacion == EstadoActivacion.Completada)
        {
            throw new InvalidOperationException(
                "No se puede cancelar una activación que ya ha sido completada.");
        }

        if (EstadoActivacion == EstadoActivacion.Cancelada)
        {
            throw new InvalidOperationException(
                "La activación ya está cancelada.");
        }

        EstadoActivacion = EstadoActivacion.Cancelada;
        FechaFin = DateTimeOffset.UtcNow;
        FechaModificacion = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Agrega un QSO a la activación. Solo se pueden agregar QSOs cuando la activación está en curso.
    /// </summary>
    /// <param name="qso">El QSO a agregar.</param>
    /// <exception cref="ArgumentNullException">Si el QSO es null.</exception>
    /// <exception cref="InvalidOperationException">Si la activación no está en estado EnCurso.</exception>
    public void AgregarQso(Qso qso)
    {
        if (qso is null)
        {
            throw new ArgumentNullException(nameof(qso),
                "El QSO no puede ser nulo.");
        }

        if (EstadoActivacion != EstadoActivacion.EnCurso)
        {
            throw new InvalidOperationException(
                $"No se pueden agregar QSOs a una activación en estado '{EstadoActivacion}'. " +
                "La activación debe estar en estado 'EnCurso'.");
        }

        _qsos.Add(qso);
        FechaModificacion = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Valida que la referencia tenga el formato correcto según el tipo de activación.
    /// </summary>
    /// <param name="tipo">Tipo de activación.</param>
    /// <param name="referencia">Referencia normalizada a validar.</param>
    /// <exception cref="ArgumentException">Si el formato no es válido para el tipo.</exception>
    private static void ValidarReferencia(TipoActivacion tipo, string referencia)
    {
        switch (tipo)
        {
            case TipoActivacion.Pota:
                // Valida creando el objeto de valor (lanza excepción si es inválido)
                _ = new ReferenciaPota(referencia);
                break;

            case TipoActivacion.Sota:
                // Valida creando el objeto de valor (lanza excepción si es inválido)
                _ = new ReferenciaSota(referencia);
                break;

            case TipoActivacion.Wwff:
            case TipoActivacion.Iota:
                // Los formatos WWFF e IOTA se validarán cuando se implementen sus objetos de valor.
                // Por ahora solo verificamos que no esté vacía (ya validado antes de llegar aquí).
                break;

            default:
                throw new ArgumentException(
                    $"Tipo de activación no soportado: '{tipo}'.",
                    nameof(tipo));
        }
    }
}
