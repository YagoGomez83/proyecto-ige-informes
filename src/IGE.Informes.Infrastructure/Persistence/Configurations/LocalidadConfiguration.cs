using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class LocalidadConfiguration : IEntityTypeConfiguration<Localidad>
{
    public void Configure(EntityTypeBuilder<Localidad> builder)
    {
        builder.ToTable("Localidades");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Nombre).HasMaxLength(200).IsRequired();

        builder.HasIndex(l => l.Nombre).IsUnique();
    }
}
