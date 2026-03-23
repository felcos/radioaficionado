using RadioAficionado.Dominio.Configuracion;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio para cargar y guardar la configuración completa de la aplicación.
/// La implementación decide el formato y la ubicación de persistencia.
/// </summary>
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
