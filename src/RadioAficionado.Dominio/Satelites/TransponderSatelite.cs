using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Satelites;

/// <summary>
/// Transponder de un satélite amateur con sus frecuencias de enlace y modo de operación.
/// </summary>
public sealed class TransponderSatelite
{
    /// <summary>
    /// Nombre identificativo del transponder (ej. "V/U lineal", "FM Voz").
    /// </summary>
    public string Nombre { get; }

    /// <summary>
    /// Frecuencia de transmisión desde tierra al satélite (uplink).
    /// </summary>
    public Frecuencia EnlaceSubida { get; }

    /// <summary>
    /// Frecuencia de transmisión desde el satélite a tierra (downlink).
    /// </summary>
    public Frecuencia EnlaceBajada { get; }

    /// <summary>
    /// Modo de operación del transponder.
    /// </summary>
    public ModoOperacion Modo { get; }

    /// <summary>
    /// Indica si el transponder invierte la banda lateral (LSB↔USB).
    /// </summary>
    public bool Invertido { get; }

    /// <summary>
    /// Crea una nueva instancia de <see cref="TransponderSatelite"/> validando los datos.
    /// </summary>
    /// <param name="nombre">Nombre identificativo del transponder.</param>
    /// <param name="enlaceSubida">Frecuencia de enlace de subida.</param>
    /// <param name="enlaceBajada">Frecuencia de enlace de bajada.</param>
    /// <param name="modo">Modo de operación.</param>
    /// <param name="invertido">Indica si invierte la banda lateral.</param>
    /// <exception cref="ArgumentException">Si el nombre está vacío.</exception>
    public TransponderSatelite(
        string nombre,
        Frecuencia enlaceSubida,
        Frecuencia enlaceBajada,
        ModoOperacion modo,
        bool invertido)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "El nombre del transponder no puede estar vacío.",
                nameof(nombre));
        }

        Nombre = nombre;
        EnlaceSubida = enlaceSubida;
        EnlaceBajada = enlaceBajada;
        Modo = modo;
        Invertido = invertido;
    }

    /// <summary>
    /// Devuelve una representación textual del transponder.
    /// </summary>
    public override string ToString()
    {
        string invertidoTexto = Invertido ? " (invertido)" : string.Empty;
        return $"{Nombre}: ↑{EnlaceSubida} ↓{EnlaceBajada} [{Modo}]{invertidoTexto}";
    }
}
