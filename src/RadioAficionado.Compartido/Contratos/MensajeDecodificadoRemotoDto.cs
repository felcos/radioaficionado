namespace RadioAficionado.Compartido.Contratos;

/// <summary>
/// DTO compartido para retransmitir mensajes decodificados (FT8, CW, etc.)
/// entre el servicio local y el servidor web remoto via SignalR.
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
/// <param name="Color">Color hex para el indicativo basado en estado DXCC.</param>
public sealed record MensajeDecodificadoRemotoDto(
    DateTime MarcaDeTiempo,
    int FrecuenciaAudioHz,
    int Snr,
    double DeltaTiempo,
    string Modo,
    string Texto,
    string? IndicativoEmisor,
    string? IndicativoDestinatario,
    string? Localizador,
    string? ReporteSenal,
    string Color);
