using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RadioAficionado.Dominio.Entidades;

namespace RadioAficionado.Web.Data;

/// <summary>
/// Contexto de base de datos para ASP.NET Identity.
/// Separado del contexto principal para no romper la app de escritorio que comparte el DbContext base.
/// Usa la misma base de datos PostgreSQL pero gestiona solo las tablas de identidad.
/// </summary>
public class ContextoIdentidadRadioAficionado : IdentityDbContext<UsuarioRadio>
{
    /// <summary>
    /// Crea una nueva instancia del contexto de identidad.
    /// </summary>
    /// <param name="opciones">Opciones de configuración del contexto.</param>
    public ContextoIdentidadRadioAficionado(DbContextOptions<ContextoIdentidadRadioAficionado> opciones)
        : base(opciones)
    {
    }

    /// <summary>
    /// Configura el modelo de identidad con convenciones específicas del proyecto.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Renombrar tablas de Identity a español con snake_case (convención PostgreSQL)
        modelBuilder.Entity<UsuarioRadio>(entidad =>
        {
            entidad.ToTable("usuarios");
            entidad.Property(u => u.Indicativo)
                .HasColumnName("indicativo")
                .HasMaxLength(20)
                .IsRequired();
            entidad.HasIndex(u => u.Indicativo)
                .IsUnique();
            entidad.Property(u => u.Localizador)
                .HasColumnName("localizador")
                .HasMaxLength(8);
            entidad.Property(u => u.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(200)
                .IsRequired();
            entidad.Property(u => u.FechaRegistro)
                .HasColumnName("fecha_registro")
                .HasDefaultValueSql("NOW()");
            entidad.Property(u => u.Biografia)
                .HasColumnName("biografia")
                .HasColumnType("text");
            entidad.Property(u => u.RegionItu)
                .HasColumnName("region_itu");
            entidad.HasCheckConstraint("ck_usuarios_region_itu", "region_itu IS NULL OR (region_itu >= 1 AND region_itu <= 3)");
        });
    }
}
