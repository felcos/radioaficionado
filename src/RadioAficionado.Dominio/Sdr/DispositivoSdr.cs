namespace RadioAficionado.Dominio.Sdr;

/// <summary>
/// Representa un dispositivo SDR detectado en el sistema.
/// Contiene la información necesaria para identificar y conectar al dispositivo.
/// </summary>
/// <param name="Nombre">Nombre legible del dispositivo (ej: "RTL-SDR", "HackRF One").</param>
/// <param name="Controlador">Nombre del controlador SoapySDR (ej: "rtlsdr", "hackrf").</param>
/// <param name="NumeroSerie">Número de serie del dispositivo, si está disponible.</param>
/// <param name="Argumentos">Argumentos adicionales de conexión clave-valor para SoapySDR.</param>
public sealed record DispositivoSdr(
    string Nombre,
    string Controlador,
    string? NumeroSerie,
    Dictionary<string, string> Argumentos);
