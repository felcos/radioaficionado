using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Nativo.Sdr;

/// <summary>
/// Convierte muestras IQ (In-phase / Quadrature) a audio mono
/// calculando la magnitud de cada par: sqrt(I^2 + Q^2).
/// Aplica ganancia digital configurable y normaliza la salida al rango [-1.0, 1.0].
/// </summary>
public sealed class ConvertidorIqAAudio : IConvertidorIqAAudio
{
    /// <inheritdoc />
    public double GananciaDigital { get; set; } = 1.0;

    /// <inheritdoc />
    public double[] Convertir(double[] muestrasI, double[] muestrasQ)
    {
        ArgumentNullException.ThrowIfNull(muestrasI, nameof(muestrasI));
        ArgumentNullException.ThrowIfNull(muestrasQ, nameof(muestrasQ));

        if (muestrasI.Length != muestrasQ.Length)
        {
            throw new ArgumentException(
                $"Las muestras I ({muestrasI.Length}) y Q ({muestrasQ.Length}) deben tener el mismo tamaño.",
                nameof(muestrasQ));
        }

        if (muestrasI.Length == 0)
        {
            return Array.Empty<double>();
        }

        double[] resultado = new double[muestrasI.Length];

        // Calcular magnitud de cada par IQ
        double magnitudMaxima = 0.0;

        for (int i = 0; i < muestrasI.Length; i++)
        {
            double valorI = muestrasI[i];
            double valorQ = muestrasQ[i];
            double magnitud = Math.Sqrt(valorI * valorI + valorQ * valorQ);
            resultado[i] = magnitud;

            if (magnitud > magnitudMaxima)
            {
                magnitudMaxima = magnitud;
            }
        }

        // Aplicar ganancia digital
        if (GananciaDigital != 1.0)
        {
            for (int i = 0; i < resultado.Length; i++)
            {
                resultado[i] *= GananciaDigital;
            }

            // Recalcular magnitud máxima después de aplicar ganancia
            magnitudMaxima *= Math.Abs(GananciaDigital);
        }

        // Normalizar al rango [-1.0, 1.0] (como es magnitud, siempre >= 0, queda en [0.0, 1.0])
        if (magnitudMaxima > 0.0)
        {
            double factorNormalizacion = 1.0 / magnitudMaxima;

            for (int i = 0; i < resultado.Length; i++)
            {
                resultado[i] *= factorNormalizacion;
            }
        }

        return resultado;
    }
}
