using System.ComponentModel.DataAnnotations;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel con DataAnnotations para el formulario de creacion de un nuevo QSO.
/// </summary>
public class CrearQsoViewModel
{
    /// <summary>
    /// Indicativo de la estacion contactada.
    /// </summary>
    [Required(ErrorMessage = "El indicativo del contacto es obligatorio.")]
    [StringLength(10, MinimumLength = 3, ErrorMessage = "El indicativo debe tener entre 3 y 10 caracteres.")]
    [Display(Name = "Indicativo Contacto")]
    public string IndicativoContacto { get; set; } = string.Empty;

    /// <summary>
    /// Frecuencia utilizada en MHz.
    /// </summary>
    [Required(ErrorMessage = "La frecuencia es obligatoria.")]
    [Range(0.001, 300000, ErrorMessage = "La frecuencia debe estar entre 0.001 y 300000 MHz.")]
    [Display(Name = "Frecuencia (MHz)")]
    public double FrecuenciaMHz { get; set; }

    /// <summary>
    /// Modo de operacion utilizado durante el contacto.
    /// </summary>
    [Required(ErrorMessage = "El modo de operacion es obligatorio.")]
    [Display(Name = "Modo")]
    public ModoOperacion Modo { get; set; }

    /// <summary>
    /// Fecha y hora de inicio del contacto (UTC).
    /// </summary>
    [Required(ErrorMessage = "La fecha y hora de inicio es obligatoria.")]
    [Display(Name = "Fecha/Hora Inicio (UTC)")]
    public DateTime FechaHoraInicio { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora de fin del contacto (UTC). Opcional.
    /// </summary>
    [Display(Name = "Fecha/Hora Fin (UTC)")]
    public DateTime? FechaHoraFin { get; set; }

    /// <summary>
    /// Reporte de senal enviado a la otra estacion.
    /// </summary>
    [Required(ErrorMessage = "La senal enviada es obligatoria.")]
    [StringLength(10, ErrorMessage = "La senal enviada no puede superar 10 caracteres.")]
    [Display(Name = "Senal Enviada")]
    public string SenalEnviada { get; set; } = "59";

    /// <summary>
    /// Reporte de senal recibido de la otra estacion. Opcional.
    /// </summary>
    [StringLength(10, ErrorMessage = "La senal recibida no puede superar 10 caracteres.")]
    [Display(Name = "Senal Recibida")]
    public string? SenalRecibida { get; set; }

    /// <summary>
    /// Potencia de transmision en vatios. Opcional.
    /// </summary>
    [Range(0.1, 100000, ErrorMessage = "La potencia debe estar entre 0.1 y 100000 vatios.")]
    [Display(Name = "Potencia (W)")]
    public double? Potencia { get; set; }

    /// <summary>
    /// Localizador Maidenhead de la estacion contactada. Opcional.
    /// </summary>
    [StringLength(8, ErrorMessage = "El localizador no puede superar 8 caracteres.")]
    [Display(Name = "Localizador Contacto")]
    public string? LocalizadorContacto { get; set; }

    /// <summary>
    /// Notas adicionales sobre el contacto. Opcional.
    /// </summary>
    [StringLength(500, ErrorMessage = "Las notas no pueden superar 500 caracteres.")]
    [Display(Name = "Notas")]
    public string? Notas { get; set; }

    /// <summary>
    /// Lista de modos de operacion disponibles para el formulario.
    /// </summary>
    public IReadOnlyList<ModoOperacion> ModosDisponibles { get; set; } = [];
}
