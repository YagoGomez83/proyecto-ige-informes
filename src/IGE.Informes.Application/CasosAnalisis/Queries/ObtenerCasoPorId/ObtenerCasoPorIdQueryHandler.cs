using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ObtenerCasoPorId;

public sealed class ObtenerCasoPorIdQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ObtenerCasoPorIdQuery, CasoAnalisisDto?>
{
    public async Task<CasoAnalisisDto?> Handle(ObtenerCasoPorIdQuery request, CancellationToken cancellationToken)
    {
        var caso = await dbContext.CasosAnalisis.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CasoId, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(CasoAnalisis), request.CasoId, cancellationToken);

        if (caso is null)
        {
            return null;
        }

        return new CasoAnalisisDto(
            caso.Id,
            caso.Fecha,
            caso.Estado,
            caso.Resultado,
            caso.NroLlamado911,
            caso.DependenciaId,
            caso.TipoIncidenteId,
            caso.VehiculoInvolucradoTexto,
            caso.ElementoSustraido,
            caso.CamarasAnalizadasTexto,
            caso.Observaciones);
    }
}
