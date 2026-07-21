using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class TipoIncidenteConfiguration : IEntityTypeConfiguration<TipoIncidente>
{
    public void Configure(EntityTypeBuilder<TipoIncidente> builder)
    {
        builder.ToTable("TiposIncidente");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Descripcion)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(t => t.Codigo).IsUnique();
    }
}
