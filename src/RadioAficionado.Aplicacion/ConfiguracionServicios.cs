using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace RadioAficionado.Aplicacion;

/// <summary>
/// Extensiones para registrar los servicios de la capa de aplicación.
/// </summary>
public static class ConfiguracionServicios
{
    /// <summary>
    /// Registra MediatR handlers y validadores de FluentValidation.
    /// </summary>
    public static IServiceCollection AgregarCapaDeAplicacion(this IServiceCollection servicios)
    {
        Assembly ensamblado = typeof(ConfiguracionServicios).Assembly;

        servicios.AddMediatR(configuracion =>
        {
            configuracion.RegisterServicesFromAssembly(ensamblado);
        });

        servicios.AddValidatorsFromAssembly(ensamblado);

        return servicios;
    }
}
