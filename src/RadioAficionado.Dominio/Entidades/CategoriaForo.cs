namespace RadioAficionado.Dominio.Entidades;

/// <summary>
/// Categorías disponibles para clasificar los hilos del foro.
/// </summary>
public enum CategoriaForo
{
    /// <summary>
    /// Temas generales de radioafición.
    /// </summary>
    General = 0,

    /// <summary>
    /// Temas técnicos: equipos, antenas, propagación.
    /// </summary>
    Tecnico = 1,

    /// <summary>
    /// DX: contactos de larga distancia y expediciones.
    /// </summary>
    DX = 2,

    /// <summary>
    /// Contests y concursos de radioafición.
    /// </summary>
    Contests = 3,

    /// <summary>
    /// Activaciones SOTA, POTA, WWFF y similares.
    /// </summary>
    Activaciones = 4,

    /// <summary>
    /// Compraventa de equipos y accesorios.
    /// </summary>
    Compraventa = 5
}
