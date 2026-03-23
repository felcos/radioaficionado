namespace RadioAficionado.Dominio.Configuracion;

/// <summary>
/// Wrapper que agrupa todas las configuraciones de la aplicación en un único objeto
/// para facilitar la persistencia y el transporte entre capas.
/// Las configuraciones de Rig, Rotador y DxCluster se representan como sub-objetos
/// con las mismas propiedades que sus clases nativas, evitando dependencias del dominio
/// hacia las capas nativas.
/// </summary>
public sealed class ConfiguracionCompleta
{
    /// <summary>Configuración de la estación del operador.</summary>
    public ConfiguracionEstacion Estacion { get; set; } = new();

    /// <summary>Configuración de conexión con el radio (rigctld).</summary>
    public ConfiguracionRigDto Rig { get; set; } = new();

    /// <summary>Configuración de conexión con el rotador (rotctld).</summary>
    public ConfiguracionRotadorDto Rotador { get; set; } = new();

    /// <summary>Configuración de dispositivos de audio.</summary>
    public ConfiguracionAudio Audio { get; set; } = new();

    /// <summary>Configuración de conexión con el DX Cluster.</summary>
    public ConfiguracionDxClusterDto DxCluster { get; set; } = new();

    /// <summary>Configuración general de la aplicación.</summary>
    public ConfiguracionGeneral General { get; set; } = new();
}

/// <summary>
/// DTO de configuración del radio para la persistencia, espejo de ConfiguracionRig nativa.
/// </summary>
public sealed class ConfiguracionRigDto
{
    /// <summary>Host donde escucha el demonio rigctld.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Puerto TCP del demonio rigctld (por defecto 4532).</summary>
    public int Puerto { get; set; } = 4532;

    /// <summary>Intervalo en milisegundos entre cada lectura de estado del radio.</summary>
    public int IntervaloPollingMs { get; set; } = 500;

    /// <summary>Potencia máxima del radio en vatios.</summary>
    public double PotenciaMaximaVatios { get; set; } = 100.0;

    /// <summary>Tiempo máximo de espera en milisegundos para una respuesta.</summary>
    public int TimeoutMs { get; set; } = 5000;
}

/// <summary>
/// DTO de configuración del rotador para la persistencia, espejo de ConfiguracionRotador nativa.
/// </summary>
public sealed class ConfiguracionRotadorDto
{
    /// <summary>Host donde escucha el demonio rotctld.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Puerto TCP del demonio rotctld (por defecto 4533).</summary>
    public int Puerto { get; set; } = 4533;

    /// <summary>Intervalo en milisegundos entre cada lectura de posición.</summary>
    public int IntervaloPollingMs { get; set; } = 1000;

    /// <summary>Umbral mínimo en grados para considerar que la posición cambió.</summary>
    public double UmbralCambioGrados { get; set; } = 0.5;

    /// <summary>Tiempo máximo de espera en milisegundos para una respuesta.</summary>
    public int TimeoutMs { get; set; } = 5000;
}

/// <summary>
/// DTO de configuración del DX Cluster para la persistencia, espejo de ConfiguracionDxCluster.
/// </summary>
public sealed class ConfiguracionDxClusterDto
{
    /// <summary>Servidor DX Cluster al que conectarse.</summary>
    public string Servidor { get; set; } = "dxc.ve7cc.net";

    /// <summary>Puerto TCP del servidor DX Cluster.</summary>
    public int Puerto { get; set; } = 7300;

    /// <summary>Indicativo propio para autenticarse en el cluster.</summary>
    public string IndicativoPropio { get; set; } = string.Empty;

    /// <summary>Tiempo máximo de espera para la conexión en milisegundos.</summary>
    public int TimeoutMs { get; set; } = 10_000;

    /// <summary>Tiempo en milisegundos antes de intentar una reconexión automática.</summary>
    public int RetrasoReconexionMs { get; set; } = 5_000;

    /// <summary>Número máximo de intentos de reconexión (0 = sin límite).</summary>
    public int MaxIntentosReconexion { get; set; } = 5;
}
