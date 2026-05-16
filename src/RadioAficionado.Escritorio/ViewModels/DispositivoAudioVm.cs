namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel ligero para representar un dispositivo de audio en la UI.
/// </summary>
public sealed class DispositivoAudioVm
{
    /// <summary>ID del dispositivo (ej: "in:0", "out:1").</summary>
    public string Id { get; }

    /// <summary>Nombre legible del dispositivo.</summary>
    public string Nombre { get; }

    /// <summary>Indica si es un dispositivo de entrada.</summary>
    public bool EsEntrada { get; }

    /// <summary>Texto para mostrar en la UI (nombre + tipo).</summary>
    public string TextoDisplay => EsEntrada ? $"🎤 {Nombre}" : $"🔊 {Nombre}";

    /// <summary>
    /// Crea un nuevo ViewModel de dispositivo de audio.
    /// </summary>
    public DispositivoAudioVm(string id, string nombre, bool esEntrada)
    {
        Id = id;
        Nombre = nombre;
        EsEntrada = esEntrada;
    }

    /// <inheritdoc />
    public override string ToString() => TextoDisplay;
}
