namespace RadioAficionado.Dominio.Sdr;

/// <summary>
/// Configuración para un receptor SDR.
/// Define los parámetros de sintonización, muestreo y buffer.
/// </summary>
/// <param name="FrecuenciaCentralHz">Frecuencia central de sintonización en Hz.</param>
/// <param name="TasaDeMuestreoHz">Tasa de muestreo en Hz (ej: 2_048_000 para RTL-SDR).</param>
/// <param name="AnchoDeBandaHz">Ancho de banda del filtro analógico en Hz.</param>
/// <param name="GananciaDb">Ganancia del LNA en dB.</param>
/// <param name="DispositivoPreferido">Nombre o identificador del dispositivo preferido. Null para usar el primero disponible.</param>
/// <param name="TamanoBufferMuestras">Tamaño del buffer de lectura de muestras IQ. Por defecto 65536.</param>
public sealed record ConfiguracionSdr(
    double FrecuenciaCentralHz,
    double TasaDeMuestreoHz,
    double AnchoDeBandaHz,
    double GananciaDb,
    string? DispositivoPreferido = null,
    int TamanoBufferMuestras = 65536);
