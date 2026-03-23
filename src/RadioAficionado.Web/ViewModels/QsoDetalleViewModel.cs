namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel para la vista de detalle de un QSO individual.
/// </summary>
public class QsoDetalleViewModel
{
    /// <summary>
    /// Identificador único del QSO.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Indicativo de la estación propia.
    /// </summary>
    public string IndicativoPropio { get; set; } = string.Empty;

    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    public string IndicativoContacto { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de inicio del contacto.
    /// </summary>
    public DateTimeOffset FechaHoraInicio { get; set; }

    /// <summary>
    /// Fecha y hora de fin del contacto. Null si aún está en curso.
    /// </summary>
    public DateTimeOffset? FechaHoraFin { get; set; }

    /// <summary>
    /// Frecuencia en formato legible.
    /// </summary>
    public string Frecuencia { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la banda (ej: "20 metros"). Null si no corresponde a ninguna banda.
    /// </summary>
    public string? Banda { get; set; }

    /// <summary>
    /// Modo de operación utilizado.
    /// </summary>
    public string Modo { get; set; } = string.Empty;

    /// <summary>
    /// Reporte de señal enviado.
    /// </summary>
    public string SenalEnviada { get; set; } = string.Empty;

    /// <summary>
    /// Reporte de señal recibido.
    /// </summary>
    public string SenalRecibida { get; set; } = string.Empty;

    /// <summary>
    /// Potencia en vatios. Null si no se registró.
    /// </summary>
    public double? Potencia { get; set; }

    /// <summary>
    /// Localizador Maidenhead del contacto. Null si no se proporcionó.
    /// </summary>
    public string? LocalizadorContacto { get; set; }

    /// <summary>
    /// Notas adicionales. Null si no hay.
    /// </summary>
    public string? Notas { get; set; }

    /// <summary>
    /// Fecha y hora de creación del registro.
    /// </summary>
    public DateTimeOffset FechaCreacion { get; set; }

    /// <summary>
    /// Fecha y hora de la última modificación. Null si nunca se modificó.
    /// </summary>
    public DateTimeOffset? FechaModificacion { get; set; }

    /// <summary>
    /// Indica si el QSO está completado (tiene fecha de fin).
    /// </summary>
    public bool EstaCompletado => FechaHoraFin.HasValue;

    /// <summary>
    /// Duración del contacto si está completado.
    /// </summary>
    public TimeSpan? Duracion => FechaHoraFin.HasValue
        ? FechaHoraFin.Value - FechaHoraInicio
        : null;
}
