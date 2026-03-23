using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Dxcc;

/// <summary>
/// Resumen consolidado de estadísticas DXCC de un operador.
/// </summary>
/// <param name="TotalTrabajadas">Número total de entidades DXCC trabajadas.</param>
/// <param name="TotalConfirmadas">Número total de entidades DXCC confirmadas.</param>
/// <param name="PorBanda">Entidades DXCC trabajadas agrupadas por banda.</param>
/// <param name="PorModo">Entidades DXCC trabajadas agrupadas por modo.</param>
/// <param name="PorContinente">Entidades DXCC trabajadas agrupadas por continente.</param>
public record ResumenDxcc(
    int TotalTrabajadas,
    int TotalConfirmadas,
    IReadOnlyDictionary<BandaRadio, HashSet<int>> PorBanda,
    IReadOnlyDictionary<ModoOperacion, HashSet<int>> PorModo,
    IReadOnlyDictionary<string, HashSet<int>> PorContinente);

/// <summary>
/// Calcula estadísticas DXCC a partir de listas de QSOs y confirmaciones.
/// Permite determinar entidades trabajadas, confirmadas, faltantes y desglosadas por banda/modo/continente.
/// </summary>
public class EstadisticasDxcc
{
    /// <summary>
    /// Obtiene los números DXCC únicos trabajados a partir de una lista de QSOs.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Conjunto de números DXCC trabajados.</returns>
    public HashSet<int> EntidadesTrabajadas(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        HashSet<int> trabajadas = new();

        foreach (Qso qso in qsos)
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
            if (entidad is not null && !entidad.Eliminada)
            {
                trabajadas.Add(entidad.Numero);
            }
        }

        return trabajadas;
    }

    /// <summary>
    /// Obtiene los números DXCC únicos confirmados cruzando QSOs con sus confirmaciones.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <param name="confirmaciones">Lista de confirmaciones de QSOs.</param>
    /// <returns>Conjunto de números DXCC confirmados.</returns>
    public HashSet<int> EntidadesConfirmadas(IReadOnlyList<Qso> qsos, IReadOnlyList<ConfirmacionQso> confirmaciones)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        if (confirmaciones is null)
        {
            throw new ArgumentNullException(nameof(confirmaciones));
        }

        HashSet<Guid> idsConfirmados = new(confirmaciones.Select(c => c.QsoId));
        HashSet<int> confirmadas = new();

        foreach (Qso qso in qsos)
        {
            if (!idsConfirmados.Contains(qso.Id))
            {
                continue;
            }

            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
            if (entidad is not null && !entidad.Eliminada)
            {
                confirmadas.Add(entidad.Numero);
            }
        }

        return confirmadas;
    }

    /// <summary>
    /// Agrupa las entidades DXCC trabajadas por banda de radio.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Diccionario donde cada clave es una banda y el valor es el conjunto de números DXCC trabajados en esa banda.</returns>
    public Dictionary<BandaRadio, HashSet<int>> PorBanda(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        Dictionary<BandaRadio, HashSet<int>> resultado = new();

        foreach (Qso qso in qsos)
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
            if (entidad is null || entidad.Eliminada)
            {
                continue;
            }

            BandaRadio? banda = BandaRadioExtensiones.DesdeFrecuencia(qso.Frecuencia);
            if (banda is null)
            {
                continue;
            }

            if (!resultado.ContainsKey(banda.Value))
            {
                resultado[banda.Value] = new HashSet<int>();
            }

            resultado[banda.Value].Add(entidad.Numero);
        }

        return resultado;
    }

    /// <summary>
    /// Agrupa las entidades DXCC trabajadas por modo de operación.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Diccionario donde cada clave es un modo y el valor es el conjunto de números DXCC trabajados en ese modo.</returns>
    public Dictionary<ModoOperacion, HashSet<int>> PorModo(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        Dictionary<ModoOperacion, HashSet<int>> resultado = new();

        foreach (Qso qso in qsos)
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
            if (entidad is null || entidad.Eliminada)
            {
                continue;
            }

            if (!resultado.ContainsKey(qso.Modo))
            {
                resultado[qso.Modo] = new HashSet<int>();
            }

            resultado[qso.Modo].Add(entidad.Numero);
        }

        return resultado;
    }

    /// <summary>
    /// Obtiene la lista de entidades DXCC activas que no han sido trabajadas.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <returns>Lista de entidades DXCC que aún no se han contactado.</returns>
    public IReadOnlyList<EntidadDxcc> EntidadesFaltantes(IReadOnlyList<Qso> qsos)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        HashSet<int> trabajadas = EntidadesTrabajadas(qsos);
        IReadOnlyList<EntidadDxcc> activas = CatalogoDxcc.ObtenerActivas();

        List<EntidadDxcc> faltantes = new();
        foreach (EntidadDxcc entidad in activas)
        {
            if (!trabajadas.Contains(entidad.Numero))
            {
                faltantes.Add(entidad);
            }
        }

        return faltantes.AsReadOnly();
    }

    /// <summary>
    /// Genera un resumen completo de estadísticas DXCC.
    /// </summary>
    /// <param name="qsos">Lista de QSOs realizados.</param>
    /// <param name="confirmaciones">Lista de confirmaciones de QSOs.</param>
    /// <returns>Resumen con totales, desglose por banda, modo y continente.</returns>
    public ResumenDxcc GenerarResumen(IReadOnlyList<Qso> qsos, IReadOnlyList<ConfirmacionQso> confirmaciones)
    {
        if (qsos is null)
        {
            throw new ArgumentNullException(nameof(qsos));
        }

        if (confirmaciones is null)
        {
            throw new ArgumentNullException(nameof(confirmaciones));
        }

        HashSet<int> trabajadas = EntidadesTrabajadas(qsos);
        HashSet<int> confirmadas = EntidadesConfirmadas(qsos, confirmaciones);
        Dictionary<BandaRadio, HashSet<int>> porBanda = PorBanda(qsos);
        Dictionary<ModoOperacion, HashSet<int>> porModo = PorModo(qsos);
        Dictionary<string, HashSet<int>> porContinente = CalcularPorContinente(qsos);

        return new ResumenDxcc(
            TotalTrabajadas: trabajadas.Count,
            TotalConfirmadas: confirmadas.Count,
            PorBanda: porBanda,
            PorModo: porModo,
            PorContinente: porContinente);
    }

    private Dictionary<string, HashSet<int>> CalcularPorContinente(IReadOnlyList<Qso> qsos)
    {
        Dictionary<string, HashSet<int>> resultado = new();

        foreach (Qso qso in qsos)
        {
            EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(qso.IndicativoContacto);
            if (entidad is null || entidad.Eliminada)
            {
                continue;
            }

            if (!resultado.ContainsKey(entidad.Continente))
            {
                resultado[entidad.Continente] = new HashSet<int>();
            }

            resultado[entidad.Continente].Add(entidad.Numero);
        }

        return resultado;
    }
}
