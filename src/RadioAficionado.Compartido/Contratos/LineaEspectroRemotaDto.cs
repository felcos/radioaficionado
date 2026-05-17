namespace RadioAficionado.Compartido.Contratos;

/// <summary>
/// DTO compartido para retransmitir lineas de espectro (waterfall) entre
/// el servicio local y el servidor web remoto via SignalR.
/// </summary>
/// <param name="MagnitudesComprimidas">Magnitudes en dB mapeadas a bytes (0-255).</param>
/// <param name="ResolucionHz">Resolucion de frecuencia en Hz por bin.</param>
/// <param name="FrecuenciaMinHz">Frecuencia minima representada en Hz.</param>
public sealed record LineaEspectroRemotaDto(
    byte[] MagnitudesComprimidas,
    double ResolucionHz,
    long FrecuenciaMinHz);
