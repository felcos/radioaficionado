namespace RadioAficionado.Servicio.Dtos;

/// <summary>
/// Mensaje decodificado para enviar al cliente via SignalR.
/// Incluye color del indicativo basado en estado DXCC.
/// </summary>
/// <param name="MarcaDeTiempo">Marca de tiempo UTC de la decodificacion.</param>
/// <param name="FrecuenciaAudioHz">Frecuencia de audio dentro de la banda pasante.</param>
/// <param name="Snr">Relacion señal/ruido en dB.</param>
/// <param name="DeltaTiempo">Delta de tiempo en segundos.</param>
/// <param name="Modo">Modo digital que genero el mensaje.</param>
/// <param name="Texto">Texto completo del mensaje decodificado.</param>
/// <param name="IndicativoEmisor">Indicativo del emisor (si se pudo extraer).</param>
/// <param name="IndicativoDestinatario">Indicativo del destinatario (si se pudo extraer).</param>
/// <param name="Localizador">Localizador grid (si se pudo extraer).</param>
/// <param name="ReporteSenal">Reporte de señal (si se pudo extraer).</param>
/// <param name="ColorIndicativo">Color hex para el indicativo basado en estado DXCC.</param>
public sealed record MensajeDecodificadoDto(
    DateTimeOffset MarcaDeTiempo,
    int FrecuenciaAudioHz,
    int Snr,
    double DeltaTiempo,
    string Modo,
    string Texto,
    string? IndicativoEmisor,
    string? IndicativoDestinatario,
    string? Localizador,
    string? ReporteSenal,
    string ColorIndicativo);
