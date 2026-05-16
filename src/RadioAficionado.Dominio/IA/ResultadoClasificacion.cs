using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.IA;

/// <summary>
/// Resultado de la clasificacion de una senal de radio mediante IA.
/// Incluye el modo detectado con mayor confianza y alternativas posibles.
/// </summary>
/// <param name="ModoDetectado">Modo de operacion detectado con mayor probabilidad.</param>
/// <param name="Confianza">Nivel de confianza en la deteccion (0.0 a 1.0).</param>
/// <param name="ModosAlternativos">Lista de modos alternativos ordenados por confianza descendente.</param>
public sealed record ResultadoClasificacion(
    ModoOperacion ModoDetectado,
    double Confianza,
    IReadOnlyList<ModoAlternativo> ModosAlternativos);

/// <summary>
/// Modo alternativo detectado durante la clasificacion con su nivel de confianza.
/// </summary>
/// <param name="Modo">Modo de operacion alternativo.</param>
/// <param name="Confianza">Nivel de confianza para este modo (0.0 a 1.0).</param>
public sealed record ModoAlternativo(
    ModoOperacion Modo,
    double Confianza);
