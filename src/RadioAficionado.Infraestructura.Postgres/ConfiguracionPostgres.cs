using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Infraestructura.Persistencia;

namespace RadioAficionado.Infraestructura.Postgres;

/// <summary>
/// Configuración del proveedor PostgreSQL para la aplicación web.
/// </summary>
public static class ConfiguracionPostgres
{
    /// <summary>
    /// Registra el contexto de base de datos con el proveedor PostgreSQL.
    /// </summary>
    /// <param name="servicios">Colección de servicios.</param>
    /// <param name="cadenaDeConexion">Cadena de conexión PostgreSQL (obligatoria).</param>
    /// <returns>La colección de servicios para encadenar llamadas.</returns>
    /// <exception cref="ArgumentException">Si la cadena de conexión es nula o vacía.</exception>
    public static IServiceCollection AgregarPostgres(
        this IServiceCollection servicios,
        string cadenaDeConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaDeConexion))
        {
            throw new ArgumentException(
                "La cadena de conexión PostgreSQL es obligatoria.",
                nameof(cadenaDeConexion));
        }

        servicios.AddDbContext<ContextoRadioAficionado>(opciones =>
        {
            opciones.UseNpgsql(cadenaDeConexion);
        });

        return servicios;
    }
}
