using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RadioAficionado.Web.Data;

/// <summary>
/// Fabrica de contexto en tiempo de diseño para EF Core CLI.
/// Permite generar migraciones de Identity sin necesidad de ejecutar la aplicacion web completa.
/// Uso: dotnet ef migrations add NombreMigracion --project src/RadioAficionado.Web --context ContextoIdentidadRadioAficionado
/// </summary>
public class FabricaContextoIdentidadEnDiseño : IDesignTimeDbContextFactory<ContextoIdentidadRadioAficionado>
{
    /// <summary>
    /// Crea una instancia del contexto de identidad para uso en tiempo de diseño (migraciones EF Core CLI).
    /// </summary>
    /// <param name="args">Argumentos de linea de comandos.</param>
    /// <returns>Instancia del contexto configurada con PostgreSQL local.</returns>
    public ContextoIdentidadRadioAficionado CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ContextoIdentidadRadioAficionado> optionsBuilder =
            new DbContextOptionsBuilder<ContextoIdentidadRadioAficionado>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=radioaficionado;Username=postgres;Password=postgres");

        return new ContextoIdentidadRadioAficionado(optionsBuilder.Options);
    }
}
