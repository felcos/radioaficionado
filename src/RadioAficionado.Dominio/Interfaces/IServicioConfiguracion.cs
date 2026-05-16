using RadioAficionado.Dominio.Configuracion;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio para cargar y guardar la configuración completa de la aplicación.
/// La implementación decide el formato y la ubicación de persistencia.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Centraliza la carga y persistencia de toda la configuración de la aplicación (indicativo, licencia, conexiones a servicios externos, preferencias de UI, etc.).</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se llama a <see cref="CargarAsync"/> al inicio para obtener la configuración y a <see cref="GuardarAsync"/> cuando el usuario modifica preferencias.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Configuracion.ServicioConfiguracionJson</c> (persiste en archivo JSON local).</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.Infraestructura.ConfiguracionServicios.AgregarCapaDeInfraestructura()</c>.</para>
/// <para><b>Configuración necesaria:</b> El archivo JSON se crea automáticamente con valores por defecto si no existe.</para>
/// <para><b>Dependencias:</b> <c>ConfiguracionCompleta</c> (objeto de dominio con toda la configuración).</para>
/// </remarks>
public interface IServicioConfiguracion
{
    /// <summary>
    /// Carga la configuración completa desde el almacenamiento persistente.
    /// Si no existe, devuelve una configuración con valores por defecto.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La configuración completa cargada.</returns>
    Task<ConfiguracionCompleta> CargarAsync(CancellationToken ct = default);

    /// <summary>
    /// Guarda la configuración completa en el almacenamiento persistente.
    /// </summary>
    /// <param name="configuracion">La configuración a guardar.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task GuardarAsync(ConfiguracionCompleta configuracion, CancellationToken ct = default);
}
