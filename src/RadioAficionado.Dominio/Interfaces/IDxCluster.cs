using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Representa un spot DX recibido desde un cluster de DX.
/// Contiene la información del spotter, la estación DX, frecuencia, comentario y hora.
/// </summary>
/// <param name="Spotteador">Indicativo de la estación que publicó el spot.</param>
/// <param name="Dx">Indicativo de la estación DX spotteada.</param>
/// <param name="Frecuencia">Frecuencia en la que se escuchó la estación DX.</param>
/// <param name="Comentario">Comentario libre del spotter (modo, señal, etc.).</param>
/// <param name="Hora">Hora UTC en la que se publicó el spot.</param>
public sealed record SpotDx(
    Indicativo Spotteador,
    Indicativo Dx,
    Frecuencia Frecuencia,
    string Comentario,
    DateTime Hora);

/// <summary>
/// Configuración para conectarse a un servidor de DX Cluster vía Telnet.
/// </summary>
public sealed class ConfiguracionDxCluster
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

    /// <summary>
    /// Lista de servidores DX Cluster populares con sus puertos.
    /// </summary>
    public static IReadOnlyList<(string Servidor, int Puerto, string Descripcion)> ServidoresConocidos { get; } =
    [
        ("dxc.ve7cc.net", 7300, "VE7CC — Canadá (muy popular)"),
        ("dxc.nc7j.com", 7373, "NC7J — Estados Unidos"),
        ("dxc.ea4abc.net", 7300, "EA4ABC — España"),
        ("dxfun.com", 8000, "DXFun — Europa"),
        ("db0sue.de", 7300, "DB0SUE — Alemania"),
        ("dx.maritimeradio.org", 7300, "VE1DX — Canadá Marítimo"),
        ("dxc.k3lr.com", 7300, "K3LR — Estados Unidos"),
        ("spider.ham-radio-op.net", 7300, "Spider — Global"),
    ];
}

/// <summary>
/// Interfaz para el cliente de DX Cluster que permite conectarse a servidores Telnet
/// de DX Cluster, recibir spots DX en tiempo real y publicar spots propios.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Conecta a servidores de DX Cluster vía Telnet para recibir spots DX en tiempo real y publicar spots propios. Fundamental para saber qué estaciones DX están activas.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se conecta con <see cref="ConectarAsync"/> y se suscribe a <see cref="SpotRecibido"/> para recibir spots en tiempo real.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.DxCluster.ClienteDxCluster</c>.</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.Infraestructura.ConfiguracionServicios.AgregarCapaDeInfraestructura()</c>.</para>
/// <para><b>Configuración necesaria:</b> Indicativo propio para autenticarse, servidor y puerto del DX Cluster (ver <see cref="ConfiguracionDxCluster"/>).</para>
/// <para><b>Dependencias:</b> Ninguna interfaz de dominio. Usa TCP para conexión Telnet al cluster.</para>
/// </remarks>
public interface IDxCluster : IAsyncDisposable
{
    /// <summary>Indica si está conectado al servidor DX Cluster.</summary>
    bool EstaConectado { get; }

    /// <summary>
    /// Conecta al servidor DX Cluster y se autentica con el indicativo proporcionado.
    /// </summary>
    /// <param name="servidor">Dirección del servidor DX Cluster.</param>
    /// <param name="puerto">Puerto TCP del servidor.</param>
    /// <param name="indicativo">Indicativo propio para autenticarse.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ConectarAsync(string servidor, int puerto, string indicativo, CancellationToken ct = default);

    /// <summary>
    /// Desconecta del servidor DX Cluster.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    Task DesconectarAsync(CancellationToken ct = default);

    /// <summary>
    /// Envía un spot DX al cluster.
    /// </summary>
    /// <param name="spot">El spot DX a publicar.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task EnviarSpotAsync(SpotDx spot, CancellationToken ct = default);

    /// <summary>
    /// Evento que se dispara cuando se recibe un spot DX desde el cluster.
    /// </summary>
    event EventHandler<SpotDx>? SpotRecibido;

    /// <summary>
    /// Evento que se dispara cuando se pierde la conexión con el cluster.
    /// </summary>
    event EventHandler<string>? ConexionPerdida;

    /// <summary>
    /// Evento que se dispara cuando se reconecta exitosamente al cluster.
    /// </summary>
    event EventHandler? Reconectado;
}
