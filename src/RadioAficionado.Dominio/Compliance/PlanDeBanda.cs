using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Compliance;

/// <summary>
/// Tipo de segmento dentro de una banda.
/// </summary>
public enum TipoSegmento
{
    /// <summary>Solo CW.</summary>
    SoloCw,

    /// <summary>CW y modos digitales de banda estrecha.</summary>
    CwYDigitalEstrecho,

    /// <summary>Todos los modos digitales.</summary>
    Digital,

    /// <summary>Solo fonía (SSB, AM, FM).</summary>
    Fonia,

    /// <summary>Todos los modos permitidos.</summary>
    TodosLosModos,

    /// <summary>Balizas y WSPR.</summary>
    Balizas,

    /// <summary>Satélites.</summary>
    Satelites,

    /// <summary>EME (moonbounce) y señal débil.</summary>
    EmeSenalDebil
}

/// <summary>
/// Representa un segmento dentro de una banda con las restricciones de modo y potencia.
/// </summary>
public sealed class SegmentoBanda
{
    /// <summary>
    /// Frecuencia de inicio del segmento.
    /// </summary>
    public Frecuencia FrecuenciaInicio { get; }

    /// <summary>
    /// Frecuencia de fin del segmento.
    /// </summary>
    public Frecuencia FrecuenciaFin { get; }

    /// <summary>
    /// Tipo de segmento (qué modos se permiten).
    /// </summary>
    public TipoSegmento Tipo { get; }

    /// <summary>
    /// Ancho de banda máximo permitido en Hz. Null si no hay restricción específica.
    /// </summary>
    public int? AnchoDeBandaMaximoHz { get; }

    /// <summary>
    /// Potencia máxima permitida en vatios para este segmento. Null si aplica la general de la licencia.
    /// </summary>
    public double? PotenciaMaximaVatios { get; }

    /// <summary>
    /// Nivel mínimo de licencia requerido para operar en este segmento.
    /// </summary>
    public NivelLicencia NivelMinimo { get; }

    /// <summary>
    /// Notas adicionales sobre el segmento (ej: "Solo Region 2", "Max 200W").
    /// </summary>
    public string? Notas { get; }

    /// <summary>
    /// Crea un nuevo segmento de banda.
    /// </summary>
    public SegmentoBanda(
        Frecuencia frecuenciaInicio,
        Frecuencia frecuenciaFin,
        TipoSegmento tipo,
        NivelLicencia nivelMinimo = NivelLicencia.Basico,
        int? anchoDeBandaMaximoHz = null,
        double? potenciaMaximaVatios = null,
        string? notas = null)
    {
        if (frecuenciaFin.Hz <= frecuenciaInicio.Hz)
        {
            throw new ArgumentException(
                "La frecuencia de fin debe ser mayor que la de inicio.",
                nameof(frecuenciaFin));
        }

        FrecuenciaInicio = frecuenciaInicio;
        FrecuenciaFin = frecuenciaFin;
        Tipo = tipo;
        NivelMinimo = nivelMinimo;
        AnchoDeBandaMaximoHz = anchoDeBandaMaximoHz;
        PotenciaMaximaVatios = potenciaMaximaVatios;
        Notas = notas;
    }

    /// <summary>
    /// Verifica si una frecuencia está dentro de este segmento.
    /// </summary>
    public bool ContieneFrecuencia(Frecuencia frecuencia)
    {
        return frecuencia.Hz >= FrecuenciaInicio.Hz && frecuencia.Hz <= FrecuenciaFin.Hz;
    }

    /// <summary>
    /// Verifica si un modo de operación está permitido en este segmento.
    /// </summary>
    public bool ModoPermitido(ModoOperacion modo)
    {
        return Tipo switch
        {
            TipoSegmento.SoloCw => modo == ModoOperacion.CW,
            TipoSegmento.CwYDigitalEstrecho => modo == ModoOperacion.CW || modo.EsDigital(),
            TipoSegmento.Digital => modo.EsDigital(),
            TipoSegmento.Fonia => modo is ModoOperacion.SSB or ModoOperacion.AM or ModoOperacion.FM,
            TipoSegmento.TodosLosModos => true,
            TipoSegmento.Balizas => modo == ModoOperacion.WSPR || modo == ModoOperacion.CW,
            TipoSegmento.Satelites => true,
            TipoSegmento.EmeSenalDebil => modo.EsSenalDebil() || modo == ModoOperacion.CW || modo == ModoOperacion.SSB,
            _ => false
        };
    }
}

/// <summary>
/// Plan de banda completo para una región y nivel de licencia específicos.
/// Define qué frecuencias y modos están permitidos.
/// </summary>
public sealed class PlanDeBanda
{
    private readonly List<SegmentoBanda> _segmentos = new();

    /// <summary>
    /// Banda a la que pertenece este plan.
    /// </summary>
    public BandaRadio Banda { get; }

