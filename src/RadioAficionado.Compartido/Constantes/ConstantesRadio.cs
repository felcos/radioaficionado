namespace RadioAficionado.Compartido.Constantes;

/// <summary>
/// Constantes físicas y de radio utilizadas en cálculos del sistema.
/// </summary>
public static class ConstantesRadio
{
    /// <summary>
    /// Velocidad de la luz en el vacío, en metros por segundo.
    /// </summary>
    public const double VelocidadDeLaLuzMetrosPorSegundo = 299_792_458.0;

    /// <summary>
    /// Radio de la Tierra en kilómetros (valor medio).
    /// </summary>
    public const double RadioDeLaTierraKm = 6_371.0;

    /// <summary>
    /// Factor de conversión de grados a radianes.
    /// </summary>
    public const double GradosARadianes = Math.PI / 180.0;

    /// <summary>
    /// Factor de conversión de radianes a grados.
    /// </summary>
    public const double RadianesAGrados = 180.0 / Math.PI;

    /// <summary>
    /// Frecuencia mínima válida en Hz (1 Hz).
    /// </summary>
    public const long FrecuenciaMinimaHz = 1;

    /// <summary>
    /// Frecuencia máxima válida en Hz (300 GHz — límite superior de microondas).
    /// </summary>
    public const long FrecuenciaMaximaHz = 300_000_000_000;
}
