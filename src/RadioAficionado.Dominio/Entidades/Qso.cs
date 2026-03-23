using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Entidades;

/// <summary>
/// Representa un contacto de radio (QSO) entre dos estaciones de radioaficionado.
/// </summary>
public class Qso
{
    /// <summary>
    /// Identificador único del QSO.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Indicativo de la estación propia.
    /// </summary>
    public Indicativo IndicativoPropio { get; private set; }

    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    public Indicativo IndicativoContacto { get; private set; }

    /// <summary>
    /// Fecha y hora de inicio del contacto (UTC).
    /// </summary>
    public DateTimeOffset FechaHoraInicio { get; private set; }

    /// <summary>
    /// Fecha y hora de fin del contacto (UTC). Null si aún está en curso.
    /// </summary>
    public DateTimeOffset? FechaHoraFin { get; private set; }

    /// <summary>
    /// Frecuencia utilizada durante el contacto.
    /// </summary>
    public Frecuencia Frecuencia { get; private set; }

    /// <summary>
    /// Modo de operación utilizado durante el contacto.
    /// </summary>
    public ModoOperacion Modo { get; private set; }

    /// <summary>
    /// Reporte de señal enviado a la otra estación (por ejemplo, "59", "599").
    /// </summary>
    public string SenalEnviada { get; private set; } = string.Empty;

    /// <summary>
    /// Reporte de señal recibido de la otra estación.
    /// </summary>
    public string SenalRecibida { get; private set; } = string.Empty;

    /// <summary>
    /// Potencia de transmisión en vatios. Null si no se registró.
    /// </summary>
    public double? Potencia { get; private set; }

    /// <summary>
    /// Localizador Maidenhead de la estación contactada. Null si no se proporcionó.
    /// </summary>
    public Localizador? LocalizadorContacto { get; private set; }

    /// <summary>
    /// Notas adicionales sobre el contacto.
    /// </summary>
    public string? Notas { get; private set; }

    /// <summary>
    /// Fecha y hora de creación del registro.
    /// </summary>
    public DateTimeOffset FechaCreacion { get; private set; }

    /// <summary>
    /// Fecha y hora de la última modificación del registro. Null si nunca se ha modificado.
    /// </summary>
    public DateTimeOffset? FechaModificacion { get; private set; }

    /// <summary>
    /// Indica si el QSO ya fue sincronizado con el servidor remoto.
    /// </summary>
    public bool Sincronizado { get; private set; }

    /// <summary>
    /// Constructor sin parámetros requerido por Entity Framework Core.
    /// </summary>
    private Qso()
    {
    }

    /// <summary>
    /// Crea un nuevo QSO con los datos proporcionados, aplicando validaciones de negocio.
    /// </summary>
    /// <param name="indicativoPropio">Indicativo de la estación propia.</param>
    /// <param name="indicativoContacto">Indicativo de la estación contactada.</param>
    /// <param name="fechaHoraInicio">Fecha y hora de inicio del contacto.</param>
    /// <param name="frecuencia">Frecuencia utilizada.</param>
    /// <param name="modo">Modo de operación.</param>
    /// <param name="senalEnviada">Reporte de señal enviado.</param>
    /// <param name="potencia">Potencia en vatios (opcional).</param>
    /// <param name="localizadorContacto">Localizador del contacto (opcional).</param>
    /// <param name="notas">Notas adicionales (opcional).</param>
    /// <returns>Nueva instancia de <see cref="Qso"/>.</returns>
    /// <exception cref="ArgumentException">Si algún dato obligatorio es inválido.</exception>
    public static Qso Crear(
        Indicativo indicativoPropio,
        Indicativo indicativoContacto,
        DateTimeOffset fechaHoraInicio,
        Frecuencia frecuencia,
        ModoOperacion modo,
        string senalEnviada,
        double? potencia = null,
        Localizador? localizadorContacto = null,
        string? notas = null)
    {
        if (string.IsNullOrWhiteSpace(senalEnviada))
        {
            throw new ArgumentException(
                "El reporte de señal enviado no puede ser nulo ni estar vacío.",
                nameof(senalEnviada));
        }

        if (potencia.HasValue && potencia.Value <= 0)
        {
            throw new ArgumentException(
                "La potencia debe ser un valor positivo en vatios.",
                nameof(potencia));
        }

        if (fechaHoraInicio > DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(
                "La fecha y hora de inicio no puede ser futura.",
                nameof(fechaHoraInicio));
        }

        Qso qso = new()
        {
            Id = Guid.NewGuid(),
            IndicativoPropio = indicativoPropio,
            IndicativoContacto = indicativoContacto,
            FechaHoraInicio = fechaHoraInicio,
            Frecuencia = frecuencia,
            Modo = modo,
            SenalEnviada = senalEnviada.Trim(),
            SenalRecibida = string.Empty,
            Potencia = potencia,
            LocalizadorContacto = localizadorContacto,
            Notas = notas?.Trim(),
            FechaCreacion = DateTimeOffset.UtcNow,
            FechaModificacion = null,
            Sincronizado = false
        };

        return qso;
    }

    /// <summary>
    /// Completa el QSO estableciendo la fecha de fin y el reporte de señal recibido.
    /// </summary>
    /// <param name="fechaFin">Fecha y hora de fin del contacto.</param>
    /// <param name="senalRecibida">Reporte de señal recibido de la otra estación.</param>
    /// <exception cref="ArgumentException">Si los datos son inválidos.</exception>
    /// <exception cref="InvalidOperationException">Si el QSO ya está completado.</exception>
    public void Completar(DateTimeOffset fechaFin, string senalRecibida)
    {
        if (FechaHoraFin.HasValue)
        {
            throw new InvalidOperationException(
                "Este QSO ya ha sido completado anteriormente.");
        }

        if (string.IsNullOrWhiteSpace(senalRecibida))
        {
            throw new ArgumentException(
                "El reporte de señal recibido no puede ser nulo ni estar vacío.",
                nameof(senalRecibida));
        }

        if (fechaFin < FechaHoraInicio)
        {
            throw new ArgumentException(
                "La fecha de fin no puede ser anterior a la fecha de inicio.",
                nameof(fechaFin));
        }

        FechaHoraFin = fechaFin;
        SenalRecibida = senalRecibida.Trim();
        FechaModificacion = DateTimeOffset.UtcNow;
        Sincronizado = false;
    }

    /// <summary>
    /// Marca el QSO como sincronizado con el servidor remoto.
    /// </summary>
    public void MarcarComoSincronizado()
    {
        Sincronizado = true;
    }

    /// <summary>
    /// Marca el QSO como pendiente de sincronización (por ejemplo, tras una edición local).
    /// </summary>
    public void MarcarComoPendienteDeSincronizacion()
    {
        Sincronizado = false;
        FechaModificacion = DateTimeOffset.UtcNow;
    }
}
