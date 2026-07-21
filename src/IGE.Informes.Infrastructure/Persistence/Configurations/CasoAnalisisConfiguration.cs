using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class CasoAnalisisConfiguration : IEntityTypeConfiguration<CasoAnalisis>
{
    public void Configure(EntityTypeBuilder<CasoAnalisis> builder)
    {
        builder.ToTable("CasosAnalisis");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Fecha).IsRequired();

        builder.Property(c => c.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Resultado)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.NroLlamado911).HasMaxLength(50);
        builder.Property(c => c.VehiculoInvolucradoTexto).HasMaxLength(1000);
        builder.Property(c => c.ElementoSustraido).HasMaxLength(1000);
        builder.Property(c => c.CamarasAnalizadasTexto).HasMaxLength(2000);
        builder.Property(c => c.Observaciones).HasMaxLength(4000);

        builder.HasIndex(c => c.DependenciaId);
        builder.HasIndex(c => c.TipoIncidenteId);

        builder.Metadata
            .FindNavigation(nameof(CasoAnalisis.Analistas))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(c => c.Analistas, casoAnalistaBuilder =>
        {
            casoAnalistaBuilder.ToTable("CasoAnalistas");
            casoAnalistaBuilder.WithOwner().HasForeignKey(ca => ca.CasoId);
            casoAnalistaBuilder.HasKey(ca => new { ca.CasoId, ca.UsuarioId });

            casoAnalistaBuilder.Property(ca => ca.Rol)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}
