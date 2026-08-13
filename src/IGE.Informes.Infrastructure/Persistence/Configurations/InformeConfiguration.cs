using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class InformeConfiguration : IEntityTypeConfiguration<Informe>
{
    public void Configure(EntityTypeBuilder<Informe> builder)
    {
        builder.ToTable("Informes");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.IdRegistro)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(i => i.IdRegistro).IsUnique();

        builder.Property(i => i.FechaAnalisis).IsRequired();

        builder.Property(i => i.Relato).HasMaxLength(8000);

        builder.Property(i => i.PdfPath).HasMaxLength(1000);

        builder.Property(i => i.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Origen)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(i => i.CasoAnalisisId);
        builder.HasIndex(i => i.CausaId);
        builder.HasIndex(i => i.DependenciaDestinoId);

        builder.Property(i => i.Eliminado).IsRequired();
        builder.HasIndex(i => i.Eliminado);

        // Borrado lógico (HU-21): filtro global, no aparece en ninguna
        // consulta salvo que se use IgnoreQueryFilters() explícitamente.
        builder.HasQueryFilter(i => !i.Eliminado);

        // Token de concurrencia optimista mapeado sobre la columna de
        // sistema xmin que Postgres ya mantiene implícitamente en toda
        // tabla (no crea una columna nueva) — propiedad shadow, no requiere
        // agregar nada al Domain (Informe no conoce EF Core). Cierra la
        // ventana de carrera entre PublicarInforme y cualquier otro Command
        // que mute un Informe en Borrador (ej.
        // VincularVehiculoInforme/VincularPersonaInforme): si el Informe
        // cambió entre la lectura y el SaveChangesAsync, EF Core lanza
        // DbUpdateConcurrencyException en vez de persistir silenciosamente
        // sobre un estado ya viejo.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Metadata
            .FindNavigation(nameof(Informe.Analistas))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(i => i.Analistas, informeAnalistaBuilder =>
        {
            informeAnalistaBuilder.ToTable("InformeAnalistas");
            informeAnalistaBuilder.WithOwner().HasForeignKey(ia => ia.InformeId);
            informeAnalistaBuilder.HasKey(ia => new { ia.InformeId, ia.UsuarioId });

            informeAnalistaBuilder.Property(ia => ia.Rol)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}
