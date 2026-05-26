using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Awards;

/// <summary>
/// Resumen de progreso de un diploma.
/// </summary>
/// <param name="Tipo">Tipo de diploma.</param>
/// <param name="Trabajadas">Cantidad de elementos unicos trabajados.</param>
/// <param name="Total">Total de elementos necesarios para completar el diploma.</param>
/// <param name="Porcentaje">Porcentaje de progreso (0-100).</param>
/// <param name="ElementosTrabajados">Lista de identificadores de los elementos trabajados.</param>
/// <param name="ElementosFaltantes">Lista de identificadores de los elementos faltantes.</param>
public sealed record ResumenDiploma(
    TipoDiploma Tipo,
    int Trabajadas,
    int Total,
    double Porcentaje,
    IReadOnlyList<string> ElementosTrabajados,
    IReadOnlyList<string> ElementosFaltantes);

/// <summary>
/// Calcula estadisticas de progreso para los diplomas WAZ, WAS y VUCC
/// a partir de listas de QSOs.
/// </summary>
public class EstadisticasAwards
{
    /// <summary>
    /// Total de continentes para WAC.
    /// </summary>
    public const int TotalContinentes = 6;

    /// <summary>
    /// Codigos de los 6 continentes para WAC.
    /// </summary>
    public static readonly IReadOnlyList<string> ContinentesWac = ["AF", "AS", "EU", "NA", "OC", "SA"];

    /// <summary>
    /// Total de zonas CQ para WAZ.
    /// </summary>
    public const int TotalZonasCq = 40;

    /// <summary>
    /// Total de grid squares necesarios para VUCC (minimo 100).
    /// </summary>
    public const int MinimoGridsVucc = 100;

    /// <summary>
    /// Calcula el progreso del diploma WAZ (Worked All Zones — 40 zonas CQ).
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Resumen del progreso WAZ.</returns>
    public ResumenDiploma CalcularWaz(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        HashSet<int> zonasTrabajadas = [];

        foreach (Qso qso in qsos)
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);

