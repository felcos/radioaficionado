namespace RadioAficionado.Servicio.Dtos;

/// <summary>
/// Estado actual del radio para enviar al cliente via SignalR.
/// </summary>
/// <param name="FrecuenciaHz">Frecuencia del VFO activo en Hz.</param>
/// <param name="FrecuenciaDisplay">Frecuencia formateada para mostrar (ej: "14.074.000").</param>
/// <param name="Modo">Modo de operacion actual (ej: "FT8", "USB").</param>
/// <param name="Banda">Banda actual (ej: "20m").</param>
/// <param name="NivelSenal">Nivel del S-meter (0-15).</param>
/// <param name="NivelSenalPorcentaje">Porcentaje del S-meter para la barra visual (0-100).</param>
/// <param name="Transmitiendo">Si el PTT esta activado.</param>
/// <param name="VfoActivo">VFO activo ('A' o 'B').</param>
/// <param name="PotenciaVatios">Potencia de transmision en vatios.</param>
/// <param name="SplitActivo">Si el split esta activo.</param>
/// <param name="Swr">SWR durante transmision (1.0 = perfecto, 0 = no disponible).</param>
/// <param name="Alc">ALC como porcentaje (0-100, 0 = no disponible).</param>
public sealed record EstadoRigDto(
    long FrecuenciaHz,
    string FrecuenciaDisplay,
    string Modo,
    string Banda,
    int NivelSenal,
    double NivelSenalPorcentaje,
    bool Transmitiendo,
    char VfoActivo,
    double PotenciaVatios,
    bool SplitActivo,
    double Swr,
    double Alc);
