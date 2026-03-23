namespace RadioAficionado.Dominio.Configuracion;

/// <summary>
/// Configuración de los dispositivos de audio para la operación digital.
/// </summary>
public sealed class ConfiguracionAudio
{
    /// <summary>Nombre del dispositivo de entrada de audio (micrófono / tarjeta de sonido).</summary>
    public string DispositivoEntrada { get; set; } = string.Empty;

    /// <summary>Nombre del dispositivo de salida de audio (altavoz / tarjeta de sonido).</summary>
    public string DispositivoSalida { get; set; } = string.Empty;

    /// <summary>Frecuencia de muestreo en Hz (por defecto 48000).</summary>
    public int FrecuenciaMuestreo { get; set; } = 48_000;
}
