using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ListarMigracionesPendientes;

public sealed class ListarMigracionesPendientesQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger, IFileStorage fileStorage)
    : IRequestHandler<ListarMigracionesPendientesQuery, IReadOnlyCollection<MigracionPendienteDto>>
{
    public async Task<IReadOnlyCollection<MigracionPendienteDto>> Handle(
        ListarMigracionesPendientesQuery request, CancellationToken cancellationToken)
    {
        var migraciones = await dbContext.MigracionesPendientes.AsNoTracking()
            .Select(m => new { m.Id, m.IdRegistro, m.CausaCaratula, m.PiezaSumarial, m.DependenciaDestinoId, m.PdfPath })
            .ToListAsync(cancellationToken);

        // IFileStorage no comparte estado mutable entre llamadas (a
        // diferencia de IAppDbContext/EF Core, que no soporta
        // SaveChangesAsync concurrente en el mismo scope) — generar las
        // URLs prefirmadas en paralelo es seguro y evita N round-trips
        // secuenciales a MinIO cuando hay varias decenas de pendientes.
        var resultado = await Task.WhenAll(migraciones.Select(async m =>
        {
            var pdfUrl = await fileStorage.ObtenerUrlDescargaAsync(m.PdfPath, cancellationToken);
            return new MigracionPendienteDto(m.Id, m.IdRegistro, m.CausaCaratula, m.PiezaSumarial, m.DependenciaDestinoId, pdfUrl);
        }));

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(MigracionPendiente), null, cancellationToken);

        return resultado;
    }
}
