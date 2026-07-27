using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class CentroControlCamarasConfiguration : IEntityTypeConfiguration<CentroControlCamaras>
{
    public void Configure(EntityTypeBuilder<CentroControlCamaras> builder)
    {
        builder.ToTable("CentrosControlCamaras");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Sigla).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Nombre).HasMaxLength(200).IsRequired();

        builder.HasIndex(c => c.Sigla).IsUnique();
    }
}
