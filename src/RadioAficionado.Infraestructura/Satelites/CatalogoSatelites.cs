using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Satelites;

namespace RadioAficionado.Infraestructura.Satelites;

/// <summary>
/// Catálogo estático de satélites amateur más populares con sus transponders.
/// Fuente de referencia: AMSAT, JE9PEL satellite frequency list.
/// </summary>
public static class CatalogoSatelites
{
    /// <summary>
    /// Obtiene la lista completa de satélites amateur conocidos.
    /// </summary>
    /// <returns>Lista inmutable de satélites amateur con sus transponders.</returns>
    public static IReadOnlyList<SateliteAmateur> ObtenerTodos()
    {
        return _satelites;
    }

    /// <summary>
    /// Busca un satélite por su número NORAD.
    /// </summary>
    /// <param name="noradId">Número de catálogo NORAD.</param>
    /// <returns>El satélite encontrado, o null si no existe en el catálogo.</returns>
    public static SateliteAmateur? BuscarPorNorad(int noradId)
    {
        for (int i = 0; i < _satelites.Count; i++)
        {
            if (_satelites[i].NumeroNorad == noradId)
            {
                return _satelites[i];
            }
        }

        return null;
    }

    private static readonly IReadOnlyList<SateliteAmateur> _satelites = new List<SateliteAmateur>
    {
        // ISS (ZARYA) — Estación Espacial Internacional con ARISS
        new SateliteAmateur(
            numeroNorad: 25544,
            nombre: "ISS (ZARYA)",
            indicativo: "RS0ISS",
            transponders: new List<TransponderSatelite>
            {
                new("ARISS VHF Voz", Frecuencia.DesdeMHz(145.200), Frecuencia.DesdeMHz(145.800), ModoOperacion.FM, false),
                new("ARISS APRS", Frecuencia.DesdeMHz(145.825), Frecuencia.DesdeMHz(145.825), ModoOperacion.PKT, false)
            },
            activo: true),

        // SO-50 (SaudiSat-1C)
        new SateliteAmateur(
            numeroNorad: 27607,
            nombre: "SO-50",
            indicativo: "NO-50",
            transponders: new List<TransponderSatelite>
            {
                new("FM Voz", Frecuencia.DesdeMHz(145.850), Frecuencia.DesdeMHz(436.795), ModoOperacion.FM, false)
            },
            activo: true),

        // AO-91 (RadFxSat / Fox-1B)
        new SateliteAmateur(
            numeroNorad: 43017,
            nombre: "AO-91",
            indicativo: "NO-91",
            transponders: new List<TransponderSatelite>
            {
                new("FM Voz U/V", Frecuencia.DesdeMHz(435.250), Frecuencia.DesdeMHz(145.960), ModoOperacion.FM, false)
            },
            activo: true),

        // AO-92 (Fox-1D)
        new SateliteAmateur(
            numeroNorad: 43137,
            nombre: "AO-92",
            indicativo: "NO-92",
            transponders: new List<TransponderSatelite>
            {
                new("FM Voz U/V", Frecuencia.DesdeMHz(435.350), Frecuencia.DesdeMHz(145.880), ModoOperacion.FM, false)
            },
            activo: true),

        // CAS-4A (ZHUHAI-1 01)
        new SateliteAmateur(
            numeroNorad: 42761,
            nombre: "CAS-4A",
            indicativo: "CAS-4A",
            transponders: new List<TransponderSatelite>
            {
                new("Lineal V/U", Frecuencia.DesdeMHz(145.855), Frecuencia.DesdeMHz(435.210), ModoOperacion.SSB, true)
            },
            activo: true),

        // CAS-4B (ZHUHAI-1 02)
        new SateliteAmateur(
            numeroNorad: 42759,
            nombre: "CAS-4B",
            indicativo: "CAS-4B",
            transponders: new List<TransponderSatelite>
            {
                new("Lineal V/U", Frecuencia.DesdeMHz(145.880), Frecuencia.DesdeMHz(435.220), ModoOperacion.SSB, true)
            },
            activo: true),

        // RS-44 (DOSAAF-85)
        new SateliteAmateur(
            numeroNorad: 44909,
            nombre: "RS-44",
            indicativo: "RS-44",
            transponders: new List<TransponderSatelite>
            {
                new("Lineal V/U", Frecuencia.DesdeMHz(145.935), Frecuencia.DesdeMHz(435.610), ModoOperacion.SSB, true),
                new("CW Baliza", Frecuencia.DesdeMHz(435.605), Frecuencia.DesdeMHz(435.605), ModoOperacion.CW, false)
            },
            activo: true),

        // FO-29 (JAS-2)
        new SateliteAmateur(
            numeroNorad: 24278,
            nombre: "FO-29",
            indicativo: "JO-29",
            transponders: new List<TransponderSatelite>
            {
                new("Lineal V/U", Frecuencia.DesdeMHz(145.900), Frecuencia.DesdeMHz(435.800), ModoOperacion.SSB, true),
                new("CW Digital", Frecuencia.DesdeMHz(145.950), Frecuencia.DesdeMHz(435.850), ModoOperacion.CW, false)
            },
            activo: true),

        // XW-2A (CAS-3A)
        new SateliteAmateur(
            numeroNorad: 40903,
            nombre: "XW-2A",
            indicativo: "XW-2A",
            transponders: new List<TransponderSatelite>
            {
                new("Lineal V/U", Frecuencia.DesdeMHz(145.660), Frecuencia.DesdeMHz(435.030), ModoOperacion.SSB, true),
                new("CW Baliza", Frecuencia.DesdeMHz(435.030), Frecuencia.DesdeMHz(435.030), ModoOperacion.CW, false)
            },
            activo: true),

        // XW-2B (CAS-3B)
        new SateliteAmateur(
            numeroNorad: 40911,
            nombre: "XW-2B",
            indicativo: "XW-2B",
            transponders: new List<TransponderSatelite>
            {
                new("Lineal V/U", Frecuencia.DesdeMHz(145.725), Frecuencia.DesdeMHz(435.090), ModoOperacion.SSB, true)
            },
            activo: true),

        // XW-2C (CAS-3C)
        new SateliteAmateur(
            numeroNorad: 40906,
            nombre: "XW-2C",
            indicativo: "XW-2C",
            transponders: new List<TransponderSatelite>
            {
                new("Lineal V/U", Frecuencia.DesdeMHz(145.790), Frecuencia.DesdeMHz(435.150), ModoOperacion.SSB, true)
            },
            activo: true),

        // PO-101 (Diwata-2B)
        new SateliteAmateur(
            numeroNorad: 43678,
            nombre: "PO-101",
            indicativo: "PO-101",
            transponders: new List<TransponderSatelite>
            {
                new("FM Voz", Frecuencia.DesdeMHz(145.900), Frecuencia.DesdeMHz(437.500), ModoOperacion.FM, false)
            },
            activo: true),

        // AO-73 (FUNcube-1)
        new SateliteAmateur(
            numeroNorad: 39444,
            nombre: "AO-73",
            indicativo: "AO-73",
            transponders: new List<TransponderSatelite>
            {
                new("Lineal V/U", Frecuencia.DesdeMHz(145.950), Frecuencia.DesdeMHz(435.150), ModoOperacion.SSB, true)
            },
            activo: true),

        // LILACSAT-2 (CAS-3H)
        new SateliteAmateur(
            numeroNorad: 40908,
            nombre: "LILACSAT-2",
            indicativo: "LO-90",
            transponders: new List<TransponderSatelite>
            {
                new("FM Voz", Frecuencia.DesdeMHz(145.985), Frecuencia.DesdeMHz(437.200), ModoOperacion.FM, false)
            },
            activo: true),

        // TEVEL-1
        new SateliteAmateur(
            numeroNorad: 50988,
            nombre: "TEVEL-1",
            indicativo: "4X-TEVEL1",
            transponders: new List<TransponderSatelite>
            {
                new("FM Voz", Frecuencia.DesdeMHz(145.970), Frecuencia.DesdeMHz(436.400), ModoOperacion.FM, false)
            },
            activo: true),

        // GREENCUBE (IO-117)
        new SateliteAmateur(
            numeroNorad: 53106,
            nombre: "IO-117",
            indicativo: "IO-117",
            transponders: new List<TransponderSatelite>
            {
                new("Digipeater UHF", Frecuencia.DesdeMHz(435.310), Frecuencia.DesdeMHz(435.310), ModoOperacion.PKT, false)
            },
            activo: true)
    };
}
