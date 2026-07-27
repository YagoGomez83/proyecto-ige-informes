using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class CamaraConfiguration : IEntityTypeConfiguration<Camara>
{
    public void Configure(EntityTypeBuilder<Camara> builder)
    {
        builder.ToTable("Camaras");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Codigo).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Ubicacion).HasMaxLength(300);

        builder.Property(c => c.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.Codigo);

        builder.HasOne<Dependencia>()
            .WithMany()
            .HasForeignKey(c => c.DependenciaId)
            .IsRequired(false);

        builder.HasOne<Localidad>()
            .WithMany()
            .HasForeignKey(c => c.LocalidadId)
            .IsRequired(false);

        builder.HasOne<CentroControlCamaras>()
            .WithMany()
            .HasForeignKey(c => c.CentroControlCamarasId)
            .IsRequired(false);
    }
}
