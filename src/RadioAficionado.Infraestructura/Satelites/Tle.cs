namespace RadioAficionado.Infraestructura.Satelites;

/// <summary>
/// Elementos TLE (Two-Line Element) parseados de un satélite.
/// Contiene los parámetros orbitales necesarios para calcular la posición mediante SGP4.
/// </summary>
/// <param name="Nombre">Nombre del satélite (línea 0 del TLE).</param>
/// <param name="NumeroNorad">Número de catálogo NORAD.</param>
/// <param name="Epoca">Fecha y hora UTC de la época del TLE.</param>
/// <param name="InclinacionGrados">Inclinación orbital en grados.</param>
/// <param name="AscensionRectaGrados">Ascensión recta del nodo ascendente (RAAN) en grados.</param>
/// <param name="Excentricidad">Excentricidad orbital (0 = circular, &lt;1 = elíptica).</param>
/// <param name="ArgumentoPerigeoGrados">Argumento del perigeo en grados.</param>
/// <param name="AnomaliaMediaGrados">Anomalía media en grados en la época.</param>
/// <param name="MovimientoMedioRevDia">Movimiento medio en revoluciones por día.</param>
/// <param name="DerivadaMovimientoMedio">Primera derivada del movimiento medio (rev/día²) / 2.</param>
/// <param name="CoeficienteArrastre">Coeficiente de arrastre BSTAR.</param>
/// <param name="NumeroRevolucion">Número de revolución en la época.</param>
public sealed record Tle(
    string Nombre,
    int NumeroNorad,
    DateTime Epoca,
    double InclinacionGrados,
    double AscensionRectaGrados,
    double Excentricidad,
    double ArgumentoPerigeoGrados,
    double AnomaliaMediaGrados,
    double MovimientoMedioRevDia,
    double DerivadaMovimientoMedio,
    double CoeficienteArrastre,
    int NumeroRevolucion);
