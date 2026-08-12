using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class TipoCausaConfiguration : IEntityTypeConfiguration<TipoCausa>
{
    public void Configure(EntityTypeBuilder<TipoCausa> builder)
    {
        builder.ToTable("TiposCausa");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(t => t.Nombre).IsUnique();
    }
}
