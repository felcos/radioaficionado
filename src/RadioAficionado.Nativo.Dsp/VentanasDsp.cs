namespace RadioAficionado.Nativo.Dsp;

/// <summary>
/// Funciones de ventana (windowing) para procesamiento de señales.
/// Cada función modifica el <see cref="Span{T}"/> de datos in-place,
/// multiplicando cada muestra por el coeficiente de la ventana correspondiente.
/// </summary>
public static class VentanasDsp
{
    /// <summary>
    /// Aplica ventana Hann (también conocida como Hanning).
    /// Buena resolución de frecuencia con supresión moderada de lóbulos laterales (-31 dB).
    /// </summary>
    /// <param name="datos">Muestras sobre las que aplicar la ventana.</param>
    public static void AplicarHann(Span<double> datos)
    {
        int longitud = datos.Length;
        for (int i = 0; i < longitud; i++)
        {
            double coeficiente = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (longitud - 1)));
            datos[i] *= coeficiente;
        }
    }

    /// <summary>
    /// Aplica ventana Hamming.
    /// Similar a Hann pero con lóbulos laterales más bajos (-43 dB) a costa de mayor ancho del lóbulo principal.
    /// </summary>
    /// <param name="datos">Muestras sobre las que aplicar la ventana.</param>
    public static void AplicarHamming(Span<double> datos)
    {
        int longitud = datos.Length;
        for (int i = 0; i < longitud; i++)
        {
            double coeficiente = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / (longitud - 1));
            datos[i] *= coeficiente;
        }
    }

    /// <summary>
    /// Aplica ventana Blackman-Harris (4 términos).
    /// Excelente supresión de lóbulos laterales (-92 dB) pero ancho de lóbulo principal mayor.
    /// Ideal para detectar señales débiles cerca de señales fuertes.
    /// </summary>
    /// <param name="datos">Muestras sobre las que aplicar la ventana.</param>
    public static void AplicarBlackmanHarris(Span<double> datos)
    {
        int longitud = datos.Length;
        for (int i = 0; i < longitud; i++)
        {
            double angulo = 2.0 * Math.PI * i / (longitud - 1);
            double coeficiente = 0.35875
                - 0.48829 * Math.Cos(angulo)
                + 0.14128 * Math.Cos(2.0 * angulo)
                - 0.01168 * Math.Cos(3.0 * angulo);
            datos[i] *= coeficiente;
        }
    }
}
