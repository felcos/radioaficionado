using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Compliance;

/// <summary>
/// Clase estática que construye los planes de banda oficiales de la ITU/IARU
/// para las bandas HF principales según la región.
/// </summary>
public static class PlanDeBandaItu
{
    private static readonly Dictionary<(BandaRadio Banda, RegionItu Region), PlanDeBanda> _planesCache = new();
    private static readonly object _bloqueo = new();
    private static bool _inicializado;

    /// <summary>
    /// Obtiene el plan de banda para una banda y región ITU específicas.
    /// </summary>
    /// <param name="banda">La banda de radioaficionado.</param>
    /// <param name="region">La región ITU.</param>
    /// <returns>El plan de banda correspondiente, o null si no hay plan definido para esa combinación.</returns>
    public static PlanDeBanda? ObtenerPlan(BandaRadio banda, RegionItu region)
    {
        InicializarSiNecesario();

        _planesCache.TryGetValue((banda, region), out PlanDeBanda? plan);
        return plan;
    }

    /// <summary>
    /// Obtiene todos los planes de banda definidos para una región ITU.
    /// </summary>
    /// <param name="region">La región ITU.</param>
    /// <returns>Lista de planes de banda para la región.</returns>
    public static IReadOnlyList<PlanDeBanda> ObtenerPlanesPorRegion(RegionItu region)
    {
        InicializarSiNecesario();

        List<PlanDeBanda> planes = _planesCache
            .Where(kvp => kvp.Key.Region == region)
            .Select(kvp => kvp.Value)
            .ToList();

        return planes.AsReadOnly();
    }

    /// <summary>
    /// Obtiene todas las bandas que tienen planes definidos.
    /// </summary>
    /// <returns>Lista de bandas con planes disponibles.</returns>
    public static IReadOnlyList<BandaRadio> ObtenerBandasDisponibles()
    {
        InicializarSiNecesario();

        List<BandaRadio> bandas = _planesCache.Keys
            .Select(k => k.Banda)
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        return bandas.AsReadOnly();
    }

    private static void InicializarSiNecesario()
    {
        if (_inicializado)
        {
            return;
        }

        lock (_bloqueo)
        {
            if (_inicializado)
            {
                return;
            }

            ConstruirPlanesRegion1();
            ConstruirPlanesRegion2();
            ConstruirPlanesRegion3();
            _inicializado = true;
        }
    }

    // ==========================================
    // REGIÓN 1 — Europa, África, Oriente Medio
    // Basado en el plan de bandas IARU Región 1
    // ==========================================

    private static void ConstruirPlanesRegion1()
    {
        ConstruirPlan160mRegion1();
        ConstruirPlan80mRegion1();
        ConstruirPlan40mRegion1();
        ConstruirPlan20mRegion1();
        ConstruirPlan15mRegion1();
        ConstruirPlan10mRegion1();
    }

