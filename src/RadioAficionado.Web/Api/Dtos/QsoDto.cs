using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Web.Api.Dtos;

/// <summary>
/// DTO para transferencia de datos de un QSO entre cliente y servidor.
/// </summary>
public sealed class QsoDto
{
    /// <summary>
    /// Identificador único del QSO. Null al crear uno nuevo desde el cliente.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Indicativo de la estación propia.
    /// </summary>
    public string IndicativoPropio { get; set; } = string.Empty;

    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    public string IndicativoContacto { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de inicio del contacto (UTC).
    /// </summary>
    public DateTimeOffset FechaHoraInicio { get; set; }

    /// <summary>
    /// Fecha y hora de fin del contacto (UTC). Null si no se completó.
    /// </summary>
    public DateTimeOffset? FechaHoraFin { get; set; }

    /// <summary>
    /// Frecuencia en megahercios (MHz).
    /// </summary>
    public double FrecuenciaMHz { get; set; }

    /// <summary>
    /// Modo de operación (FT8, SSB, CW, etc.).
    /// </summary>
    public string Modo { get; set; } = string.Empty;

    /// <summary>
    /// Reporte de señal enviado a la otra estación.
    /// </summary>
    public string SenalEnviada { get; set; } = string.Empty;

    /// <summary>
    /// Reporte de señal recibido de la otra estación.
    /// </summary>
    public string? SenalRecibida { get; set; }

    /// <summary>
    /// Potencia de transmisión en vatios. Null si no se registró.
    /// </summary>
    public double? Potencia { get; set; }

    /// <summary>
    /// Localizador Maidenhead de la estación contactada. Null si no se proporcionó.
    /// </summary>
    public string? LocalizadorContacto { get; set; }

    /// <summary>
    /// Notas adicionales sobre el contacto.
    /// </summary>
    public string? Notas { get; set; }

    /// <summary>
    /// Fecha y hora de creación del registro.
    /// </summary>
    public DateTimeOffset FechaCreacion { get; set; }

    /// <summary>
    /// Fecha y hora de la última modificación del registro. Null si nunca se ha modificado.
    /// </summary>
    public DateTimeOffset? FechaModificacion { get; set; }
}
