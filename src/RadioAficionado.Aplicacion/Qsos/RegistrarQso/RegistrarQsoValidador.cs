using FluentValidation;

namespace RadioAficionado.Aplicacion.Qsos.RegistrarQso;

/// <summary>
/// Validador de FluentValidation para el comando RegistrarQso.
/// </summary>
public sealed class RegistrarQsoValidador : AbstractValidator<RegistrarQsoComando>
{
    /// <summary>
    /// Configura las reglas de validación.
    /// </summary>
    public RegistrarQsoValidador()
    {
        RuleFor(x => x.IndicativoPropio)
            .NotEmpty().WithMessage("El indicativo propio es obligatorio.")
            .MinimumLength(3).WithMessage("El indicativo propio debe tener al menos 3 caracteres.")
            .MaximumLength(10).WithMessage("El indicativo propio no puede exceder 10 caracteres.");

        RuleFor(x => x.IndicativoContacto)
            .NotEmpty().WithMessage("El indicativo del contacto es obligatorio.")
            .MinimumLength(3).WithMessage("El indicativo del contacto debe tener al menos 3 caracteres.")
            .MaximumLength(10).WithMessage("El indicativo del contacto no puede exceder 10 caracteres.");

        RuleFor(x => x.FechaHoraInicio)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow).WithMessage("La fecha de inicio no puede ser futura.");

        RuleFor(x => x.FrecuenciaHz)
            .GreaterThan(0).WithMessage("La frecuencia debe ser positiva.");

        RuleFor(x => x.SenalEnviada)
            .NotEmpty().WithMessage("El reporte de señal enviado es obligatorio.");

        RuleFor(x => x.Modo)
            .IsInEnum().WithMessage("El modo de operación no es válido.");

        RuleFor(x => x.Potencia)
            .GreaterThan(0).When(x => x.Potencia.HasValue)
            .WithMessage("La potencia debe ser positiva.");

        RuleFor(x => x.LocalizadorContacto)
            .Matches(@"^[A-Ra-r]{2}[0-9]{2}([A-Xa-x]{2}([0-9]{2})?)?$")
            .When(x => !string.IsNullOrWhiteSpace(x.LocalizadorContacto))
            .WithMessage("El localizador Maidenhead no tiene un formato válido.");
    }
}