    private static void ConstruirPlan160mRegion1()
    {
        PlanDeBanda plan = new(BandaRadio.Banda160m, RegionItu.Region1);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.810), Frecuencia.DesdeMHz(1.838),
            TipoSegmento.SoloCw, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.838), Frecuencia.DesdeMHz(1.840),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.840), Frecuencia.DesdeMHz(1.843),
            TipoSegmento.Digital, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.843), Frecuencia.DesdeMHz(2.000),
            TipoSegmento.TodosLosModos, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda160m, RegionItu.Region1)] = plan;
    }

    private static void ConstruirPlan80mRegion1()
    {
        PlanDeBanda plan = new(BandaRadio.Banda80m, RegionItu.Region1);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.500), Frecuencia.DesdeMHz(3.510),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200,
            notas: "Preferencia para comunicación intercontinental"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.510), Frecuencia.DesdeMHz(3.560),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200,
            notas: "CW contest preferido"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.560), Frecuencia.DesdeMHz(3.570),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.570), Frecuencia.DesdeMHz(3.580),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.580), Frecuencia.DesdeMHz(3.600),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.600), Frecuencia.DesdeMHz(3.620),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.620), Frecuencia.DesdeMHz(3.800),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda80m, RegionItu.Region1)] = plan;
    }

    private static void ConstruirPlan40mRegion1()
    {
        PlanDeBanda plan = new(BandaRadio.Banda40m, RegionItu.Region1);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.000), Frecuencia.DesdeMHz(7.040),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.040), Frecuencia.DesdeMHz(7.047),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.047), Frecuencia.DesdeMHz(7.050),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.050), Frecuencia.DesdeMHz(7.060),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.060), Frecuencia.DesdeMHz(7.100),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700,
            notas: "SSB contest preferido"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.100), Frecuencia.DesdeMHz(7.200),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda40m, RegionItu.Region1)] = plan;
    }

    private static void ConstruirPlan20mRegion1()
    {
        PlanDeBanda plan = new(BandaRadio.Banda20m, RegionItu.Region1);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.000), Frecuencia.DesdeMHz(14.060),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.060), Frecuencia.DesdeMHz(14.070),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.070), Frecuencia.DesdeMHz(14.089),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.089), Frecuencia.DesdeMHz(14.099),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.099), Frecuencia.DesdeMHz(14.101),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.101), Frecuencia.DesdeMHz(14.112),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.112), Frecuencia.DesdeMHz(14.350),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda20m, RegionItu.Region1)] = plan;
    }

    private static void ConstruirPlan15mRegion1()
    {
        PlanDeBanda plan = new(BandaRadio.Banda15m, RegionItu.Region1);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.000), Frecuencia.DesdeMHz(21.070),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.070), Frecuencia.DesdeMHz(21.090),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.090), Frecuencia.DesdeMHz(21.110),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.110), Frecuencia.DesdeMHz(21.120),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.120), Frecuencia.DesdeMHz(21.149),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.149), Frecuencia.DesdeMHz(21.151),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.151), Frecuencia.DesdeMHz(21.450),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda15m, RegionItu.Region1)] = plan;
    }

    private static void ConstruirPlan10mRegion1()
    {
        PlanDeBanda plan = new(BandaRadio.Banda10m, RegionItu.Region1);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.000), Frecuencia.DesdeMHz(28.070),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.070), Frecuencia.DesdeMHz(28.120),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.120), Frecuencia.DesdeMHz(28.150),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.150), Frecuencia.DesdeMHz(28.190),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.190), Frecuencia.DesdeMHz(28.199),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP beacons regionales"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.199), Frecuencia.DesdeMHz(28.201),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.201), Frecuencia.DesdeMHz(28.225),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "Balizas de tiempo continuo"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.225), Frecuencia.DesdeMHz(28.300),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.300), Frecuencia.DesdeMHz(29.100),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(29.100), Frecuencia.DesdeMHz(29.510),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 6000));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(29.510), Frecuencia.DesdeMHz(29.700),
            TipoSegmento.Satelites, NivelLicencia.Intermedio));

        _planesCache[(BandaRadio.Banda10m, RegionItu.Region1)] = plan;
    }

    // ==========================================
    // REGIÓN 2 — Américas
    // Basado en el plan de bandas IARU Región 2
    // ==========================================

    private static void ConstruirPlanesRegion2()
    {
        ConstruirPlan160mRegion2();
        ConstruirPlan80mRegion2();
        ConstruirPlan40mRegion2();
        ConstruirPlan20mRegion2();
        ConstruirPlan15mRegion2();
        ConstruirPlan10mRegion2();
    }

    private static void ConstruirPlan160mRegion2()
    {
        PlanDeBanda plan = new(BandaRadio.Banda160m, RegionItu.Region2);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.800), Frecuencia.DesdeMHz(1.840),
            TipoSegmento.SoloCw, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.840), Frecuencia.DesdeMHz(1.850),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.850), Frecuencia.DesdeMHz(2.000),
            TipoSegmento.TodosLosModos, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda160m, RegionItu.Region2)] = plan;
    }

    private static void ConstruirPlan80mRegion2()
    {
        PlanDeBanda plan = new(BandaRadio.Banda80m, RegionItu.Region2);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.500), Frecuencia.DesdeMHz(3.525),
            TipoSegmento.SoloCw, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.525), Frecuencia.DesdeMHz(3.580),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.580), Frecuencia.DesdeMHz(3.600),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.600), Frecuencia.DesdeMHz(3.700),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.700), Frecuencia.DesdeMHz(3.800),
            TipoSegmento.Fonia, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.800), Frecuencia.DesdeMHz(4.000),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda80m, RegionItu.Region2)] = plan;
    }

    private static void ConstruirPlan40mRegion2()
    {
        PlanDeBanda plan = new(BandaRadio.Banda40m, RegionItu.Region2);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.000), Frecuencia.DesdeMHz(7.025),
            TipoSegmento.SoloCw, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.025), Frecuencia.DesdeMHz(7.040),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.040), Frecuencia.DesdeMHz(7.080),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.080), Frecuencia.DesdeMHz(7.125),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.125), Frecuencia.DesdeMHz(7.175),
            TipoSegmento.Fonia, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.175), Frecuencia.DesdeMHz(7.300),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda40m, RegionItu.Region2)] = plan;
    }

    private static void ConstruirPlan20mRegion2()
    {
        PlanDeBanda plan = new(BandaRadio.Banda20m, RegionItu.Region2);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.000), Frecuencia.DesdeMHz(14.025),
            TipoSegmento.SoloCw, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.025), Frecuencia.DesdeMHz(14.070),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.070), Frecuencia.DesdeMHz(14.095),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.095), Frecuencia.DesdeMHz(14.099),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.099), Frecuencia.DesdeMHz(14.101),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.101), Frecuencia.DesdeMHz(14.150),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.150), Frecuencia.DesdeMHz(14.175),
            TipoSegmento.Fonia, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.175), Frecuencia.DesdeMHz(14.225),
            TipoSegmento.Fonia, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.225), Frecuencia.DesdeMHz(14.350),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda20m, RegionItu.Region2)] = plan;
    }

    private static void ConstruirPlan15mRegion2()
    {
        PlanDeBanda plan = new(BandaRadio.Banda15m, RegionItu.Region2);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.000), Frecuencia.DesdeMHz(21.025),
            TipoSegmento.SoloCw, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.025), Frecuencia.DesdeMHz(21.070),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.070), Frecuencia.DesdeMHz(21.110),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.110), Frecuencia.DesdeMHz(21.149),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.149), Frecuencia.DesdeMHz(21.151),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.151), Frecuencia.DesdeMHz(21.200),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.200), Frecuencia.DesdeMHz(21.225),
            TipoSegmento.Fonia, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.225), Frecuencia.DesdeMHz(21.275),
            TipoSegmento.Fonia, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.275), Frecuencia.DesdeMHz(21.450),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda15m, RegionItu.Region2)] = plan;
    }

    private static void ConstruirPlan10mRegion2()
    {
        PlanDeBanda plan = new(BandaRadio.Banda10m, RegionItu.Region2);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.000), Frecuencia.DesdeMHz(28.070),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.070), Frecuencia.DesdeMHz(28.150),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.150), Frecuencia.DesdeMHz(28.190),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.190), Frecuencia.DesdeMHz(28.199),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "Balizas regionales"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.199), Frecuencia.DesdeMHz(28.201),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.201), Frecuencia.DesdeMHz(28.300),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "Balizas de tiempo continuo"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.300), Frecuencia.DesdeMHz(29.100),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(29.100), Frecuencia.DesdeMHz(29.510),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 6000));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(29.510), Frecuencia.DesdeMHz(29.700),
            TipoSegmento.Satelites, NivelLicencia.Intermedio));

        _planesCache[(BandaRadio.Banda10m, RegionItu.Region2)] = plan;
    }

    // ==========================================
    // REGIÓN 3 — Asia-Pacífico
    // Basado en el plan de bandas IARU Región 3
    // ==========================================

    private static void ConstruirPlanesRegion3()
    {
        ConstruirPlan160mRegion3();
        ConstruirPlan80mRegion3();
        ConstruirPlan40mRegion3();
        ConstruirPlan20mRegion3();
        ConstruirPlan15mRegion3();
        ConstruirPlan10mRegion3();
    }

    private static void ConstruirPlan160mRegion3()
    {
        PlanDeBanda plan = new(BandaRadio.Banda160m, RegionItu.Region3);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.800), Frecuencia.DesdeMHz(1.830),
            TipoSegmento.SoloCw, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.830), Frecuencia.DesdeMHz(1.840),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(1.840), Frecuencia.DesdeMHz(2.000),
            TipoSegmento.TodosLosModos, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda160m, RegionItu.Region3)] = plan;
    }

    private static void ConstruirPlan80mRegion3()
    {
        PlanDeBanda plan = new(BandaRadio.Banda80m, RegionItu.Region3);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.500), Frecuencia.DesdeMHz(3.510),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200,
            notas: "Preferencia para comunicación intercontinental"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.510), Frecuencia.DesdeMHz(3.535),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.535), Frecuencia.DesdeMHz(3.560),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.560), Frecuencia.DesdeMHz(3.580),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.580), Frecuencia.DesdeMHz(3.600),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(3.600), Frecuencia.DesdeMHz(3.900),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda80m, RegionItu.Region3)] = plan;
    }

    private static void ConstruirPlan40mRegion3()
    {
        PlanDeBanda plan = new(BandaRadio.Banda40m, RegionItu.Region3);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.000), Frecuencia.DesdeMHz(7.025),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.025), Frecuencia.DesdeMHz(7.040),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.040), Frecuencia.DesdeMHz(7.050),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.050), Frecuencia.DesdeMHz(7.060),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.060), Frecuencia.DesdeMHz(7.100),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700,
            notas: "SSB contest preferido"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(7.100), Frecuencia.DesdeMHz(7.200),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda40m, RegionItu.Region3)] = plan;
    }

    private static void ConstruirPlan20mRegion3()
    {
        PlanDeBanda plan = new(BandaRadio.Banda20m, RegionItu.Region3);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.000), Frecuencia.DesdeMHz(14.050),
            TipoSegmento.SoloCw, NivelLicencia.Intermedio, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.050), Frecuencia.DesdeMHz(14.070),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.070), Frecuencia.DesdeMHz(14.089),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.089), Frecuencia.DesdeMHz(14.099),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.099), Frecuencia.DesdeMHz(14.101),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.101), Frecuencia.DesdeMHz(14.112),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.112), Frecuencia.DesdeMHz(14.350),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda20m, RegionItu.Region3)] = plan;
    }

    private static void ConstruirPlan15mRegion3()
    {
        PlanDeBanda plan = new(BandaRadio.Banda15m, RegionItu.Region3);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.000), Frecuencia.DesdeMHz(21.070),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.070), Frecuencia.DesdeMHz(21.090),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.090), Frecuencia.DesdeMHz(21.110),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.110), Frecuencia.DesdeMHz(21.120),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.120), Frecuencia.DesdeMHz(21.149),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.149), Frecuencia.DesdeMHz(21.151),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(21.151), Frecuencia.DesdeMHz(21.450),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        _planesCache[(BandaRadio.Banda15m, RegionItu.Region3)] = plan;
    }

    private static void ConstruirPlan10mRegion3()
    {
        PlanDeBanda plan = new(BandaRadio.Banda10m, RegionItu.Region3);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.000), Frecuencia.DesdeMHz(28.070),
            TipoSegmento.SoloCw, NivelLicencia.Basico, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.070), Frecuencia.DesdeMHz(28.150),
            TipoSegmento.CwYDigitalEstrecho, NivelLicencia.Basico, anchoDeBandaMaximoHz: 500));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.150), Frecuencia.DesdeMHz(28.190),
            TipoSegmento.Digital, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.190), Frecuencia.DesdeMHz(28.199),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.199), Frecuencia.DesdeMHz(28.201),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200,
            notas: "IBP - International Beacon Project"));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.201), Frecuencia.DesdeMHz(28.300),
            TipoSegmento.Balizas, NivelLicencia.Avanzado, anchoDeBandaMaximoHz: 200));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(28.300), Frecuencia.DesdeMHz(29.100),
            TipoSegmento.Fonia, NivelLicencia.Basico, anchoDeBandaMaximoHz: 2700));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(29.100), Frecuencia.DesdeMHz(29.510),
            TipoSegmento.TodosLosModos, NivelLicencia.Basico, anchoDeBandaMaximoHz: 6000));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(29.510), Frecuencia.DesdeMHz(29.700),
            TipoSegmento.Satelites, NivelLicencia.Intermedio));

        _planesCache[(BandaRadio.Banda10m, RegionItu.Region3)] = plan;
    }
}
