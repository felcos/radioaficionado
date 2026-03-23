namespace RadioAficionado.Web.ViewModels;

/// <summary>
/// ViewModel principal para el dashboard de estadisticas.
/// Contiene totales, records y promedios del logbook.
/// </summary>
public class EstadisticasViewModel
{
    /// <summary>
    /// Numero total de QSOs registrados.
    /// </summary>
    public int TotalQsos { get; set; }

    /// <summary>
    /// Numero total de entidades DXCC trabajadas (activas).
    /// </summary>
    public int TotalEntidadesDxcc { get; set; }

    /// <summary>
    /// Numero de bandas distintas utilizadas.
    /// </summary>
    public int TotalBandas { get; set; }

    /// <summary>
    /// Numero de modos distintos utilizados.
    /// </summary>
    public int TotalModos { get; set; }

    /// <summary>
    /// Numero de indicativos unicos contactados.
    /// </summary>
    public int TotalIndicativosUnicos { get; set; }

    /// <summary>
    /// Fecha del primer QSO registrado. Null si no hay QSOs.
    /// </summary>
    public DateTimeOffset? PrimerQso { get; set; }

    /// <summary>
    /// Fecha del ultimo QSO registrado. Null si no hay QSOs.
    /// </summary>
    public DateTimeOffset? UltimoQso { get; set; }

    /// <summary>
    /// Promedio de QSOs por dia (solo dias con actividad).
    /// </summary>
    public double PromedioQsosPorDia { get; set; }

    /// <summary>
    /// Dia con mas QSOs registrados. Null si no hay QSOs.
    /// </summary>
    public string? DiaRecord { get; set; }

    /// <summary>
    /// Cantidad de QSOs en el dia record.
    /// </summary>
    public int QsosEnDiaRecord { get; set; }

    /// <summary>
    /// Banda mas utilizada. Null si no hay QSOs.
    /// </summary>
    public string? BandaMasUsada { get; set; }

    /// <summary>
    /// Modo mas utilizado. Null si no hay QSOs.
    /// </summary>
    public string? ModoMasUsado { get; set; }
}

/// <summary>
/// ViewModel para datos de grafico de barras (QSOs por banda).
/// </summary>
public class DatosBandasViewModel
{
    /// <summary>
    /// Etiquetas de las bandas (ej: "20 metros", "40 metros").
    /// </summary>
    public IReadOnlyList<string> Etiquetas { get; set; } = [];

    /// <summary>
    /// Cantidad de QSOs por cada banda.
    /// </summary>
    public IReadOnlyList<int> Cantidades { get; set; } = [];
}

/// <summary>
/// ViewModel para datos de grafico de pastel/dona (QSOs por modo).
/// </summary>
public class DatosModosViewModel
{
    /// <summary>
    /// Etiquetas de los modos (ej: "FT8", "SSB", "CW").
    /// </summary>
    public IReadOnlyList<string> Etiquetas { get; set; } = [];

    /// <summary>
    /// Cantidad de QSOs por cada modo.
    /// </summary>
    public IReadOnlyList<int> Cantidades { get; set; } = [];
}

/// <summary>
/// ViewModel para datos de grafico de lineas (QSOs por mes).
/// </summary>
public class DatosTemporalesViewModel
{
    /// <summary>
    /// Etiquetas de los meses (ej: "Ene 2025", "Feb 2025").
    /// </summary>
    public IReadOnlyList<string> Etiquetas { get; set; } = [];

    /// <summary>
    /// Cantidad de QSOs por cada mes.
    /// </summary>
    public IReadOnlyList<int> Cantidades { get; set; } = [];
}

/// <summary>
/// ViewModel para datos de grafico de dona (QSOs por continente).
/// </summary>
public class DatosContinentesViewModel
{
    /// <summary>
    /// Etiquetas de los continentes (ej: "Europa", "America del Norte").
    /// </summary>
    public IReadOnlyList<string> Etiquetas { get; set; } = [];

    /// <summary>
    /// Cantidad de QSOs por cada continente.
    /// </summary>
    public IReadOnlyList<int> Cantidades { get; set; } = [];
}
