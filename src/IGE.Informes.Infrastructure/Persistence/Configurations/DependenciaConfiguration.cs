using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class DependenciaConfiguration : IEntityTypeConfiguration<Dependencia>
{
    public void Configure(EntityTypeBuilder<Dependencia> builder)
    {
        builder.ToTable("Dependencias");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Tipo)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(d => d.Nombre).IsUnique();

        builder.PrimitiveCollection(d => d.BarrioIds)
            .HasColumnName("BarrioIds")
            .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
