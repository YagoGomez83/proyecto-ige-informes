using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        builder.ToTable("Vehiculos");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Marca).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Modelo).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Color).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Dominio).HasMaxLength(20);
        builder.Property(v => v.AvisarA).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Caracteristicas).HasMaxLength(2000);

        builder.Property(v => v.DominioCerteza).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.AccionARealizar).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(v => v.Dominio);
        builder.HasIndex(v => v.Estado);

        builder.PrimitiveCollection(v => v.CategoriasAlertaIds)
            .HasColumnName("CategoriasAlertaIds")
            .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
