using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Representa un spot de señal digital recibido o enviado a través de PSKReporter.
/// Contiene la información del receptor, transmisor, frecuencia, modo, SNR, localizadores y hora.
/// </summary>
/// <param name="Receptor">Indicativo de la estación receptora que decodificó la señal.</param>
/// <param name="Transmisor">Indicativo de la estación transmisora cuya señal fue decodificada.</param>
/// <param name="Frecuencia">Frecuencia en la que se recibió la señal.</param>
/// <param name="Modo">Modo de operación digital utilizado.</param>
/// <param name="Snr">Relación señal/ruido en dB (puede ser negativo).</param>
/// <param name="LocalizadorReceptor">Localizador Maidenhead del receptor (opcional).</param>
/// <param name="LocalizadorTransmisor">Localizador Maidenhead del transmisor (opcional).</param>
/// <param name="Hora">Hora UTC en la que se decodificó la señal.</param>
public sealed record SpotPsk(
    Indicativo Receptor,
    Indicativo Transmisor,
    Frecuencia Frecuencia,
    ModoOperacion Modo,
    int Snr,
    Localizador? LocalizadorReceptor,
    Localizador? LocalizadorTransmisor,
    DateTime Hora);

/// <summary>
/// Configuración necesaria para conectarse al servicio PSKReporter.
/// </summary>
public sealed class ConfiguracionPskReporter
{
    /// <summary>Indicativo propio para autenticarse en PSKReporter.</summary>
    public string IndicativoPropio { get; set; } = string.Empty;

    /// <summary>Localizador Maidenhead de la estación receptora.</summary>
    public string Localizador { get; set; } = string.Empty;

    /// <summary>Identificador del software que reporta (asignado por PSKReporter).</summary>
    public string SoftwareId { get; set; } = "RadioAficionado";

    /// <summary>Versión del software que reporta.</summary>
    public string VersionSoftware { get; set; } = "1.0";

    /// <summary>Intervalo en segundos entre envíos de batch de spots (por defecto 300 = 5 minutos).</summary>
    public int IntervaloEnvioSegundos { get; set; } = 300;

    /// <summary>URL del endpoint de recepción de reportes de PSKReporter.</summary>
    public string UrlEnvio { get; set; } = "https://report.pskreporter.info/cgi-bin/HPHpreceiver.cgi";

    /// <summary>URL base de la API pública de consulta de PSKReporter.</summary>
    public string UrlConsulta { get; set; } = "https://retrieve.pskreporter.info/query";
}

/// <summary>
/// Interfaz para el cliente de PSKReporter que permite enviar y consultar
/// spots de señales digitales decodificadas.
/// </summary>
public interface IPskReporter : IAsyncDisposable
{
    /// <summary>
    /// Envía un lote de spots al servicio PSKReporter.
    /// Los spots se acumulan internamente y se envían en batch según el intervalo configurado.
    /// </summary>
    /// <param name="spots">Lista de spots a enviar.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task EnviarSpotsAsync(IReadOnlyList<SpotPsk> spots, CancellationToken ct = default);

    /// <summary>
    /// Consulta los spots recientes de un indicativo en PSKReporter.
    /// </summary>
    /// <param name="indicativo">Indicativo del que se quieren obtener los spots.</param>
    /// <param name="periodo">Período de tiempo hacia atrás desde ahora.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de spots encontrados.</returns>
    Task<IReadOnlyList<SpotPsk>> ObtenerSpotsAsync(Indicativo indicativo, TimeSpan periodo, CancellationToken ct = default);

    /// <summary>
    /// Evento que se dispara cuando se reciben spots tras una consulta exitosa.
    /// </summary>
    event Action<IReadOnlyList<SpotPsk>>? SpotsConsultados;
}
