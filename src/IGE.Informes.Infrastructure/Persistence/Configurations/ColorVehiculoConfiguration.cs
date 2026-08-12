using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class ColorVehiculoConfiguration : IEntityTypeConfiguration<ColorVehiculo>
{
    public void Configure(EntityTypeBuilder<ColorVehiculo> builder)
    {
        builder.ToTable("ColoresVehiculo");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(c => c.Nombre).IsUnique();
    }
}
