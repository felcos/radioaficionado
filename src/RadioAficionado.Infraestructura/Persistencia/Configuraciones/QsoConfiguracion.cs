using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Configuración de EF Core para la entidad Qso.
/// Define la tabla, columnas, conversiones de objetos de valor e índices.
/// </summary>
public sealed class QsoConfiguracion : IEntityTypeConfiguration<Qso>
{
    /// <summary>
    /// Configura la tabla, columnas, conversiones e índices de la entidad Qso.
    /// </summary>
    /// <param name="builder">Constructor de la configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<Qso> builder)
    {
        builder.ToTable("Qsos");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .ValueGeneratedNever();

        // --- Conversiones de objetos de valor ---

        // Indicativo (record struct) → string (propiedad Valor)
        builder.Property(q => q.IndicativoPropio)
            .HasConversion(
                indicativo => indicativo.Valor,
                valor => new Indicativo(valor))
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(q => q.IndicativoContacto)
            .HasConversion(
                indicativo => indicativo.Valor,
                valor => new Indicativo(valor))
            .HasMaxLength(10)
            .IsRequired();

        // Frecuencia (record struct con constructor privado) → long (propiedad Hz)
        builder.Property(q => q.Frecuencia)
            .HasConversion(
                frecuencia => frecuencia.Hz,
                hz => Frecuencia.DesdeHz(hz))
            .IsRequired();

        // ModoOperacion (enum) → string para legibilidad en la base de datos
        builder.Property(q => q.Modo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // --- Propiedades simples ---

        builder.Property(q => q.SenalEnviada)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(q => q.SenalRecibida)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(q => q.Notas)
            .HasMaxLength(1000);

        // Localizador? (nullable record struct) → string?
        builder.Property(q => q.LocalizadorContacto)
            .HasConversion(
                localizador => localizador.HasValue ? localizador.Value.Valor : null,
                valor => valor != null ? new Localizador(valor) : (Localizador?)null)
            .HasMaxLength(8);

        builder.Property(q => q.FechaHoraInicio)
            .IsRequired();

        builder.Property(q => q.FechaCreacion)
            .IsRequired();

        // --- Índices ---

        builder.HasIndex(q => q.IndicativoPropio);
        builder.HasIndex(q => q.IndicativoContacto);
        builder.HasIndex(q => q.FechaHoraInicio);
        builder.HasIndex(q => q.Frecuencia);
    }
}
