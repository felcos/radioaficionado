namespace RadioAficionado.Nativo.Dsp.Interfaces;

/// <summary>
/// Interfaz para implementaciones de FFT. Permite intercambiar entre
/// la implementación managed y FFTW3 nativa en el futuro.
/// </summary>
public interface ITransformadaFourier : IDisposable
{
    /// <summary>
    /// Tamaño de la FFT (debe ser potencia de 2).
    /// </summary>
    int Tamano { get; }

    /// <summary>
    /// Calcula la FFT directa de una señal real.
    /// </summary>
    /// <param name="entrada">Señal de entrada (muestras reales).</param>
    /// <returns>Espectro complejo como pares [re0, im0, re1, im1, ...] con N valores (N/2 pares complejos no redundantes + componentes DC y Nyquist).</returns>
    double[] Calcular(ReadOnlySpan<double> entrada);

    /// <summary>
    /// Calcula el espectro de magnitud en dB.
    /// </summary>
    /// <param name="entrada">Señal de entrada.</param>
    /// <returns>Magnitudes en dB para cada bin (solo mitad positiva: N/2+1 valores).</returns>
    double[] CalcularMagnitudDb(ReadOnlySpan<double> entrada);
}
