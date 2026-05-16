namespace RadioAficionado.Servicio.Dtos;

/// <summary>
/// Spot DX para enviar al cliente via SignalR.
/// </summary>
/// <param name="Spotteador">Indicativo de quien publico el spot.</param>
/// <param name="Dx">Indicativo de la estacion DX.</param>
/// <param name="FrecuenciaHz">Frecuencia en Hz.</param>
/// <param name="Comentario">Comentario del spot.</param>
/// <param name="HoraUtc">Hora UTC del spot.</param>
public sealed record SpotDxDto(
    string Spotteador,
    string Dx,
    long FrecuenciaHz,
    string Comentario,
    DateTime HoraUtc);
