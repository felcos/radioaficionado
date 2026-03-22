using RadioAficionado.Nativo.Dsp.Interfaces;

namespace RadioAficionado.Nativo.Dsp;

/// <summary>
/// Implementación managed de FFT usando el algoritmo Cooley-Tukey radix-2 DIT.
/// Pre-computa factores twiddle, tabla de bit-reversal y coeficientes de ventana Hann
/// en el constructor para maximizar rendimiento en llamadas repetidas.
/// Diseñada para ser reemplazable por FFTW3 nativa a través de <see cref="ITransformadaFourier"/>.
/// </summary>
public sealed class TransformadaCooleyTukey : ITransformadaFourier
{
    private readonly int _tamano;
    private readonly int _numeroDeBits;
    private readonly int[] _tablaInversionBits;
    private readonly double[] _factoresTwiddleReales;
    private readonly double[] _factoresTwiddleImaginarios;
    private readonly double[] _coeficientesHann;
    private bool _descartado;

    /// <inheritdoc />
    public int Tamano => _tamano;

    /// <summary>
    /// Piso mínimo en dB para evitar -infinito en el cálculo logarítmico.
    /// </summary>
    private const double PisoMinimoDb = -120.0;

    /// <summary>
    /// Crea una nueva instancia de la FFT Cooley-Tukey.
    /// </summary>
    /// <param name="tamano">Tamaño de la FFT. Debe ser potencia de 2 y mayor que 0.</param>
    /// <exception cref="ArgumentException">Si <paramref name="tamano"/> no es potencia de 2 o es menor que 2.</exception>
    public TransformadaCooleyTukey(int tamano)
    {
        if (tamano < 2 || !EsPotenciaDeDos(tamano))
        {
            throw new ArgumentException(
                $"El tamaño de la FFT debe ser una potencia de 2 mayor o igual a 2. Valor recibido: {tamano}.",
                nameof(tamano));
        }

        _tamano = tamano;
        _numeroDeBits = (int)Math.Log2(tamano);

        _tablaInversionBits = PrecomputarTablaInversionBits(tamano, _numeroDeBits);
        (_factoresTwiddleReales, _factoresTwiddleImaginarios) = PrecomputarFactoresTwiddle(tamano);
        _coeficientesHann = PrecomputarCoeficientesHann(tamano);
    }

    /// <inheritdoc />
    public double[] Calcular(ReadOnlySpan<double> entrada)
    {
        ObjectDisposedException.ThrowIf(_descartado, this);

        if (entrada.Length != _tamano)
        {
            throw new ArgumentException(
                $"La entrada debe tener exactamente {_tamano} muestras. Se recibieron {entrada.Length}.",
                nameof(entrada));
        }

        // Copiar entrada y aplicar ventana Hann
        double[] parteReal = new double[_tamano];
        double[] parteImaginaria = new double[_tamano];

        for (int i = 0; i < _tamano; i++)
        {
            parteReal[i] = entrada[i] * _coeficientesHann[i];
        }
        // parteImaginaria ya inicializada a ceros

        // Ejecutar FFT in-place
        EjecutarFftInPlace(parteReal, parteImaginaria);

        // Empaquetar resultado como pares [re, im] para los N/2+1 bins no redundantes
        int numeroDeBins = _tamano / 2 + 1;
        double[] resultado = new double[numeroDeBins * 2];

        for (int i = 0; i < numeroDeBins; i++)
        {
            resultado[i * 2] = parteReal[i];
            resultado[i * 2 + 1] = parteImaginaria[i];
        }

        return resultado;
    }

    /// <inheritdoc />
    public double[] CalcularMagnitudDb(ReadOnlySpan<double> entrada)
    {
        ObjectDisposedException.ThrowIf(_descartado, this);

        if (entrada.Length != _tamano)
        {
            throw new ArgumentException(
                $"La entrada debe tener exactamente {_tamano} muestras. Se recibieron {entrada.Length}.",
                nameof(entrada));
        }

        // Copiar entrada y aplicar ventana Hann
        double[] parteReal = new double[_tamano];
        double[] parteImaginaria = new double[_tamano];

        for (int i = 0; i < _tamano; i++)
        {
            parteReal[i] = entrada[i] * _coeficientesHann[i];
        }

        // Ejecutar FFT in-place
        EjecutarFftInPlace(parteReal, parteImaginaria);

        // Calcular magnitud en dB para la mitad positiva (N/2 + 1 bins)
        int numeroDeBins = _tamano / 2 + 1;
        double[] magnitudesDb = new double[numeroDeBins];
        double factorNormalizacion = _tamano;

        for (int i = 0; i < numeroDeBins; i++)
        {
            double re = parteReal[i];
            double im = parteImaginaria[i];
            double magnitud = Math.Sqrt(re * re + im * im);
            double magnitudNormalizada = magnitud / factorNormalizacion;

            if (magnitudNormalizada < 1e-12)
            {
                magnitudesDb[i] = PisoMinimoDb;
            }
            else
            {
                double db = 20.0 * Math.Log10(magnitudNormalizada);
                magnitudesDb[i] = Math.Max(db, PisoMinimoDb);
            }
        }

        return magnitudesDb;
    }

