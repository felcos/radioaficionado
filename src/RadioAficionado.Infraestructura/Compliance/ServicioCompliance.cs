using RadioAficionado.Dominio.Compliance;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Compliance;

/// <summary>
/// Implementación del servicio de verificación de compliance regulatorio.
/// Verifica si las transmisiones cumplen con las regulaciones de la ITU/IARU
/// según la región y nivel de licencia del operador.
/// </summary>
public sealed class ServicioCompliance : IServicioCompliance
{
    /// <summary>
    /// Verifica si una operación cumple con las regulaciones de radio vigentes.
    /// Evalúa frecuencia, modo, nivel de licencia y potencia.
    /// </summary>
    /// <param name="frecuencia">Frecuencia de operación.</param>
    /// <param name="modo">Modo de operación.</param>
    /// <param name="licencia">Licencia del operador.</param>
    /// <param name="potenciaVatios">Potencia de transmisión en vatios (opcional).</param>
    /// <returns>Resultado de la verificación con detalle de la violación si existe.</returns>
    public ResultadoCompliance Verificar(
        Frecuencia frecuencia,
        ModoOperacion modo,
        LicenciaOperador licencia,
        double? potenciaVatios = null)
    {
        ArgumentNullException.ThrowIfNull(licencia);

        // 1. Determinar a qué banda pertenece la frecuencia
        BandaRadio? bandaNullable = BandaRadioExtensiones.DesdeFrecuencia(frecuencia);

        if (bandaNullable is null)
        {
            return ResultadoCompliance.FueraDeBanda(
                $"La frecuencia {frecuencia} no pertenece a ninguna banda de radioaficionado.");
        }

        BandaRadio banda = bandaNullable.Value;

        // 2. Obtener el plan de banda para la región del operador
        PlanDeBanda? plan = PlanDeBandaItu.ObtenerPlan(banda, licencia.Region);

        if (plan is null)
        {
            return ResultadoCompliance.FueraDeBanda(
                $"No hay plan de banda definido para {banda.ObtenerNombre()} en {licencia.Region}.");
        }

        // 3. Verificar potencia contra el límite de la licencia
        if (potenciaVatios.HasValue && potenciaVatios.Value > licencia.PotenciaMaximaVatios)
        {
            return ResultadoCompliance.PotenciaExcedida(
                $"La potencia de {potenciaVatios.Value}W excede el máximo de {licencia.PotenciaMaximaVatios}W permitido por su licencia.");
        }

        // 4. Delegar la verificación detallada al plan de banda
        return plan.VerificarCompliance(frecuencia, modo, licencia.Nivel, potenciaVatios);
    }

    /// <summary>
    /// Obtiene el plan de banda para una banda y región específicas.
    /// </summary>
    /// <param name="banda">La banda de radioaficionado.</param>
    /// <param name="region">La región ITU.</param>
    /// <returns>El plan de banda correspondiente, o null si no existe.</returns>
    public PlanDeBanda? ObtenerPlanDeBanda(BandaRadio banda, RegionItu region)
    {
        return PlanDeBandaItu.ObtenerPlan(banda, region);
    }

    /// <summary>
    /// Verifica si el operador está cerca del borde de un segmento de banda
    /// según sus privilegios de licencia. Útil para alertas en tiempo real.
    /// </summary>
    /// <param name="frecuencia">Frecuencia actual.</param>
    /// <param name="licencia">Licencia del operador.</param>
    /// <param name="margenHz">Margen en Hz para considerar "cerca del borde" (default: 1000 Hz).</param>
    /// <returns>True si está dentro del margen del borde de su segmento permitido.</returns>
    public bool EstaCercaDelBordeDeBanda(Frecuencia frecuencia, LicenciaOperador licencia, int margenHz = 1000)
    {
        ArgumentNullException.ThrowIfNull(licencia);

        BandaRadio? bandaNullable = BandaRadioExtensiones.DesdeFrecuencia(frecuencia);

        if (bandaNullable is null)
        {
            return false;
        }

        PlanDeBanda? plan = PlanDeBandaItu.ObtenerPlan(bandaNullable.Value, licencia.Region);

        if (plan is null)
        {
            return false;
        }

        // Buscar segmentos accesibles por el nivel de licencia
        IReadOnlyList<SegmentoBanda> segmentosAccesibles = ObtenerSegmentosAccesibles(plan, licencia.Nivel);

        foreach (SegmentoBanda segmento in segmentosAccesibles)
        {
            // Verificar si está cerca del borde inferior del segmento
            long distanciaInicio = Math.Abs(frecuencia.Hz - segmento.FrecuenciaInicio.Hz);
            if (distanciaInicio <= margenHz && distanciaInicio > 0)
            {
                return true;
            }

            // Verificar si está cerca del borde superior del segmento
            long distanciaFin = Math.Abs(frecuencia.Hz - segmento.FrecuenciaFin.Hz);
            if (distanciaFin <= margenHz && distanciaFin > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Obtiene todos los segmentos de todas las bandas accesibles para una licencia dada.
    /// </summary>
    /// <param name="licencia">Licencia del operador.</param>
    /// <returns>Lista de segmentos de banda accesibles para el operador.</returns>
    public IReadOnlyList<SegmentoBanda> ObtenerSegmentosPermitidos(LicenciaOperador licencia)
    {
        ArgumentNullException.ThrowIfNull(licencia);

        List<SegmentoBanda> segmentosPermitidos = new();

        IReadOnlyList<PlanDeBanda> planes = PlanDeBandaItu.ObtenerPlanesPorRegion(licencia.Region);

        foreach (PlanDeBanda plan in planes)
        {
            IReadOnlyList<SegmentoBanda> accesibles = ObtenerSegmentosAccesibles(plan, licencia.Nivel);
            segmentosPermitidos.AddRange(accesibles);
        }

        return segmentosPermitidos.AsReadOnly();
    }

    private static IReadOnlyList<SegmentoBanda> ObtenerSegmentosAccesibles(PlanDeBanda plan, NivelLicencia nivel)
    {
        List<SegmentoBanda> accesibles = plan.Segmentos
            .Where(s => nivel >= s.NivelMinimo)
            .ToList();

        return accesibles.AsReadOnly();
    }
}
