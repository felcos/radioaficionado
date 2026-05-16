namespace RadioAficionado.Nativo.ModosDigitales.Q65;

/// <summary>
/// Configuracion para el decodificador Q65.
/// Q65 usa 65-FSK (como JT65) pero con codificacion Q-ary Repeat Accumulate,
/// lo que permite decodificar senales muy debiles (hasta -28 dB SNR en modo E).
/// El espaciado de tonos depende del submodo (mas largo el periodo = mas estrecho el tono).
/// </summary>
public sealed class ConfiguracionQ65
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz. Q65 requiere 12000 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 12_000;

    /// <summary>
    /// Submodo de Q65 que determina la duracion del periodo de transmision.
    /// </summary>
    public SubModoQ65 SubModo { get; set; } = SubModoQ65.A;

    /// <summary>
    /// Numero de tonos usados en la modulacion 65-FSK.
    /// </summary>
    public int NumeroTonos { get; set; } = 65;

    /// <summary>
    /// Frecuencia base en Hz desde donde comienzan los tonos.
    /// </summary>
    public double FrecuenciaBaseHz { get; set; } = 1000.0;

    /// <summary>
    /// Umbral minimo de magnitud Goertzel para considerar un tono valido.
    /// </summary>
    public double UmbralMagnitudTono { get; set; } = 0.005;

    /// <summary>
    /// Obtiene la duracion del periodo de transmision en segundos segun el submodo actual.
    /// </summary>
    /// <returns>Duracion del periodo en segundos.</returns>
    public int ObtenerDuracionPeriodoSegundos()
    {
        return SubModo.ObtenerDuracionPeriodoSegundos();
    }

    /// <summary>
    /// Obtiene el espaciado entre tonos en Hz segun el submodo.
    /// A mayor periodo, menor espaciado (y mayor sensibilidad).
    /// </summary>
    /// <returns>Espaciado entre tonos en Hz.</returns>
    public double ObtenerEspaciadoTonosHz()
    {
        int duracionPeriodo = ObtenerDuracionPeriodoSegundos();

        // El espaciado de tonos es inversamente proporcional a la duracion del periodo.
        // Referencia: Q65A usa ~5.0 Hz, Q65E usa ~1.0 Hz.
        return duracionPeriodo switch
        {
            15 => 5.0,
            30 => 2.5,
            60 => 1.25,
            120 => 0.625,
            300 => 0.25,
            _ => 5.0
        };
    }
}
