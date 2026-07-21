using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("Personas");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre).HasMaxLength(200);
        builder.Property(p => p.Dni).HasMaxLength(20);
        builder.Property(p => p.Caracteristicas).HasMaxLength(2000);

        builder.Property(p => p.Rol)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(p => p.Dni);
    }
}
