namespace RadioAficionado.Servicio.Dtos;

/// <summary>
/// Linea de espectro comprimida para enviar al waterfall via SignalR.
/// Las magnitudes se convierten de double[] a byte[] para reducir ancho de banda.
/// </summary>
/// <param name="MagnitudesDb">Magnitudes en dB mapeadas a bytes (0-255).</param>
/// <param name="ResolucionHz">Resolucion de frecuencia en Hz por bin.</param>
/// <param name="FrecuenciaMinHz">Frecuencia minima representada en Hz.</param>
public sealed record LineaEspectroDto(
    byte[] MagnitudesDb,
    double ResolucionHz,
    double FrecuenciaMinHz);
