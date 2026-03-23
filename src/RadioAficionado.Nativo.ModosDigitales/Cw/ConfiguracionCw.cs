namespace RadioAficionado.Nativo.ModosDigitales.Cw;

/// <summary>
/// Configuracion para el decodificador de CW (codigo Morse).
/// Contiene parametros ajustables para la deteccion de tono y decodificacion.
/// </summary>
public sealed class ConfiguracionCw
{
    /// <summary>
    /// Frecuencia del tono CW a detectar en Hz. Tipicamente entre 600 y 800 Hz.
    /// </summary>
    public double FrecuenciaTono { get; set; } = 700.0;

    /// <summary>
    /// Umbral de deteccion para considerar que el tono esta presente.
    /// Valores entre 0 y 1. Se usa como factor relativo sobre la media movil de magnitud.
    /// </summary>
    public double UmbralDeteccion { get; set; } = 0.3;

    /// <summary>
    /// Frecuencia de muestreo del audio de entrada en Hz.
    /// </summary>
    public int FrecuenciaMuestreo { get; set; } = 12000;

    /// <summary>
    /// Tamano del bloque de analisis en milisegundos.
    /// Bloques mas pequenos permiten detectar dits rapidos pero con menor precision de frecuencia.
    /// </summary>
    public int TamanoBloqueMilisegundos { get; set; } = 10;

    /// <summary>
    /// Velocidad inicial estimada en WPM (palabras por minuto).
    /// Se ajusta automaticamente durante la decodificacion.
    /// </summary>
    public int VelocidadInicialWpm { get; set; } = 20;

    /// <summary>
    /// Factor de suavizado para el umbral adaptativo (0.0 a 1.0).
    /// Valores mas altos hacen que el umbral se adapte mas lentamente.
    /// </summary>
    public double FactorSuavizadoUmbral { get; set; } = 0.95;
}