            if (entidad is not null && !entidad.Eliminada && entidad.ZonaCq >= 1 && entidad.ZonaCq <= TotalZonasCq)
            {
                zonasTrabajadas.Add(entidad.ZonaCq);
            }
        }

        List<string> trabajados = zonasTrabajadas.OrderBy(z => z).Select(z => z.ToString()).ToList();
        List<string> faltantes = [];

        for (int zona = 1; zona <= TotalZonasCq; zona++)
        {
            if (!zonasTrabajadas.Contains(zona))
            {
                faltantes.Add(zona.ToString());
            }
        }

        double porcentaje = (double)zonasTrabajadas.Count / TotalZonasCq * 100.0;

        return new ResumenDiploma(
            TipoDiploma.Waz,
            zonasTrabajadas.Count,
            TotalZonasCq,
            Math.Round(porcentaje, 1),
            trabajados,
            faltantes);
    }

    /// <summary>
    /// Calcula el progreso del diploma WAS (Worked All States — 50 estados EEUU).
    /// Requiere que los QSOs con estaciones de EEUU tengan el campo Estado informado.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Resumen del progreso WAS.</returns>
    public ResumenDiploma CalcularWas(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        HashSet<string> estadosTrabajados = [];

        foreach (Qso qso in qsos)
        {
            // Solo QSOs con estaciones de EEUU (prefijo W, K, N, AA-AL)
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);

            if (entidad is null || entidad.Numero != 291) // 291 = EEUU
            {
                continue;
            }

            // Necesitamos el estado del QSO (campo Notas o futuro campo Estado)
            // Por ahora intentamos extraerlo del campo Notas si contiene un codigo de estado
            string? estado = ExtraerEstadoDeNotas(qso.Notas);

            if (estado is not null)
            {
                estadosTrabajados.Add(estado);
            }
        }

        List<string> trabajados = estadosTrabajados.OrderBy(e => e).ToList();
        List<string> faltantes = [];

        foreach (CatalogoEstadosUsa.EstadoUsa estadoUsa in CatalogoEstadosUsa.Estados)
        {
            if (!estadosTrabajados.Contains(estadoUsa.Abreviatura))
            {
                faltantes.Add($"{estadoUsa.Abreviatura} ({estadoUsa.Nombre})");
            }
        }

        double porcentaje = (double)estadosTrabajados.Count / CatalogoEstadosUsa.TotalEstados * 100.0;

        return new ResumenDiploma(
            TipoDiploma.Was,
            estadosTrabajados.Count,
            CatalogoEstadosUsa.TotalEstados,
            Math.Round(porcentaje, 1),
            trabajados,
            faltantes);
    }

    /// <summary>
    /// Calcula el progreso del diploma VUCC (VHF/UHF Century Club — 100 grid squares).
    /// Solo cuenta QSOs en bandas VHF y superiores (6m+).
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Resumen del progreso VUCC.</returns>
    public ResumenDiploma CalcularVucc(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        HashSet<string> gridsTrabajados = [];

        foreach (Qso qso in qsos)
        {
            if (qso.LocalizadorContacto is null)
            {
                continue;
            }

            // VUCC solo cuenta en VHF+ (6m y superiores)
            BandaRadio? banda = qso.Frecuencia.ObtenerBanda();

            if (banda is null || !EsBandaVhfOSuperior(banda.Value))
            {
                continue;
            }

            // Usar los primeros 4 caracteres del grid (ej: "FN31" de "FN31pr")
            string valorGrid = qso.LocalizadorContacto.Value.Valor;
            string grid4 = valorGrid.Length >= 4
                ? valorGrid[..4].ToUpperInvariant()
                : valorGrid.ToUpperInvariant();

            gridsTrabajados.Add(grid4);
        }

        List<string> trabajados = gridsTrabajados.OrderBy(g => g).ToList();
        // VUCC no tiene lista finita de faltantes (hay 32400 grids posibles)
        List<string> faltantes = [];

        double porcentaje = Math.Min((double)gridsTrabajados.Count / MinimoGridsVucc * 100.0, 100.0);

        return new ResumenDiploma(
            TipoDiploma.Vucc,
            gridsTrabajados.Count,
            MinimoGridsVucc,
            Math.Round(porcentaje, 1),
            trabajados,
            faltantes);
    }

    /// <summary>
    /// Calcula el progreso del diploma WAC (Worked All Continents — 6 continentes).
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Resumen del progreso WAC.</returns>
    public ResumenDiploma CalcularWac(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        HashSet<string> continentesTrabajados = [];

        foreach (Qso qso in qsos)
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);

            if (entidad is not null && !entidad.Eliminada &&
                !string.IsNullOrWhiteSpace(entidad.Continente))
            {
                continentesTrabajados.Add(entidad.Continente);
            }
        }

        List<string> trabajados = continentesTrabajados.OrderBy(c => c).ToList();
        List<string> faltantes = [];

        foreach (string continente in ContinentesWac)
        {
            if (!continentesTrabajados.Contains(continente))
            {
                faltantes.Add($"{continente} ({NombreContinente(continente)})");
            }
        }

        double porcentaje = (double)continentesTrabajados.Count / TotalContinentes * 100.0;

        return new ResumenDiploma(
            TipoDiploma.Wac,
            continentesTrabajados.Count,
            TotalContinentes,
            Math.Round(porcentaje, 1),
            trabajados,
            faltantes);
    }

    /// <summary>
    /// Calcula el resumen de todos los diplomas disponibles.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Lista de resumenes de todos los diplomas.</returns>
    public IReadOnlyList<ResumenDiploma> CalcularTodos(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        return
        [
            CalcularWac(qsos),
            CalcularWaz(qsos),
            CalcularWas(qsos),
            CalcularVucc(qsos)
        ];
    }

    /// <summary>
    /// Determina si una banda es VHF o superior (6m+).
    /// </summary>
    private static bool EsBandaVhfOSuperior(BandaRadio banda)
    {
        return banda >= BandaRadio.Banda6m;
    }

    /// <summary>
    /// Intenta extraer un codigo de estado de EEUU del campo Notas de un QSO.
    /// Busca codigos de 2 letras que coincidan con estados validos.
    /// </summary>
    private static string? ExtraerEstadoDeNotas(string? notas)
    {
        if (string.IsNullOrWhiteSpace(notas))
        {
            return null;
        }

        // Buscar patrones comunes: "State: CA", "ST:NY", "CA", etc.
        string upper = notas.Trim().ToUpperInvariant();

        // Intentar extraer despues de "STATE:" o "ST:"
        string[] partes = upper.Split([' ', ':', ',', ';', '/', '-'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string parte in partes)
        {
            if (parte.Length == 2)
            {
                CatalogoEstadosUsa.EstadoUsa? estado = CatalogoEstadosUsa.ObtenerPorAbreviatura(parte);

                if (estado is not null)
                {
                    return estado.Abreviatura;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Devuelve el nombre en espanol de un codigo de continente.
    /// </summary>
    private static string NombreContinente(string codigo)
    {
        return codigo switch
        {
            "AF" => "Africa",
            "AN" => "Antartida",
            "AS" => "Asia",
            "EU" => "Europa",
            "NA" => "America del Norte",
            "OC" => "Oceania",
            "SA" => "America del Sur",
            _ => codigo
        };
    }
}
