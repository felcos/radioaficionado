namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Clase de licencia de radioaficionado. Las clases varían por país,
/// pero se mapean a un nivel de privilegios estándar.
/// </summary>
public enum NivelLicencia
{
    /// <summary>Nivel básico/novato — privilegios limitados (ej: Technician USA, Foundation UK/VK).</summary>
    Basico,

    /// <summary>Nivel intermedio — privilegios ampliados en HF (ej: General USA, Intermediate UK/VK).</summary>
    Intermedio,

    /// <summary>Nivel avanzado/completo — todos los privilegios (ej: Extra USA, Full UK/VK).</summary>
    Avanzado
}

/// <summary>
/// Representa la licencia de un operador de radioaficionado con su país y clase.
/// </summary>
public sealed class LicenciaOperador
{
    /// <summary>
    /// Indicativo del operador.
    /// </summary>
    public Indicativo Indicativo { get; }

    /// <summary>
    /// Código de país ISO 3166-1 alpha-2 (ej: "US", "ES", "VK").
    /// </summary>
    public string CodigoPais { get; }

    /// <summary>
    /// Nivel de licencia del operador.
    /// </summary>
    public NivelLicencia Nivel { get; }

    /// <summary>
    /// Región ITU correspondiente al país del operador.
    /// </summary>
    public RegionItu Region { get; }

    /// <summary>
    /// Potencia máxima permitida en vatios según la clase de licencia.
    /// </summary>
    public double PotenciaMaximaVatios { get; }

    /// <summary>
    /// Crea una nueva licencia de operador.
    /// </summary>
    public LicenciaOperador(
        Indicativo indicativo,
        string codigoPais,
        NivelLicencia nivel,
        RegionItu region,
        double potenciaMaximaVatios)
    {
        if (string.IsNullOrWhiteSpace(codigoPais))
        {
            throw new ArgumentException("El código de país no puede ser nulo ni estar vacío.", nameof(codigoPais));
        }

        if (potenciaMaximaVatios <= 0)
        {
            throw new ArgumentException("La potencia máxima debe ser positiva.", nameof(potenciaMaximaVatios));
        }

        Indicativo = indicativo;
        CodigoPais = codigoPais.ToUpperInvariant();
        Nivel = nivel;
        Region = region;
        PotenciaMaximaVatios = potenciaMaximaVatios;
    }
}
