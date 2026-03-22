using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Estado actual del radio.
/// </summary>
public sealed class EstadoRig
{
    /// <summary>Frecuencia actual del VFO activo.</summary>
    public Frecuencia Frecuencia { get; init; }

    /// <summary>Modo de operación actual.</summary>
    public ModoOperacion Modo { get; init; }

    /// <summary>Submodo actual (ej: USB/LSB para SSB).</summary>
    public SubModoOperacion? SubModo { get; init; }

    /// <summary>Nivel del S-meter (0-9+).</summary>
    public int NivelSenal { get; init; }

    /// <summary>Potencia de transmisión configurada en vatios.</summary>
    public double PotenciaVatios { get; init; }

    /// <summary>Si el PTT está activado (transmitiendo).</summary>
    public bool Transmitiendo { get; init; }

    /// <summary>Ancho de banda del filtro en Hz.</summary>
    public int AnchoDeBandaHz { get; init; }

    /// <summary>VFO activo (A o B).</summary>
    public char VfoActivo { get; init; } = 'A';
}

/// <summary>
/// Interfaz para el control de equipos de radio vía rigctld (Hamlib).
/// </summary>
public interface IControlRig : IAsyncDisposable
{
    /// <summary>Indica si está conectado al radio.</summary>
    bool EstaConectado { get; }

    /// <summary>Modelo del radio conectado.</summary>
    string? ModeloRadio { get; }

    /// <summary>
    /// Conecta al demonio rigctld.
    /// </summary>
    Task ConectarAsync(string host = "localhost", int puerto = 4532, CancellationToken ct = default);

    /// <summary>
    /// Desconecta del demonio rigctld.
    /// </summary>
    Task DesconectarAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene el estado completo del radio.
    /// </summary>
    Task<EstadoRig> ObtenerEstadoAsync(CancellationToken ct = default);

    /// <summary>
    /// Cambia la frecuencia del VFO activo.
    /// </summary>
    Task CambiarFrecuenciaAsync(Frecuencia frecuencia, CancellationToken ct = default);

    /// <summary>
    /// Cambia el modo de operación.
    /// </summary>
    Task CambiarModoAsync(ModoOperacion modo, SubModoOperacion? subModo = null, CancellationToken ct = default);

    /// <summary>
    /// Cambia la potencia de transmisión.
    /// </summary>
    Task CambiarPotenciaAsync(double vatios, CancellationToken ct = default);

    /// <summary>
    /// Activa o desactiva el PTT (Push To Talk).
    /// </summary>
    Task CambiarPttAsync(bool activar, CancellationToken ct = default);

    /// <summary>
    /// Cambia el VFO activo.
    /// </summary>
    Task CambiarVfoAsync(char vfo, CancellationToken ct = default);

    /// <summary>
    /// Evento que se dispara cuando cambia el estado del radio.
    /// </summary>
    event EventHandler<EstadoRig>? EstadoCambiado;
}
