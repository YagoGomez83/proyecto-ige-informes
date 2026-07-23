using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class BarrioConfiguration : IEntityTypeConfiguration<Barrio>
{
    public void Configure(EntityTypeBuilder<Barrio> builder)
    {
        builder.ToTable("Barrios");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Nombre).HasMaxLength(200).IsRequired();

        builder.HasIndex(b => b.Nombre).IsUnique();
    }
}
