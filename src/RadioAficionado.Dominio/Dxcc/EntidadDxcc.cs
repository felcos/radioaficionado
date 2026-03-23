namespace RadioAficionado.Dominio.Dxcc;

/// <summary>
/// Representa una entidad DXCC (país o territorio) según la lista oficial de la ARRL.
/// </summary>
/// <param name="Numero">Número identificador DXCC único.</param>
/// <param name="Nombre">Nombre oficial de la entidad.</param>
/// <param name="Prefijo">Prefijo principal asignado a la entidad.</param>
/// <param name="Continente">Código de continente (AF, AN, AS, EU, NA, OC, SA).</param>
/// <param name="ZonaCq">Zona CQ (WAZ) de la entidad.</param>
/// <param name="ZonaItu">Zona ITU de la entidad.</param>
/// <param name="Latitud">Latitud geográfica en grados decimales.</param>
/// <param name="Longitud">Longitud geográfica en grados decimales.</param>
/// <param name="Eliminada">Indica si la entidad ha sido eliminada de la lista DXCC activa.</param>
public record EntidadDxcc(
    int Numero,
    string Nombre,
    string Prefijo,
    string Continente,
    int ZonaCq,
    int ZonaItu,
    double Latitud,
    double Longitud,
    bool Eliminada);
