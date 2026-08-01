using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class AlertaConfiguration : IEntityTypeConfiguration<Alerta>
{
    public void Configure(EntityTypeBuilder<Alerta> builder)
    {
        builder.ToTable("Alertas");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(a => a.VehiculoId);
        builder.HasIndex(a => a.PersonaId);
        builder.HasIndex(a => a.InformeId);
        builder.HasIndex(a => a.Atendida);
    }
}
