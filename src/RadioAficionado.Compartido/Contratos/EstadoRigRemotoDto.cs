namespace RadioAficionado.Compartido.Contratos;

/// <summary>
/// DTO del estado del rig compartido entre Web y Servicio para el relay remoto.
/// Subconjunto del EstadoRigDto del Servicio, serializable sin dependencias nativas.
/// </summary>
/// <param name="FrecuenciaHz">Frecuencia del VFO activo en Hz.</param>
/// <param name="FrecuenciaDisplay">Frecuencia formateada para mostrar.</param>
/// <param name="Modo">Modo de operacion actual.</param>
/// <param name="Banda">Banda actual.</param>
/// <param name="NivelSenal">Nivel del S-meter (0-15).</param>
/// <param name="Transmitiendo">Si el PTT esta activado.</param>
/// <param name="VfoActivo">VFO activo ('A' o 'B').</param>
/// <param name="PotenciaVatios">Potencia de transmision en vatios.</param>
/// <param name="SplitActivo">Si el split esta activo.</param>
/// <param name="Conectado">Si el rig esta conectado.</param>
/// <param name="Swr">SWR durante transmision (1.0 = perfecto, 0 = no disponible).</param>
/// <param name="Alc">ALC como porcentaje (0-100, 0 = no disponible).</param>
public sealed record EstadoRigRemotoDto(
    long FrecuenciaHz,
    string FrecuenciaDisplay,
    string Modo,
    string Banda,
    int NivelSenal,
    bool Transmitiendo,
    char VfoActivo,
    double PotenciaVatios,
    bool SplitActivo,
    bool Conectado,
    double Swr,
    double Alc);
