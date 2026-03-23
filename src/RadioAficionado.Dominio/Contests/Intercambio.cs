namespace RadioAficionado.Dominio.Contests;

/// <summary>
/// Representa el intercambio de información durante un QSO de contest.
/// Contiene los datos enviados y recibidos que validan el contacto.
/// </summary>
/// <param name="SenalEnviada">Reporte de señal enviado (RST), por ejemplo "59" o "599".</param>
/// <param name="SenalRecibida">Reporte de señal recibido (RST), por ejemplo "59" o "599".</param>
/// <param name="NumeroSerial">Número serial secuencial del QSO. Null si el contest no usa seriales.</param>
/// <param name="Zona">Número de zona CQ o ITU. Null si el contest no usa zonas.</param>
/// <param name="Estado">Abreviatura de estado, provincia o sección. Null si no aplica.</param>
/// <param name="Edad">Edad del operador. Null si el contest no usa edad (solo All Asian).</param>
/// <param name="Clase">Clase de participación (por ejemplo, "2A" en Field Day). Null si no aplica.</param>
/// <param name="Qth">Ubicación QTH del operador. Null si no aplica.</param>
public record Intercambio(
    string SenalEnviada,
    string SenalRecibida,
    int? NumeroSerial = null,
    int? Zona = null,
    string? Estado = null,
    int? Edad = null,
    string? Clase = null,
    string? Qth = null)
{
    /// <summary>
    /// Valida que el intercambio contiene los datos requeridos según el tipo de contest.
    /// </summary>
    /// <param name="tipoIntercambio">El tipo de intercambio esperado por el contest.</param>
    /// <returns>True si el intercambio es válido para el tipo especificado.</returns>
    public bool EsValido(TipoIntercambio tipoIntercambio)
    {
        if (string.IsNullOrWhiteSpace(SenalEnviada) || string.IsNullOrWhiteSpace(SenalRecibida))
        {
            return false;
        }

        return tipoIntercambio switch
        {
            TipoIntercambio.RstZona => Zona.HasValue,
            TipoIntercambio.RstEstado => !string.IsNullOrWhiteSpace(Estado),
            TipoIntercambio.RstSerial => NumeroSerial.HasValue,
            TipoIntercambio.RstEdad => Edad.HasValue,
            TipoIntercambio.RstSeccion => !string.IsNullOrWhiteSpace(Estado),
            TipoIntercambio.RstZonaItu => Zona.HasValue,
            TipoIntercambio.ClaseTransmisores => !string.IsNullOrWhiteSpace(Clase),
            TipoIntercambio.RstQth => !string.IsNullOrWhiteSpace(Qth),
            _ => false
        };
    }
}
