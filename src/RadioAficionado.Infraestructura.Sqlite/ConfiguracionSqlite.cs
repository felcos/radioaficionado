using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Infraestructura.Persistencia;

namespace RadioAficionado.Infraestructura.Sqlite;

/// <summary>
/// Configuración del proveedor SQLite para la aplicación de escritorio.
/// </summary>
public static class ConfiguracionSqlite
{
    /// <summary>
    /// Registra el contexto de base de datos con el proveedor SQLite.
    /// </summary>
    /// <param name="servicios">Colección de servicios.</param>
    /// <param name="cadenaDeConexion">Cadena de conexión SQLite (por defecto: archivo local radioaficionado.db).</param>
    /// <returns>La colección de servicios para encadenar llamadas.</returns>
    public static IServiceCollection AgregarSqlite(
        this IServiceCollection servicios,
        string cadenaDeConexion = "Data Source=radioaficionado.db")
    {
        servicios.AddDbContext<ContextoRadioAficionado>(opciones =>
        {
            opciones.UseSqlite(cadenaDeConexion);
        });

        return servicios;
    }
}
