namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Convierte muestras IQ (In-phase / Quadrature) a audio mono
/// calculando la magnitud de cada par: sqrt(I^2 + Q^2).
/// Permite aplicar una ganancia digital configurable y normaliza
/// la salida al rango [-1.0, 1.0].
/// </summary>
public interface IConvertidorIqAAudio
{
    /// <summary>
    /// Ganancia digital aplicada a las muestras convertidas.
    /// Valor por defecto: 1.0 (sin amplificación ni atenuación).
    /// </summary>
    double GananciaDigital { get; set; }

    /// <summary>
    /// Convierte un par de arrays IQ a un array de audio mono.
    /// Calcula la magnitud sqrt(I^2 + Q^2) para cada par de muestras,
    /// aplica la ganancia digital y normaliza al rango [-1.0, 1.0].
    /// </summary>
    /// <param name="muestrasI">Muestras de la componente en fase (In-phase).</param>
    /// <param name="muestrasQ">Muestras de la componente en cuadratura (Quadrature).</param>
    /// <returns>Array de audio mono normalizado en el rango [-1.0, 1.0].</returns>
    /// <exception cref="ArgumentNullException">Si alguno de los arrays es nulo.</exception>
    /// <exception cref="ArgumentException">Si los arrays tienen tamaños diferentes.</exception>
    double[] Convertir(double[] muestrasI, double[] muestrasQ);
}
