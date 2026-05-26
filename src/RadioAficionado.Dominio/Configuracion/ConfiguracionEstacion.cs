using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Configuracion;

/// <summary>
/// Configuración de la estación del operador: indicativo, localizador, región ITU,
/// nivel de licencia, potencia máxima y nombre.
/// </summary>
public sealed class ConfiguracionEstacion
{
    /// <summary>Indicativo propio del operador (ej: EA4ABC).</summary>
    public string IndicativoPropio { get; set; } = string.Empty;

    /// <summary>Localizador Maidenhead de la estación (ej: IN80DK).</summary>
    public string Localizador { get; set; } = string.Empty;

    /// <summary>Región ITU donde opera la estación.</summary>
    public RegionItu RegionItu { get; set; } = RegionItu.Region2;

    /// <summary>Nivel de licencia del operador.</summary>
    public NivelLicencia NivelLicencia { get; set; } = NivelLicencia.Basico;

    /// <summary>Potencia máxima en vatios permitida por la licencia.</summary>
    public int PotenciaMaximaVatios { get; set; } = 100;

    /// <summary>Nombre del operador.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Notas libres sobre la estacion (equipo, antenas, QTH, etc.).</summary>
    public string NotasEstacion { get; set; } = string.Empty;
}
