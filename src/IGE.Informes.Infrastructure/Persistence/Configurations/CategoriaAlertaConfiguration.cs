using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class CategoriaAlertaConfiguration : IEntityTypeConfiguration<CategoriaAlerta>
{
    public void Configure(EntityTypeBuilder<CategoriaAlerta> builder)
    {
        builder.ToTable("CategoriasAlerta");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(c => c.Nombre).IsUnique();
    }
}
