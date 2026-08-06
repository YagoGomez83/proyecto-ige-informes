using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ListarMigracionesPendientes;

public sealed class ListarMigracionesPendientesQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarMigracionesPendientesQuery, IReadOnlyCollection<MigracionPendienteDto>>
{
    public async Task<IReadOnlyCollection<MigracionPendienteDto>> Handle(
        ListarMigracionesPendientesQuery request, CancellationToken cancellationToken)
    {
        var resultado = await dbContext.MigracionesPendientes.AsNoTracking()
            .Select(m => new MigracionPendienteDto(m.Id, m.IdRegistro, m.CausaCaratula, m.PiezaSumarial, m.DependenciaDestinoId))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(MigracionPendiente), null, cancellationToken);

        return resultado;
    }
}
