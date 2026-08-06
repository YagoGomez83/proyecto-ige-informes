using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IGE.Informes.Infrastructure.Persistence.Configurations;

public sealed class MigracionPendienteConfiguration : IEntityTypeConfiguration<MigracionPendiente>
{
    public void Configure(EntityTypeBuilder<MigracionPendiente> builder)
    {
        builder.ToTable("MigracionesPendientes");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.IdRegistro)
            .HasMaxLength(20);

        // Índice único PARCIAL: null significa "ID Registro no reconocido
        // todavía" (HU-04, escenario "PDF con ID Registro no reconocido
        // también queda pendiente") — Postgres no colisiona valores NULL
        // en un índice único por defecto, pero se declara explícito acá
        // para no depender de ese comportamiento implícito sin documentar
        // (ver docs/03-modelo-dominio.md, "Decisiones ya resueltas").
        builder.HasIndex(m => m.IdRegistro)
            .IsUnique()
            .HasFilter("\"IdRegistro\" IS NOT NULL");

        builder.Property(m => m.PdfPath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(m => m.CausaCaratula).HasMaxLength(500);
        builder.Property(m => m.PiezaSumarial).HasMaxLength(100);
        builder.Property(m => m.Relato).HasMaxLength(8000);

        builder.HasIndex(m => m.DependenciaDestinoId);

        // Token de concurrencia optimista sobre xmin (mismo patrón que
        // InformeConfiguration): sin esto, dos Admins completando la misma
        // MigracionPendiente casi simultáneamente (doble click, dos
        // pestañas) no chocan entre sí — "gana" el último SaveChangesAsync
        // silenciosamente, con la fecha equivocada aceptada sin error y
        // riesgo de doble Remove sobre la misma fila (hallazgo del
        // security-reviewer). El índice único en IdRegistro solo protege
        // contra duplicar el Informe resultante, no contra esta carrera
        // sobre la MigracionPendiente en sí.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
