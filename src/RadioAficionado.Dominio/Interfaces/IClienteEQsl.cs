namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Configuración necesaria para conectarse al servicio eQSL.cc.
/// </summary>
public sealed class ConfiguracionEQsl
{
    /// <summary>Nombre de usuario en eQSL.cc.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Contraseña de eQSL.cc.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Carpeta local para almacenar archivos descargados de eQSL.</summary>
    public string CarpetaDescarga { get; set; } = string.Empty;

    /// <summary>URL base del servicio eQSL.</summary>
    public string UrlBase { get; set; } = "https://www.eqsl.cc";
}

/// <summary>
/// Cliente para interactuar con el servicio eQSL.cc.
/// Permite subir QSOs en formato ADIF y descargar confirmaciones electrónicas.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Comunica con el servicio eQSL.cc para subir QSOs en formato ADIF y descargar tarjetas QSL electrónicas recibidas.</para>
/// <para><b>Cómo se usa:</b> No se usa directamente — es invocado por <see cref="IServicioConfirmaciones"/>. Se puede inyectar por constructor si se necesita acceso directo.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Confirmaciones.ClienteEQsl</c>.</para>
/// <para><b>Registro DI:</b> Registrada con <c>AddHttpClient</c> (Transient con HttpClient gestionado) en <c>RadioAficionado.Infraestructura.ConfiguracionServicios.AgregarCapaDeInfraestructura()</c>.</para>
/// <para><b>Configuración necesaria:</b> Usuario y contraseña de eQSL.cc (ver <see cref="ConfiguracionEQsl"/>).</para>
/// <para><b>Dependencias:</b> <c>HttpClient</c> (inyectado por factory), <see cref="ConfiguracionEQsl"/>.</para>
/// </remarks>
public interface IClienteEQsl
{
    /// <summary>
    /// Sube contenido ADIF al servicio eQSL.cc.
    /// </summary>
    /// <param name="contenidoAdif">Contenido del archivo ADIF a subir.</param>
    /// <param name="usuario">Nombre de usuario en eQSL.</param>
    /// <param name="password">Contraseña del usuario.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado de la operación de subida.</returns>
    Task<ResultadoSubida> SubirAdifAsync(string contenidoAdif, string usuario, string password, CancellationToken ct = default);

    /// <summary>
    /// Descarga las confirmaciones (inbox) disponibles en eQSL.cc en formato ADIF.
    /// </summary>
    /// <param name="usuario">Nombre de usuario en eQSL.</param>
    /// <param name="password">Contraseña del usuario.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Contenido ADIF con las confirmaciones descargadas.</returns>
    Task<string> DescargarConfirmacionesAsync(string usuario, string password, CancellationToken ct = default);
}
