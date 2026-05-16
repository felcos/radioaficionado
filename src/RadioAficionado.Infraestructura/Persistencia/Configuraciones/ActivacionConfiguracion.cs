using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Configuración de EF Core para la entidad Activacion.
/// Define la tabla, columnas, conversiones de objetos de valor, relaciones e índices.
/// </summary>
public sealed class ActivacionConfiguracion : IEntityTypeConfiguration<Activacion>
{
    /// <summary>
    /// Configura la tabla, columnas, conversiones, relaciones e índices de la entidad Activacion.
    /// </summary>
    /// <param name="builder">Constructor de la configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<Activacion> builder)
    {
        builder.ToTable("Activaciones");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        // --- Conversiones de objetos de valor ---

        // TipoActivacion (enum) → string para legibilidad en la base de datos
        builder.Property(a => a.TipoActivacion)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.Referencia)
            .HasMaxLength(20)
            .IsRequired();

        // Indicativo (record struct) → string
        builder.Property(a => a.IndicativoActivador)
            .HasConversion(
                indicativo => indicativo.Valor,
                valor => new Indicativo(valor))
            .HasMaxLength(10)
            .IsRequired();

        // EstadoActivacion (enum) → string
        builder.Property(a => a.EstadoActivacion)
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsRequired();

        // Localizador? (nullable record struct) → string?
        builder.Property(a => a.Localizador)
            .HasConversion(
                localizador => localizador.HasValue ? localizador.Value.Valor : null,
                valor => valor != null ? new Localizador(valor) : (Localizador?)null)
            .HasMaxLength(8);

        builder.Property(a => a.Notas)
            .HasMaxLength(2000);

        // DateTimeOffset → string ISO 8601 para compatibilidad con SQLite ORDER BY
        builder.Property(a => a.FechaInicio)
            .HasConversion(
                v => v.ToString("O"),
                v => DateTimeOffset.Parse(v))
            .IsRequired();

        builder.Property(a => a.FechaFin)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString("O") : null,
                v => v != null ? DateTimeOffset.Parse(v) : (DateTimeOffset?)null);

        builder.Property(a => a.FechaCreacion)
            .HasConversion(
                v => v.ToString("O"),
                v => DateTimeOffset.Parse(v))
            .IsRequired();

        builder.Property(a => a.FechaModificacion)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString("O") : null,
                v => v != null ? DateTimeOffset.Parse(v) : (DateTimeOffset?)null);

        // --- Relación con QSOs ---

        builder.HasMany(a => a.Qsos)
            .WithOne()
            .HasForeignKey("ActivacionId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // --- Índices ---

        builder.HasIndex(a => a.TipoActivacion);
        builder.HasIndex(a => a.EstadoActivacion);
        builder.HasIndex(a => a.IndicativoActivador);
        builder.HasIndex(a => a.Referencia);
        builder.HasIndex(a => a.FechaInicio);
    }
}
