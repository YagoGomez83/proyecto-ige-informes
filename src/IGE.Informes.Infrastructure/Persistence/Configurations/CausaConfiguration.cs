using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class CausaConfiguration : IEntityTypeConfiguration<Causa>
{
    public void Configure(EntityTypeBuilder<Causa> builder)
    {
        builder.ToTable("Causas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Caratula)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.NroPiezaSumarial)
            .HasMaxLength(100);

        builder.Property(c => c.CircunscripcionJudicial)
            .HasMaxLength(200);
    }
}
