using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Accion)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Entidad)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.HasIndex(a => new { a.Entidad, a.EntidadId });
        builder.HasIndex(a => a.UsuarioId);
    }
}
