using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Aprs;

/// <summary>
/// Representa un objeto APRS posicionado en el mapa.
/// Los objetos tienen un nombre de hasta 9 caracteres y pueden estar vivos o eliminados.
/// </summary>
/// <param name="Nombre">Nombre del objeto (hasta 9 caracteres, rellenado con espacios).</param>
/// <param name="Coordenadas">Coordenadas geográficas del objeto.</param>
/// <param name="Vivo">Indica si el objeto está activo (true) o ha sido eliminado (false).</param>
/// <param name="Comentario">Comentario descriptivo del objeto.</param>
public record ObjetoAprs(
    string Nombre,
    Coordenadas Coordenadas,
    bool Vivo,
    string? Comentario);
