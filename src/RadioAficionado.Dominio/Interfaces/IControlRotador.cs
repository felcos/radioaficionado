namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Posición del rotador de antena.
/// </summary>
public sealed class PosicionRotador
{
    /// <summary>
    /// Azimut en grados (0-360). 0 = Norte, 90 = Este, 180 = Sur, 270 = Oeste.
    /// </summary>
    public double Azimut { get; }

    /// <summary>
    /// Elevación en grados (0-90). 0 = horizonte, 90 = cénit.
    /// Solo aplicable para rotadores AZ/EL (satélites, EME).
    /// </summary>
    public double Elevacion { get; }

    /// <summary>
    /// Crea una posición de rotador.
    /// </summary>
    public PosicionRotador(double azimut, double elevacion = 0.0)
    {
        if (azimut < 0.0 || azimut > 360.0)
        {
            throw new ArgumentOutOfRangeException(nameof(azimut), azimut, "El azimut debe estar entre 0 y 360 grados.");
        }

        if (elevacion < 0.0 || elevacion > 90.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevacion), elevacion, "La elevación debe estar entre 0 y 90 grados.");
        }

        Azimut = azimut;
        Elevacion = elevacion;
    }
}

/// <summary>
/// Interfaz para el control de rotadores de antena vía rotctld (Hamlib).
/// </summary>
public interface IControlRotador : IAsyncDisposable
{
    /// <summary>
    /// Indica si está conectado al demonio rotctld.
    /// </summary>
    bool EstaConectado { get; }

    /// <summary>
    /// Indica si el rotador soporta elevación (AZ/EL) además de azimut.
    /// </summary>
    bool SoportaElevacion { get; }

    /// <summary>
    /// Conecta al demonio rotctld.
    /// </summary>
    /// <param name="host">Host del servidor rotctld (default: localhost).</param>
    /// <param name="puerto">Puerto TCP del servidor rotctld (default: 4533).</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ConectarAsync(string host = "localhost", int puerto = 4533, CancellationToken ct = default);

    /// <summary>
    /// Desconecta del demonio rotctld.
    /// </summary>
    Task DesconectarAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene la posición actual del rotador.
    /// </summary>
    Task<PosicionRotador> ObtenerPosicionAsync(CancellationToken ct = default);

    /// <summary>
    /// Mueve el rotador a la posición especificada.
    /// </summary>
    Task MoverAsync(PosicionRotador posicion, CancellationToken ct = default);

    /// <summary>
    /// Detiene el movimiento del rotador inmediatamente.
    /// </summary>
    Task DetenerAsync(CancellationToken ct = default);

    /// <summary>
    /// Mueve el rotador al norte (posición de estacionamiento).
    /// </summary>
    Task EstacionarAsync(CancellationToken ct = default);

    /// <summary>
    /// Evento que se dispara cuando cambia la posición del rotador.
    /// </summary>
    event EventHandler<PosicionRotador>? PosicionCambiada;
}
