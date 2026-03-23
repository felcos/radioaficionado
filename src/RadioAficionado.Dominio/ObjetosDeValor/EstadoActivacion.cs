namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Estado del ciclo de vida de una activación de radio.
/// </summary>
public enum EstadoActivacion
{
    /// <summary>
    /// La activación ha sido planificada pero aún no ha comenzado.
    /// </summary>
    Planificada,

    /// <summary>
    /// La activación está en curso actualmente.
    /// </summary>
    EnCurso,

    /// <summary>
    /// La activación ha sido completada exitosamente.
    /// </summary>
    Completada,

    /// <summary>
    /// La activación ha sido cancelada.
    /// </summary>
    Cancelada
}
