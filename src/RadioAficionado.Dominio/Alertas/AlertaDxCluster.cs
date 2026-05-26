using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Alertas;

/// <summary>
/// Tipo de condicion que puede disparar una alerta.
/// </summary>
public enum TipoAlerta
{
    /// <summary>Alerta cuando aparece un spot de una entidad DXCC no trabajada.</summary>
    DxccNueva,

    /// <summary>Alerta cuando aparece un spot en una banda especifica.</summary>
    Banda,

    /// <summary>Alerta cuando aparece un spot con un modo especifico.</summary>
    Modo,

    /// <summary>Alerta cuando aparece un spot de un indicativo concreto.</summary>
    Indicativo,

    /// <summary>Alerta cuando aparece un spot en una banda+modo combinados.</summary>
    BandaYModo
}

/// <summary>
/// Regla de alerta configurable por el operador. Define las condiciones
/// bajo las cuales se debe notificar al usuario cuando llega un spot DX.
/// </summary>
public sealed class ReglaAlerta
{
    /// <summary>Identificador unico de la regla.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nombre descriptivo de la alerta (ej: "DXCC nuevas en 20m").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Tipo de condicion que evalua esta regla.</summary>
    public TipoAlerta Tipo { get; set; }

    /// <summary>Banda a filtrar (solo para tipos Banda y BandaYModo). Null = cualquier banda.</summary>
    public BandaRadio? Banda { get; set; }

    /// <summary>Modo a filtrar (solo para tipos Modo y BandaYModo). Null = cualquier modo.</summary>
    public string? Modo { get; set; }

    /// <summary>Indicativo parcial a buscar (solo para tipo Indicativo).</summary>
    public string? Indicativo { get; set; }

    /// <summary>Indica si la regla esta activa.</summary>
    public bool Activa { get; set; } = true;

    /// <summary>Indica si debe emitir sonido al dispararse.</summary>
    public bool ConSonido { get; set; } = true;

    /// <summary>Fecha de creacion de la regla.</summary>
    public DateTimeOffset FechaCreacion { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Resultado cuando un spot DX cumple una regla de alerta.
/// </summary>
/// <param name="Regla">Regla que se cumplio.</param>
/// <param name="Spotteador">Indicativo del spotter.</param>
/// <param name="Dx">Indicativo de la estacion DX.</param>
/// <param name="Frecuencia">Frecuencia del spot.</param>
/// <param name="Comentario">Comentario del spot.</param>
/// <param name="Hora">Hora UTC del spot.</param>
/// <param name="EntidadDxcc">Entidad DXCC del indicativo DX (si se resolvio).</param>
/// <param name="Mensaje">Mensaje legible para mostrar al usuario.</param>
public sealed record ResultadoAlerta(
    ReglaAlerta Regla,
    string Spotteador,
    string Dx,
    Frecuencia Frecuencia,
    string Comentario,
    DateTime Hora,
    EntidadDxcc? EntidadDxcc,
    string Mensaje);
