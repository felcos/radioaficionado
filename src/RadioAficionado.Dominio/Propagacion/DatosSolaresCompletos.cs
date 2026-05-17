namespace RadioAficionado.Dominio.Propagacion;

/// <summary>
/// Datos solares completos combinando fuentes NOAA y N0NBH.
/// </summary>
public sealed record DatosSolaresCompletos(
    int Sfi,
    int Kp,
    int Ap,
    double NumeroManchasSolares,
    string RayosX,
    double VelocidadVientoSolar,
    double Bt,
    double BzGsm,
    double FlujoProtones,
    double FlujoElectrones,
    string CampoGeomagnetico,
    string RuidoSenal,
    IReadOnlyList<CondicionBandaHf> CondicionesHf,
    CondicionesVhf CondicionesVhf,
    EscalasEspaciales Escalas,
    IReadOnlyList<AlertaSolar> AlertasActivas,
    DateTime FechaActualizacion);

/// <summary>
/// Condición de propagación para una banda HF específica (día y noche).
/// </summary>
public sealed record CondicionBandaHf(string Banda, string Dia, string Noche);

/// <summary>
/// Condiciones de propagación VHF (aurora y E-skip).
/// </summary>
public sealed record CondicionesVhf(string AuroraVhf, string ESkipEuropa, string ESkipNorteamerica);

/// <summary>
/// Escalas NOAA de clima espacial (R=radio, S=solar, G=geomagnética).
/// </summary>
public sealed record EscalasEspaciales(
    string EscalaR,
    string EscalaS,
    string EscalaG,
    int ProbRadiacionMenor,
    int ProbRadiacionMayor,
    int ProbTormentaSolar);

/// <summary>
/// Alerta solar emitida por NOAA SWPC.
/// </summary>
public sealed record AlertaSolar(string Codigo, string Mensaje, DateTime FechaEmision);

/// <summary>
/// Punto de datos histórico del índice de flujo solar (SFI) de 10 cm.
/// </summary>
public sealed record PuntoHistoricoSfi(DateTime Fecha, int Sfi);

/// <summary>
/// Punto de datos histórico del índice planetario Kp.
/// </summary>
public sealed record PuntoHistoricoKp(DateTime Fecha, double Kp);
