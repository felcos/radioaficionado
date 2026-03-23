using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Propagacion;

/// <summary>
/// Prediccion de propagacion para una banda HF especifica en un momento y ubicacion dados.
/// </summary>
/// <param name="Banda">Banda de radioaficionado evaluada.</param>
/// <param name="Nivel">Nivel de calidad de propagacion estimado.</param>
/// <param name="Descripcion">Descripcion textual de las condiciones esperadas.</param>
/// <param name="MejorHoraInicio">Hora UTC de inicio de la mejor ventana de propagacion.</param>
/// <param name="MejorHoraFin">Hora UTC de fin de la mejor ventana de propagacion.</param>
/// <param name="RegionesAlcanzables">Lista de regiones del mundo alcanzables con esta banda.</param>
public sealed record PrediccionBanda(
    BandaRadio Banda,
    NivelPropagacion Nivel,
    string Descripcion,
    TimeSpan MejorHoraInicio,
    TimeSpan MejorHoraFin,
    IReadOnlyList<string> RegionesAlcanzables);
