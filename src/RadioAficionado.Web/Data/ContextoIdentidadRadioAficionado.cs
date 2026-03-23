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
    /// Hilos del foro de la comunidad.
    /// </summary>
    public DbSet<HiloForo> HilosForo { get; set; } = null!;

    /// <summary>
    /// Respuestas a los hilos del foro.
    /// </summary>
    public DbSet<RespuestaForo> RespuestasForo { get; set; } = null!;

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

        // Configuración de HiloForo
        modelBuilder.Entity<HiloForo>(entidad =>
        {
            entidad.ToTable("hilos_foro");
            entidad.HasKey(h => h.Id);
            entidad.Property(h => h.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            entidad.Property(h => h.Titulo)
                .HasColumnName("titulo")
                .HasMaxLength(200)
                .IsRequired();
            entidad.Property(h => h.Contenido)
                .HasColumnName("contenido")
                .HasColumnType("text")
                .IsRequired();
            entidad.Property(h => h.AutorId)
                .HasColumnName("autor_id")
                .IsRequired();
            entidad.Property(h => h.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("NOW()");
            entidad.Property(h => h.FechaUltimaRespuesta)
                .HasColumnName("fecha_ultima_respuesta")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("NOW()");
            entidad.Property(h => h.Categoria)
                .HasColumnName("categoria")
                .IsRequired();
            entidad.Property(h => h.Fijado)
                .HasColumnName("fijado")
                .HasDefaultValue(false);
            entidad.Property(h => h.Cerrado)
                .HasColumnName("cerrado")
                .HasDefaultValue(false);

            entidad.HasOne(h => h.Autor)
                .WithMany()
                .HasForeignKey(h => h.AutorId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasMany(h => h.Respuestas)
                .WithOne(r => r.Hilo)
                .HasForeignKey(r => r.HiloId)
                .OnDelete(DeleteBehavior.Cascade);

            entidad.HasIndex(h => h.Categoria);
            entidad.HasIndex(h => new { h.Fijado, h.FechaUltimaRespuesta });
        });

        // Configuración de RespuestaForo
        modelBuilder.Entity<RespuestaForo>(entidad =>
        {
            entidad.ToTable("respuestas_foro");
            entidad.HasKey(r => r.Id);
            entidad.Property(r => r.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            entidad.Property(r => r.HiloId)
                .HasColumnName("hilo_id")
                .IsRequired();
            entidad.Property(r => r.Contenido)
                .HasColumnName("contenido")
                .HasColumnType("text")
                .IsRequired();
            entidad.Property(r => r.AutorId)
                .HasColumnName("autor_id")
                .IsRequired();
            entidad.Property(r => r.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("NOW()");
            entidad.Property(r => r.FechaEdicion)
                .HasColumnName("fecha_edicion")
                .HasColumnType("timestamptz");

            entidad.HasOne(r => r.Autor)
                .WithMany()
                .HasForeignKey(r => r.AutorId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasIndex(r => r.HiloId);
        });
    }
}
