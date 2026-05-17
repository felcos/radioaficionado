using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Compliance;
using RadioAficionado.Infraestructura.Configuracion;
using RadioAficionado.Infraestructura.Confirmaciones;
using RadioAficionado.Infraestructura.DxCluster;
using RadioAficionado.Infraestructura.Persistencia;
using RadioAficionado.Infraestructura.Activaciones;
using RadioAficionado.Infraestructura.Propagacion;
using RadioAficionado.Infraestructura.PskReporter;
using RadioAficionado.Infraestructura.Qsl;
using RadioAficionado.Infraestructura.Aprs;
using RadioAficionado.Infraestructura.Satelites;
using RadioAficionado.Infraestructura.Sincronizacion;

namespace RadioAficionado.Infraestructura;

/// <summary>
/// Extensiones para registrar los servicios de la capa de infraestructura en el contenedor de DI.
/// </summary>
public static class ConfiguracionServicios
{
    /// <summary>
    /// Registra los servicios de infraestructura (repositorios, unidad de trabajo, DX Cluster) en el contenedor de DI.
    /// </summary>
    /// <param name="servicios">Colección de servicios.</param>
    /// <returns>La colección de servicios para encadenar llamadas.</returns>
    public static IServiceCollection AgregarCapaDeInfraestructura(this IServiceCollection servicios)
    {
        servicios.AddScoped<IRepositorioQso, RepositorioQso>();
        servicios.AddScoped<IRepositorioActivaciones, RepositorioActivaciones>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<IServicioActivaciones, ServicioActivaciones>();
        servicios.AddSingleton<IDxCluster, ClienteDxCluster>();
        servicios.AddSingleton<IServicioCompliance, ServicioCompliance>();
        servicios.AddSingleton<ConfiguracionPskReporter>();
        servicios.AddSingleton<IPskReporter, ClientePskReporter>();
        servicios.AddSingleton<IServicioConfiguracion, ServicioConfiguracionJson>();

        // Confirmaciones externas (LoTW, eQSL, Club Log)
        servicios.AddSingleton<ConfiguracionLoTW>();
        servicios.AddSingleton<ConfiguracionEQsl>();
        servicios.AddSingleton<ConfiguracionClubLog>();
        servicios.AddHttpClient<IClienteLoTW, ClienteLoTW>();
        servicios.AddHttpClient<IClienteEQsl, ClienteEQsl>();
        servicios.AddHttpClient<IClienteClubLog, ClienteClubLog>();
        servicios.AddScoped<IServicioConfirmaciones, ServicioConfirmaciones>();

        // Propagacion HF
        servicios.AddSingleton<ConfiguracionPropagacion>();
        servicios.AddSingleton<IServicioPropagacion, ServicioPropagacion>();

        // Cliente de datos solares en tiempo real (NOAA + N0NBH)
        servicios.AddHttpClient<IClienteDatosSolares, ClienteDatosSolares>();

        // Sincronización de QSOs con la API web
        servicios.AddHttpClient<IServicioSincronizacion, ServicioSincronizacion>();

        // Generador de tarjetas QSL digitales
        servicios.AddSingleton<IGeneradorQsl, GeneradorQslSkia>();

        // APRS-IS (Automatic Packet Reporting System)
        servicios.AddSingleton<IServicioAprs, ClienteAprsIs>();

        // Tracking de satélites amateur
        servicios.AddSingleton<ConfiguracionSatelites>();
        servicios.AddHttpClient<IServicioSatelites, ServicioSatelites>();

        return servicios;
    }
}
