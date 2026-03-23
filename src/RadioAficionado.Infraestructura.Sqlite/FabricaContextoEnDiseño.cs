using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RadioAficionado.Infraestructura.Persistencia;

namespace RadioAficionado.Infraestructura.Sqlite;

/// <summary>
/// Fábrica de contexto en tiempo de diseño para EF Core CLI.
/// Permite ejecutar comandos como 'dotnet ef migrations add' sin necesidad del startup project.
/// </summary>
public class FabricaContextoEnDiseño : IDesignTimeDbContextFactory<ContextoRadioAficionado>
{
    /// <summary>
    /// Crea una instancia del contexto configurada con SQLite para uso en tiempo de diseño.
    /// </summary>
    /// <param name="args">Argumentos de línea de comandos (no utilizados).</param>
    /// <returns>Instancia configurada de <see cref="ContextoRadioAficionado"/>.</returns>
    public ContextoRadioAficionado CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ContextoRadioAficionado> constructorOpciones =
            new DbContextOptionsBuilder<ContextoRadioAficionado>();

        constructorOpciones.UseSqlite(
            "Data Source=radioaficionado.db",
            sqlite => sqlite.MigrationsAssembly("RadioAficionado.Infraestructura.Sqlite"));

        return new ContextoRadioAficionado(constructorOpciones.Options);
    }
}