    /// <summary>
    /// Región ITU para la que aplica este plan.
    /// </summary>
    public RegionItu Region { get; }

    /// <summary>
    /// Segmentos de banda definidos.
    /// </summary>
    public IReadOnlyList<SegmentoBanda> Segmentos => _segmentos.AsReadOnly();

    /// <summary>
    /// Crea un nuevo plan de banda.
    /// </summary>
    public PlanDeBanda(BandaRadio banda, RegionItu region)
    {
        Banda = banda;
        Region = region;
    }

    /// <summary>
    /// Agrega un segmento al plan de banda.
    /// </summary>
    public void AgregarSegmento(SegmentoBanda segmento)
    {
        ArgumentNullException.ThrowIfNull(segmento);
        _segmentos.Add(segmento);
    }

    /// <summary>
    /// Verifica si una frecuencia y modo están permitidos para un nivel de licencia dado.
    /// </summary>
    public ResultadoCompliance VerificarCompliance(
        Frecuencia frecuencia,
        ModoOperacion modo,
        NivelLicencia nivelOperador,
        double? potenciaVatios = null)
    {
        SegmentoBanda? segmento = _segmentos.FirstOrDefault(s => s.ContieneFrecuencia(frecuencia));

        if (segmento is null)
        {
            return ResultadoCompliance.FueraDeBanda(
                $"La frecuencia {frecuencia} no está dentro de ningún segmento del plan de banda de {Banda.ObtenerNombre()}.");
        }

        if (nivelOperador < segmento.NivelMinimo)
        {
            return ResultadoCompliance.LicenciaInsuficiente(
                $"Se requiere nivel {segmento.NivelMinimo} para operar en {frecuencia}. Su nivel: {nivelOperador}.");
        }

        if (!segmento.ModoPermitido(modo))
        {
            return ResultadoCompliance.ModoNoPermitido(
                $"El modo {modo} no está permitido en el segmento {segmento.FrecuenciaInicio}-{segmento.FrecuenciaFin} ({segmento.Tipo}).");
        }

        if (potenciaVatios.HasValue && segmento.PotenciaMaximaVatios.HasValue
            && potenciaVatios.Value > segmento.PotenciaMaximaVatios.Value)
        {
            return ResultadoCompliance.PotenciaExcedida(
                $"La potencia de {potenciaVatios.Value}W excede el máximo de {segmento.PotenciaMaximaVatios.Value}W para este segmento.");
        }

        return ResultadoCompliance.Permitido();
    }
}

/// <summary>
/// Resultado de una verificación de compliance regulatorio.
/// </summary>
public sealed class ResultadoCompliance
{
    /// <summary>
    /// Si la operación es permitida.
    /// </summary>
    public bool EsPermitido { get; }

    /// <summary>
    /// Tipo de violación si no es permitida.
    /// </summary>
    public TipoViolacion Violacion { get; }

    /// <summary>
    /// Mensaje descriptivo.
    /// </summary>
    public string Mensaje { get; }

    private ResultadoCompliance(bool esPermitido, TipoViolacion violacion, string mensaje)
    {
        EsPermitido = esPermitido;
        Violacion = violacion;
        Mensaje = mensaje;
    }

    /// <summary>Operación permitida.</summary>
    public static ResultadoCompliance Permitido()
    {
        return new ResultadoCompliance(true, TipoViolacion.Ninguna, "Operación permitida.");
    }

    /// <summary>Frecuencia fuera de banda.</summary>
    public static ResultadoCompliance FueraDeBanda(string mensaje)
    {
        return new ResultadoCompliance(false, TipoViolacion.FueraDeBanda, mensaje);
    }

    /// <summary>Licencia insuficiente.</summary>
    public static ResultadoCompliance LicenciaInsuficiente(string mensaje)
    {
        return new ResultadoCompliance(false, TipoViolacion.LicenciaInsuficiente, mensaje);
    }

    /// <summary>Modo no permitido en este segmento.</summary>
    public static ResultadoCompliance ModoNoPermitido(string mensaje)
    {
        return new ResultadoCompliance(false, TipoViolacion.ModoNoPermitido, mensaje);
    }

    /// <summary>Potencia excedida.</summary>
    public static ResultadoCompliance PotenciaExcedida(string mensaje)
    {
        return new ResultadoCompliance(false, TipoViolacion.PotenciaExcedida, mensaje);
    }
}

/// <summary>
/// Tipos de violación regulatoria.
/// </summary>
public enum TipoViolacion
{
    /// <summary>Sin violación.</summary>
    Ninguna,

    /// <summary>Frecuencia fuera del rango permitido.</summary>
    FueraDeBanda,

    /// <summary>Nivel de licencia insuficiente para este segmento.</summary>
    LicenciaInsuficiente,

    /// <summary>Modo de operación no permitido en este segmento.</summary>
    ModoNoPermitido,

    /// <summary>Potencia excede el máximo permitido.</summary>
    PotenciaExcedida
}
