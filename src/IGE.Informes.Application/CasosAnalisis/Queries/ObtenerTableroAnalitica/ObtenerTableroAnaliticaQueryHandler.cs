using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ObtenerTableroAnalitica;

public sealed class ObtenerTableroAnaliticaQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ObtenerTableroAnaliticaQuery, TableroAnaliticaDto>
{
    public async Task<TableroAnaliticaDto> Handle(ObtenerTableroAnaliticaQuery request, CancellationToken cancellationToken)
    {
        var casos = dbContext.CasosAnalisis.AsNoTracking()
            .Where(c => request.FechaDesde == null || c.Fecha >= request.FechaDesde)
            .Where(c => request.FechaHasta == null || c.Fecha <= request.FechaHasta);

        var porDependencia = await casos
            .GroupBy(c => c.DependenciaId)
            .Select(g => new ConteoPorDependenciaDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var porTipoIncidente = await casos
            .GroupBy(c => c.TipoIncidenteId)
            .Select(g => new ConteoPorTipoIncidenteDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var porAnalista = await casos
            .SelectMany(c => c.Analistas)
            .GroupBy(a => a.UsuarioId)
            .Select(g => new ConteoPorAnalistaDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var porResultado = await casos
            .Where(c => c.Resultado != null)
            .GroupBy(c => c.Resultado!.Value)
            .Select(g => new ConteoPorResultadoDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Analitica", nameof(CasoAnalisis), null, cancellationToken);

        return new TableroAnaliticaDto(porDependencia, porTipoIncidente, porAnalista, porResultado);
    }
}
