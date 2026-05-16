using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.IA;

/// <summary>
/// Resultado de la prediccion de propagacion generada por un modelo de IA.
/// Contiene la probabilidad de apertura de una banda y datos complementarios.
/// </summary>
/// <param name="Banda">Banda de radioaficionado evaluada.</param>
/// <param name="ProbabilidadApertura">Probabilidad de que la banda este abierta (0.0 a 1.0).</param>
/// <param name="NivelConfianza">Nivel de confianza del modelo en la prediccion (0.0 a 1.0).</param>
/// <param name="HoraOptima">Hora UTC optima estimada para operar en esta banda, si se puede determinar.</param>
/// <param name="MufEstimado">Frecuencia maxima utilizable estimada en MHz, si se puede calcular.</param>
public sealed record PrediccionPropagacionIa(
    BandaRadio Banda,
    double ProbabilidadApertura,
    double NivelConfianza,
    TimeOnly? HoraOptima,
    double? MufEstimado);