    /// <summary>
    /// Ejecuta la FFT radix-2 DIT (decimation-in-time) de Cooley-Tukey in-place.
    /// </summary>
    /// <param name="parteReal">Array de partes reales (se modifica in-place).</param>
    /// <param name="parteImaginaria">Array de partes imaginarias (se modifica in-place).</param>
    private void EjecutarFftInPlace(double[] parteReal, double[] parteImaginaria)
    {
        // Paso 1: Reordenar con bit-reversal
        for (int i = 0; i < _tamano; i++)
        {
            int j = _tablaInversionBits[i];
            if (i < j)
            {
                (parteReal[i], parteReal[j]) = (parteReal[j], parteReal[i]);
                (parteImaginaria[i], parteImaginaria[j]) = (parteImaginaria[j], parteImaginaria[i]);
            }
        }

        // Paso 2: Mariposas (butterfly) por etapas
        int indiceTwiddle = 0;
        for (int etapa = 1; etapa <= _numeroDeBits; etapa++)
        {
            int tamanoSubgrupo = 1 << etapa;       // 2, 4, 8, ...
            int mitadSubgrupo = tamanoSubgrupo >> 1; // 1, 2, 4, ...

            for (int grupo = 0; grupo < _tamano; grupo += tamanoSubgrupo)
            {
                for (int k = 0; k < mitadSubgrupo; k++)
                {
                    int indicePar = grupo + k;
                    int indiceImpar = grupo + k + mitadSubgrupo;

                    double twiddleRe = _factoresTwiddleReales[indiceTwiddle + k];
                    double twiddleIm = _factoresTwiddleImaginarios[indiceTwiddle + k];

                    // Multiplicación compleja: twiddle * X[impar]
                    double productoRe = twiddleRe * parteReal[indiceImpar] - twiddleIm * parteImaginaria[indiceImpar];
                    double productoIm = twiddleRe * parteImaginaria[indiceImpar] + twiddleIm * parteReal[indiceImpar];

                    // Butterfly
                    parteReal[indiceImpar] = parteReal[indicePar] - productoRe;
                    parteImaginaria[indiceImpar] = parteImaginaria[indicePar] - productoIm;
                    parteReal[indicePar] += productoRe;
                    parteImaginaria[indicePar] += productoIm;
                }
            }

            indiceTwiddle += mitadSubgrupo;
        }
    }

    /// <summary>
    /// Determina si un número es potencia de 2.
    /// </summary>
    private static bool EsPotenciaDeDos(int valor)
    {
        return valor > 0 && (valor & (valor - 1)) == 0;
    }

    /// <summary>
    /// Pre-computa la tabla de inversión de bits para el reordenamiento inicial.
    /// </summary>
    private static int[] PrecomputarTablaInversionBits(int tamano, int numeroDeBits)
    {
        int[] tabla = new int[tamano];

        for (int i = 0; i < tamano; i++)
        {
            int invertido = 0;
            int valor = i;

            for (int bit = 0; bit < numeroDeBits; bit++)
            {
                invertido = (invertido << 1) | (valor & 1);
                valor >>= 1;
            }

            tabla[i] = invertido;
        }

        return tabla;
    }

    /// <summary>
    /// Pre-computa los factores twiddle (W_N^k = e^{-2πik/N}) para todas las etapas.
    /// Se almacenan secuencialmente: primero los de la etapa 1 (1 factor), luego etapa 2 (2 factores), etc.
    /// </summary>
    private static (double[] reales, double[] imaginarios) PrecomputarFactoresTwiddle(int tamano)
    {
        int numeroDeBits = (int)Math.Log2(tamano);

        // Total de factores: 1 + 2 + 4 + ... + N/2 = N - 1
        int totalFactores = tamano - 1;
        double[] reales = new double[totalFactores];
        double[] imaginarios = new double[totalFactores];

        int indice = 0;
        for (int etapa = 1; etapa <= numeroDeBits; etapa++)
        {
            int mitadSubgrupo = 1 << (etapa - 1);
            double anguloBase = -2.0 * Math.PI / (1 << etapa);

            for (int k = 0; k < mitadSubgrupo; k++)
            {
                double angulo = anguloBase * k;
                reales[indice + k] = Math.Cos(angulo);
                imaginarios[indice + k] = Math.Sin(angulo);
            }

            indice += mitadSubgrupo;
        }

        return (reales, imaginarios);
    }

    /// <summary>
    /// Pre-computa los coeficientes de la ventana Hann para el tamaño dado.
    /// </summary>
    private static double[] PrecomputarCoeficientesHann(int tamano)
    {
        double[] coeficientes = new double[tamano];

        for (int i = 0; i < tamano; i++)
        {
            coeficientes[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (tamano - 1)));
        }

        return coeficientes;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _descartado = true;
    }
}
