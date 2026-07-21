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

        builder.HasIndex(i => i.CasoAnalisisId);
        builder.HasIndex(i => i.CausaId);
        builder.HasIndex(i => i.DependenciaDestinoId);

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
