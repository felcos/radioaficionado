using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Persistencia;

namespace RadioAficionado.Infraestructura;

/// <summary>
/// Extensiones para registrar los servicios de la capa de infraestructura en el contenedor de DI.
/// </summary>
public static class ConfiguracionServicios
{
    /// <summary>
    /// Registra los servicios de infraestructura (repositorios, unidad de trabajo) en el contenedor de DI.
    /// </summary>
    /// <param name="servicios">Colección de servicios.</param>
    /// <returns>La colección de servicios para encadenar llamadas.</returns>
    public static IServiceCollection AgregarCapaDeInfraestructura(this IServiceCollection servicios)
    {
        servicios.AddScoped<IRepositorioQso, RepositorioQso>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

        return servicios;
    }
}
