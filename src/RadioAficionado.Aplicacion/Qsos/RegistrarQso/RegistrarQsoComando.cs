using MediatR;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Aplicacion.Qsos.RegistrarQso;

/// <summary>
/// Comando para registrar un nuevo QSO (contacto de radio).
/// </summary>
public sealed class RegistrarQsoComando : IRequest<RegistrarQsoResultado>
{
    /// <summary>Indicativo de la estación propia.</summary>
    public required string IndicativoPropio { get; init; }

    /// <summary>Indicativo de la estación contactada.</summary>
    public required string IndicativoContacto { get; init; }

    /// <summary>Fecha y hora de inicio del contacto (UTC).</summary>
    public required DateTimeOffset FechaHoraInicio { get; init; }

    /// <summary>Frecuencia en Hz.</summary>
    public required long FrecuenciaHz { get; init; }

    /// <summary>Modo de operación.</summary>
    public required ModoOperacion Modo { get; init; }

    /// <summary>Reporte de señal enviado.</summary>
    public required string SenalEnviada { get; init; }

    /// <summary>Reporte de señal recibido (vacío si QSO aún no completado).</summary>
    public string SenalRecibida { get; init; } = string.Empty;

    /// <summary>Potencia en vatios (opcional).</summary>
    public double? Potencia { get; init; }

    /// <summary>Localizador Maidenhead del contacto (opcional).</summary>
    public string? LocalizadorContacto { get; init; }

    /// <summary>Notas adicionales (opcional).</summary>
    public string? Notas { get; init; }
}

/// <summary>
/// Resultado del registro de un QSO.
/// </summary>
public sealed class RegistrarQsoResultado
{
    /// <summary>Identificador del QSO creado.</summary>
    public Guid QsoId { get; init; }

    /// <summary>Si el registro fue exitoso.</summary>
    public bool Exitoso { get; init; }

    /// <summary>Mensaje de error si no fue exitoso.</summary>
    public string? Error { get; init; }

    /// <summary>Crea un resultado exitoso.</summary>
    public static RegistrarQsoResultado Exito(Guid qsoId)
    {
        return new RegistrarQsoResultado { QsoId = qsoId, Exitoso = true };
    }

    /// <summary>Crea un resultado fallido.</summary>
    public static RegistrarQsoResultado Fallo(string error)
    {
        return new RegistrarQsoResultado { Exitoso = false, Error = error };
    }
}
