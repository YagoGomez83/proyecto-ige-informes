using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ListarCausasAutoAsignadas;

public sealed class ListarCausasAutoAsignadasQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarCausasAutoAsignadasQuery, IReadOnlyCollection<InformeConCausaAutoAsignadaDto>>
{
    private const string AccionCausaAutoAsignada = "CausaAutoAsignadaMigracion";

    public async Task<IReadOnlyCollection<InformeConCausaAutoAsignadaDto>> Handle(
        ListarCausasAutoAsignadasQuery request, CancellationToken cancellationToken)
    {
        var registros = await dbContext.AuditLogs.AsNoTracking()
            .Where(a => a.Accion == AccionCausaAutoAsignada && a.EntidadId != null)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        var informeIds = registros.Select(r => r.EntidadId!.Value).Distinct().ToList();

        var informes = await dbContext.Informes.AsNoTracking()
            .Where(i => informeIds.Contains(i.Id) && i.CausaId != null)
            .Select(i => new { i.Id, i.IdRegistro, i.CausaId })
            .ToListAsync(cancellationToken);

        var causaIds = informes.Select(i => i.CausaId!.Value).Distinct().ToList();
        var causas = await dbContext.Causas.AsNoTracking()
            .Where(c => causaIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var resultado = registros
            .Join(informes, r => r.EntidadId!.Value, i => i.Id, (registro, informe) => (registro, informe))
            .Where(x => causas.ContainsKey(x.informe.CausaId!.Value))
            .Select(x =>
            {
                var causa = causas[x.informe.CausaId!.Value];
                return new InformeConCausaAutoAsignadaDto(
                    x.informe.Id,
                    x.informe.IdRegistro,
                    causa.Caratula,
                    causa.NroPiezaSumarial,
                    x.registro.Timestamp);
            })
            .ToList();

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Informe), null, cancellationToken);

        return resultado;
    }
}
