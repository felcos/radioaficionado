using System.Runtime.InteropServices;
using RadioAficionado.Nativo.Dsp.Interfaces;

namespace RadioAficionado.Nativo.Dsp;

/// <summary>
/// Implementacion de FFT usando la libreria nativa FFTW3 via P/Invoke.
/// Ofrece rendimiento significativamente superior a la implementacion managed
/// gracias a optimizaciones SIMD, cache-oblivious algorithms y planes pre-computados.
/// Implementa <see cref="ITransformadaFourier"/> para ser intercambiable con <see cref="TransformadaCooleyTukey"/>.
/// </summary>
public sealed class TransformadaFftw3 : ITransformadaFourier
{
    private readonly int _tamano;
    private readonly int _numeroDeBins;
    private readonly IntPtr _bufferEntrada;
    private readonly IntPtr _bufferSalida;
    private readonly IntPtr _plan;
    private readonly double[] _coeficientesHann;
    private readonly object _bloqueo = new();
    private bool _descartado;

    /// <summary>
    /// Piso minimo en dB para evitar -infinito en el calculo logaritmico.
    /// </summary>
    private const double PisoMinimoDb = -120.0;

    /// <inheritdoc />
    public int Tamano => _tamano;

    /// <summary>
    /// Crea una nueva instancia de FFT usando FFTW3 nativa.
    /// Pre-aloca buffers con memoria alineada SIMD y crea un plan optimizado.
    /// </summary>
    /// <param name="tamano">Tamano de la FFT. Debe ser potencia de 2 y mayor o igual a 2.</param>
    /// <exception cref="ArgumentException">Si el tamano no es potencia de 2 o es menor que 2.</exception>
    /// <exception cref="InvalidOperationException">Si no se puede crear el plan FFTW3.</exception>
    public TransformadaFftw3(int tamano)
    {
        if (tamano < 2 || !EsPotenciaDeDos(tamano))
        {
            throw new ArgumentException(
                $"El tamano de la FFT debe ser una potencia de 2 mayor o igual a 2. Valor recibido: {tamano}.",
                nameof(tamano));
        }

        _tamano = tamano;
        _numeroDeBins = tamano / 2 + 1;

        // Asignar memoria alineada para buffers (SIMD-friendly)
        _bufferEntrada = Fftw3Nativo.AsignarMemoria(new IntPtr(tamano * sizeof(double)));
        _bufferSalida = Fftw3Nativo.AsignarMemoria(new IntPtr(_numeroDeBins * 2 * sizeof(double)));

        if (_bufferEntrada == IntPtr.Zero || _bufferSalida == IntPtr.Zero)
        {
            LiberarRecursos();
            throw new InvalidOperationException("No se pudo asignar memoria FFTW3.");
        }

        // Crear plan real→complejo con ESTIMATE (rapido de crear)
        _plan = Fftw3Nativo.PlanearRealAComplejo(tamano, _bufferEntrada, _bufferSalida, Fftw3Nativo.FFTW_ESTIMATE);

        if (_plan == IntPtr.Zero)
        {
            LiberarRecursos();
            throw new InvalidOperationException(
                $"No se pudo crear el plan FFTW3 para tamano {tamano}.");
        }

        // Pre-computar coeficientes de ventana Hann
        _coeficientesHann = new double[tamano];
        for (int i = 0; i < tamano; i++)
        {
            _coeficientesHann[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (tamano - 1)));
        }
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

        double[] resultado = new double[_numeroDeBins * 2];

        lock (_bloqueo)
        {
            CopiarEntradaConVentana(entrada);
            Fftw3Nativo.Ejecutar(_plan);
            Marshal.Copy(_bufferSalida, resultado, 0, resultado.Length);
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

        double[] salidaCompleja = new double[_numeroDeBins * 2];

        lock (_bloqueo)
        {
            CopiarEntradaConVentana(entrada);
            Fftw3Nativo.Ejecutar(_plan);
            Marshal.Copy(_bufferSalida, salidaCompleja, 0, salidaCompleja.Length);
        }

        // Calcular magnitudes en dB
        double[] magnitudesDb = new double[_numeroDeBins];
        double factorNormalizacion = _tamano;

        for (int i = 0; i < _numeroDeBins; i++)
        {
            double re = salidaCompleja[i * 2];
            double im = salidaCompleja[i * 2 + 1];
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
    /// Copia la entrada al buffer FFTW3 aplicando la ventana Hann.
    /// </summary>
    /// <param name="entrada">Muestras de entrada.</param>
    private void CopiarEntradaConVentana(ReadOnlySpan<double> entrada)
    {
        double[] temporal = new double[_tamano];

        for (int i = 0; i < _tamano; i++)
        {
            temporal[i] = entrada[i] * _coeficientesHann[i];
        }

        Marshal.Copy(temporal, 0, _bufferEntrada, _tamano);
    }

    /// <summary>
    /// Determina si un numero es potencia de 2.
    /// </summary>
    private static bool EsPotenciaDeDos(int valor)
    {
        return valor > 0 && (valor & (valor - 1)) == 0;
    }

    /// <summary>
    /// Libera los recursos nativos de FFTW3.
    /// </summary>
    private void LiberarRecursos()
    {
        if (_plan != IntPtr.Zero)
        {
            Fftw3Nativo.DestruirPlan(_plan);
        }

        if (_bufferEntrada != IntPtr.Zero)
        {
            Fftw3Nativo.LiberarMemoria(_bufferEntrada);
        }

        if (_bufferSalida != IntPtr.Zero)
        {
            Fftw3Nativo.LiberarMemoria(_bufferSalida);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_descartado)
        {
            _descartado = true;
            LiberarRecursos();
        }
    }
}
