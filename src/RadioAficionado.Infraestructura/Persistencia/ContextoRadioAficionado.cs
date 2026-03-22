using Microsoft.EntityFrameworkCore;
using RadioAficionado.Dominio.Entidades;

namespace RadioAficionado.Infraestructura.Persistencia;

/// <summary>
/// Contexto de base de datos principal de RadioAficionado.
/// </summary>
public class ContextoRadioAficionado : DbContext
{
    /// <summary>
    /// Tabla de contactos de radio (QSOs).
    /// </summary>
    public DbSet<Qso> Qsos => Set<Qso>();

    /// <summary>
    /// Crea una nueva instancia del contexto.
    /// </summary>
    /// <param name="opciones">Opciones de configuración del contexto.</param>
    public ContextoRadioAficionado(DbContextOptions<ContextoRadioAficionado> opciones)
        : base(opciones)
    {
    }

    /// <summary>
    /// Configura el modelo de entidades aplicando las configuraciones del ensamblado.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoRadioAficionado).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
