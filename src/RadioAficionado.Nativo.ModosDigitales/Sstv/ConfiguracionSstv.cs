namespace RadioAficionado.Nativo.ModosDigitales.Sstv;

/// <summary>
/// Configuracion para el decodificador SSTV (Slow-Scan Television).
/// SSTV codifica imagenes como tonos de audio, donde la frecuencia representa
/// la luminancia de cada pixel (1500 Hz = negro, 2300 Hz = blanco).
/// </summary>
public sealed class ConfiguracionSstv
{
    /// <summary>
    /// Tasa de muestreo del audio en Hz. SSTV usa tipicamente 11025 Hz.
    /// </summary>
    public int TasaDeMuestreo { get; set; } = 11_025;

    /// <summary>
    /// Modo SSTV a decodificar.
    /// </summary>
    public ModoSstv ModoSstv { get; set; } = ModoSstv.Scottie1;

    /// <summary>
    /// Frecuencia del pulso de sincronizacion en Hz (1200 Hz estandar).
    /// </summary>
    public double FrecuenciaSincronizacionHz { get; set; } = 1200.0;

    /// <summary>
    /// Frecuencia que representa el color negro en Hz.
    /// </summary>
    public double FrecuenciaNegroHz { get; set; } = 1500.0;

    /// <summary>
    /// Frecuencia que representa el color blanco en Hz.
    /// </summary>
    public double FrecuenciaBlancoHz { get; set; } = 2300.0;

    /// <summary>
    /// Duracion minima del pulso de sincronizacion en milisegundos para deteccion VIS.
    /// </summary>
    public double DuracionPulsoSincronizacionMs { get; set; } = 5.0;

    /// <summary>
    /// Umbral de magnitud para deteccion de frecuencia de sincronizacion.
    /// </summary>
    public double UmbralSincronizacion { get; set; } = 0.02;

    /// <summary>
    /// Ancho de la imagen en pixeles segun el modo.
    /// </summary>
    public int AnchoImagen => ModoSstv switch
    {
        ModoSstv.Robot36 => 320,
        _ => 320
    };

    /// <summary>
    /// Alto de la imagen en pixeles segun el modo.
    /// </summary>
    public int AltoImagen => ModoSstv switch
    {
        ModoSstv.Robot36 => 240,
        _ => 256
    };
}
