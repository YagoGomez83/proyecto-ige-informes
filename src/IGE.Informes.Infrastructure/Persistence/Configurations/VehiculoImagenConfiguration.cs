using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class VehiculoImagenConfiguration : IEntityTypeConfiguration<VehiculoImagen>
{
    public void Configure(EntityTypeBuilder<VehiculoImagen> builder)
    {
        builder.ToTable("VehiculoImagenes");

        builder.HasKey(vi => vi.Id);

        builder.Property(vi => vi.ImagenPath).HasMaxLength(1000).IsRequired();

        builder.HasIndex(vi => vi.VehiculoId);
    }
}
