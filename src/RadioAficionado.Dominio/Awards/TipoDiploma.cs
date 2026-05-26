namespace RadioAficionado.Dominio.Awards;

/// <summary>
/// Tipos de diplomas/awards de radioaficion soportados.
/// </summary>
public enum TipoDiploma
{
    /// <summary>DXCC — Worked All DXCC Entities (gestionado por EstadisticasDxcc existente).</summary>
    Dxcc,

    /// <summary>WAZ — Worked All Zones (40 zonas CQ).</summary>
    Waz,

    /// <summary>WAS — Worked All States (50 estados de EEUU).</summary>
    Was,

    /// <summary>VUCC — VHF/UHF Century Club (100 grid squares en VHF+).</summary>
    Vucc,

    /// <summary>WAC — Worked All Continents (6 continentes).</summary>
    Wac,

    /// <summary>IOTA — Islands On The Air (islas activadas).</summary>
    Iota
}
