using RadioAficionado.Nativo.Dsp.Interfaces;

namespace RadioAficionado.Nativo.Dsp;

/// <summary>
/// Procesa audio en tiempo real para generar datos de waterfall.
/// Convierte muestras PCM de 16 bits a espectros de frecuencia usando FFT.
/// Configurable para diferentes tasas de muestreo y tamaños de FFT.
/// </summary>
public sealed class ProcesadorEspectro : IDisposable
{
    private readonly ITransformadaFourier _fft;
    private readonly int _tasaDeMuestreoHz;
    private readonly double _resolucionHz;
    private readonly double _frecuenciaMaxHz;
    private bool _descartado;

    /// <summary>
    /// Crea un procesador de espectro.
    /// </summary>
    /// <param name="tasaDeMuestreoHz">Tasa de muestreo del audio en Hz (ej: 12000, 44100, 48000).</param>
    /// <param name="tamanoFft">Tamaño de la FFT. Debe ser potencia de 2. Por defecto 2048 (para 12 kHz da ~5.86 Hz de resolución).</param>
    /// <exception cref="ArgumentOutOfRangeException">Si la tasa de muestreo es menor o igual a 0.</exception>
    public ProcesadorEspectro(int tasaDeMuestreoHz, int tamanoFft = 2048)
    {
        if (tasaDeMuestreoHz <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tasaDeMuestreoHz),
                tasaDeMuestreoHz,
                "La tasa de muestreo debe ser mayor que 0.");
        }

        _tasaDeMuestreoHz = tasaDeMuestreoHz;
        _fft = new TransformadaCooleyTukey(tamanoFft);
        _resolucionHz = (double)tasaDeMuestreoHz / tamanoFft;
        _frecuenciaMaxHz = (double)tasaDeMuestreoHz / 2.0;
    }

    /// <summary>
    /// Procesa una ventana de muestras PCM de 16 bits y devuelve una línea de espectro.
    /// La cantidad de muestras debe coincidir con el tamaño de la FFT.
    /// </summary>
    /// <param name="muestras">Muestras PCM de 16 bits (signed). La longitud debe ser igual al tamaño de FFT.</param>
    /// <returns>Línea de espectro con magnitudes en dB y metadatos de frecuencia.</returns>
    /// <exception cref="ArgumentException">Si la cantidad de muestras no coincide con el tamaño de FFT.</exception>
    public LineaEspectro Procesar(ReadOnlySpan<short> muestras)
    {
        ObjectDisposedException.ThrowIf(_descartado, this);

        if (muestras.Length != _fft.Tamano)
        {
            throw new ArgumentException(
                $"Se esperaban {_fft.Tamano} muestras pero se recibieron {muestras.Length}.",
                nameof(muestras));
        }

        double[] muestrasNormalizadas = ConvertirPcm16ADouble(muestras);
        double[] magnitudesDb = _fft.CalcularMagnitudDb(muestrasNormalizadas);

        return new LineaEspectro
        {
            MarcaDeTiempo = DateTimeOffset.UtcNow,
            MagnitudesDb = magnitudesDb,
            ResolucionHz = _resolucionHz,
            FrecuenciaMinHz = 0.0,
            FrecuenciaMaxHz = _frecuenciaMaxHz
        };
    }

    /// <summary>
    /// Procesa un bloque largo de muestras PCM usando ventanas deslizantes con solapamiento.
    /// Cada ventana produce una línea de espectro independiente.
    /// </summary>
    /// <param name="muestras">Bloque de muestras PCM de 16 bits.</param>
    /// <param name="solapamiento">
    /// Porcentaje de solapamiento entre ventanas consecutivas (0-99).
    /// Típicamente 50 para un buen compromiso entre resolución temporal y cómputo.
    /// </param>
    /// <returns>Lista de líneas de espectro generadas a partir del bloque.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si el solapamiento no está en el rango 0-99.</exception>
    public IReadOnlyList<LineaEspectro> ProcesarBloque(ReadOnlySpan<short> muestras, int solapamiento = 50)
    {
        ObjectDisposedException.ThrowIf(_descartado, this);

        if (solapamiento < 0 || solapamiento > 99)
        {
            throw new ArgumentOutOfRangeException(
                nameof(solapamiento),
                solapamiento,
                "El solapamiento debe estar entre 0 y 99 (porcentaje).");
        }

        int tamanoVentana = _fft.Tamano;

        if (muestras.Length < tamanoVentana)
        {
            return Array.Empty<LineaEspectro>();
        }

        int avancePorVentana = tamanoVentana - (tamanoVentana * solapamiento / 100);

        // Asegurar que el avance sea al menos 1 para evitar bucle infinito
        if (avancePorVentana < 1)
        {
            avancePorVentana = 1;
        }

        int cantidadDeVentanas = (muestras.Length - tamanoVentana) / avancePorVentana + 1;
        List<LineaEspectro> lineas = new(cantidadDeVentanas);

        // Calcular el intervalo de tiempo entre ventanas
        double segundosPorAvance = (double)avancePorVentana / _tasaDeMuestreoHz;
        DateTimeOffset marcaInicial = DateTimeOffset.UtcNow;

        for (int ventana = 0; ventana < cantidadDeVentanas; ventana++)
        {
            int inicio = ventana * avancePorVentana;
            ReadOnlySpan<short> fragmento = muestras.Slice(inicio, tamanoVentana);

            double[] muestrasNormalizadas = ConvertirPcm16ADouble(fragmento);
            double[] magnitudesDb = _fft.CalcularMagnitudDb(muestrasNormalizadas);

            LineaEspectro linea = new()
            {
                MarcaDeTiempo = marcaInicial.AddSeconds(ventana * segundosPorAvance),
                MagnitudesDb = magnitudesDb,
                ResolucionHz = _resolucionHz,
                FrecuenciaMinHz = 0.0,
                FrecuenciaMaxHz = _frecuenciaMaxHz
            };

            lineas.Add(linea);
        }

        return lineas;
    }

    /// <summary>
    /// Convierte muestras PCM de 16 bits (signed) a doubles normalizados en el rango [-1.0, 1.0].
    /// </summary>
    /// <param name="muestras">Muestras PCM de 16 bits.</param>
    /// <returns>Array de doubles normalizados.</returns>
    private static double[] ConvertirPcm16ADouble(ReadOnlySpan<short> muestras)
    {
        double[] resultado = new double[muestras.Length];
        const double factorNormalizacion = 1.0 / 32768.0;

        for (int i = 0; i < muestras.Length; i++)
        {
            resultado[i] = muestras[i] * factorNormalizacion;
        }

        return resultado;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_descartado)
        {
            _descartado = true;
            _fft.Dispose();
        }
    }
}
