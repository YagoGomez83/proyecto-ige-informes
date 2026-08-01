using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class PersonaVehiculoConfiguration : IEntityTypeConfiguration<PersonaVehiculo>
{
    public void Configure(EntityTypeBuilder<PersonaVehiculo> builder)
    {
        builder.ToTable("PersonasVehiculo");

        builder.HasKey(pv => pv.Id);

        builder.HasIndex(pv => new { pv.PersonaId, pv.VehiculoId }).IsUnique();
    }
}
