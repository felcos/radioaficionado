namespace RadioAficionado.Nativo.ModosDigitales.Cw;

/// <summary>
/// Implementa el algoritmo de Goertzel para deteccion de un tono especifico en una senal de audio.
/// Mas eficiente que una FFT completa cuando solo se necesita detectar una unica frecuencia,
/// ya que opera en O(N) con constante muy baja frente a O(N log N) de la FFT.
/// </summary>
public static class FiltroGoertzel
{
    /// <summary>
    /// Calcula la magnitud del tono especificado dentro del bloque de muestras usando el algoritmo de Goertzel.
    /// </summary>
    /// <param name="muestras">Bloque de muestras de audio PCM de 16 bits.</param>
    /// <param name="frecuenciaTono">Frecuencia del tono a detectar en Hz.</param>
    /// <param name="frecuenciaMuestreo">Frecuencia de muestreo del audio en Hz.</param>
    /// <returns>
    /// Magnitud relativa del tono detectado. Valores mas altos indican mayor presencia del tono.
    /// El valor se normaliza dividiendo por el numero de muestras al cuadrado para independizarlo del tamano del bloque.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Si la frecuencia del tono es negativa o cero, o si la frecuencia de muestreo es menor o igual a cero.
    /// </exception>
    public static double CalcularMagnitud(ReadOnlySpan<short> muestras, double frecuenciaTono, int frecuenciaMuestreo)
    {
        if (frecuenciaTono <= 0)
        {
            throw new ArgumentException("La frecuencia del tono debe ser mayor que cero.", nameof(frecuenciaTono));
        }

        if (frecuenciaMuestreo <= 0)
        {
            throw new ArgumentException("La frecuencia de muestreo debe ser mayor que cero.", nameof(frecuenciaMuestreo));
        }

        if (muestras.Length == 0)
        {
            return 0.0;
        }

        int longitudBloque = muestras.Length;

        // k = indice de frecuencia Goertzel (no necesita ser entero)
        double k = 0.5 + (longitudBloque * frecuenciaTono / frecuenciaMuestreo);
        double omega = 2.0 * Math.PI * k / longitudBloque;
        double coeficiente = 2.0 * Math.Cos(omega);

        double s0 = 0.0;
        double s1 = 0.0;
        double s2 = 0.0;

        // Iteracion principal del algoritmo de Goertzel
        for (int i = 0; i < longitudBloque; i++)
        {
            s0 = coeficiente * s1 - s2 + muestras[i];
            s2 = s1;
            s1 = s0;
        }

        // Calcular magnitud al cuadrado: |X(k)|^2
        double magnitudCuadrada = s1 * s1 + s2 * s2 - coeficiente * s1 * s2;

        // Normalizar por el tamano del bloque al cuadrado para que sea independiente del tamano
        double magnitudNormalizada = magnitudCuadrada / ((double)longitudBloque * longitudBloque);

        return magnitudNormalizada;
    }

    /// <summary>
    /// Calcula la magnitud del tono especificado a partir de muestras en formato double (normalizadas entre -1.0 y 1.0).
    /// </summary>
    /// <param name="muestras">Bloque de muestras de audio normalizadas.</param>
    /// <param name="frecuenciaTono">Frecuencia del tono a detectar en Hz.</param>
    /// <param name="frecuenciaMuestreo">Frecuencia de muestreo del audio en Hz.</param>
    /// <returns>Magnitud relativa normalizada del tono detectado.</returns>
    /// <exception cref="ArgumentException">
    /// Si la frecuencia del tono es negativa o cero, o si la frecuencia de muestreo es menor o igual a cero.
    /// </exception>
    public static double CalcularMagnitud(ReadOnlySpan<double> muestras, double frecuenciaTono, int frecuenciaMuestreo)
    {
        if (frecuenciaTono <= 0)
        {
            throw new ArgumentException("La frecuencia del tono debe ser mayor que cero.", nameof(frecuenciaTono));
        }

        if (frecuenciaMuestreo <= 0)
        {
            throw new ArgumentException("La frecuencia de muestreo debe ser mayor que cero.", nameof(frecuenciaMuestreo));
        }

        if (muestras.Length == 0)
        {
            return 0.0;
        }

        int longitudBloque = muestras.Length;

        double k = 0.5 + (longitudBloque * frecuenciaTono / frecuenciaMuestreo);
        double omega = 2.0 * Math.PI * k / longitudBloque;
        double coeficiente = 2.0 * Math.Cos(omega);

        double s0 = 0.0;
        double s1 = 0.0;
        double s2 = 0.0;

        for (int i = 0; i < longitudBloque; i++)
        {
            s0 = coeficiente * s1 - s2 + muestras[i];
            s2 = s1;
            s1 = s0;
        }

        double magnitudCuadrada = s1 * s1 + s2 * s2 - coeficiente * s1 * s2;
        double magnitudNormalizada = magnitudCuadrada / ((double)longitudBloque * longitudBloque);

        return magnitudNormalizada;
    }
}
