namespace RadioAficionado.Dominio.Contests;

/// <summary>
/// Define cómo se calculan los multiplicadores en un contest.
/// Los multiplicadores se aplican a los puntos brutos para obtener la puntuación final.
/// </summary>
public enum MetodoMultiplicador
{
    /// <summary>Un multiplicador por cada entidad DXCC única contactada.</summary>
    PorDxcc,

    /// <summary>Un multiplicador por cada zona CQ única contactada.</summary>
    PorZonaCq,

    /// <summary>Un multiplicador por cada estado/provincia único contactado.</summary>
    PorEstado,

    /// <summary>Un multiplicador por cada prefijo único contactado.</summary>
    PorPrefijo,

    /// <summary>Un multiplicador por cada zona ITU única contactada.</summary>
    PorZonaItu,

    /// <summary>Un multiplicador por cada sección ARRL única contactada.</summary>
    PorSeccion
}
