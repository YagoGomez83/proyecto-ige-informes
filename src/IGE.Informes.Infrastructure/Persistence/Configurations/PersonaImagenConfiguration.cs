using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class PersonaImagenConfiguration : IEntityTypeConfiguration<PersonaImagen>
{
    public void Configure(EntityTypeBuilder<PersonaImagen> builder)
    {
        builder.ToTable("PersonaImagenes");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.ImagenPath).HasMaxLength(1000).IsRequired();

        builder.HasIndex(pi => pi.PersonaId);
    }
}
