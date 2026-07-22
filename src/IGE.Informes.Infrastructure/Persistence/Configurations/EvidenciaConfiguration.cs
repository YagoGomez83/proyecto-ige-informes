using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class EvidenciaConfiguration : IEntityTypeConfiguration<Evidencia>
{
    public void Configure(EntityTypeBuilder<Evidencia> builder)
    {
        builder.ToTable("Evidencias");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NumeroImagen).IsRequired();
        builder.Property(e => e.Descripcion).HasMaxLength(2000);
        builder.Property(e => e.ImagenPath).HasMaxLength(1000);

        builder.HasIndex(e => e.InformeId);
        builder.HasIndex(e => e.CamaraId);
        builder.HasIndex(e => new { e.InformeId, e.NumeroImagen }).IsUnique();

        builder.PrimitiveCollection(e => e.VehiculoIds)
            .HasColumnName("VehiculoIds")
            .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.PrimitiveCollection(e => e.PersonaIds)
            .HasColumnName("PersonaIds")
            .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
