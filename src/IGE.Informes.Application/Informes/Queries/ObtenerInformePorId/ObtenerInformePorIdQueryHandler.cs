using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ObtenerInformePorId;

public sealed class ObtenerInformePorIdQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ObtenerInformePorIdQuery, InformeDto?>
{
    public async Task<InformeDto?> Handle(ObtenerInformePorIdQuery request, CancellationToken cancellationToken)
    {
        var informe = await dbContext.Informes.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InformeId, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Informe), request.InformeId, cancellationToken);

        if (informe is null)
        {
            return null;
        }

        return new InformeDto(
            informe.Id,
            informe.IdRegistro,
            informe.FechaAnalisis,
            informe.Relato,
            informe.CasoAnalisisId,
            informe.CausaId,
            informe.DependenciaDestinoId,
            informe.PdfPath,
            informe.Estado);
    }
}
