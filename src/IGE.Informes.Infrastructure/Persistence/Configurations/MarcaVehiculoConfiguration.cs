using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class MarcaVehiculoConfiguration : IEntityTypeConfiguration<MarcaVehiculo>
{
    public void Configure(EntityTypeBuilder<MarcaVehiculo> builder)
    {
        builder.ToTable("MarcasVehiculo");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(m => m.Nombre).IsUnique();
    }
}
