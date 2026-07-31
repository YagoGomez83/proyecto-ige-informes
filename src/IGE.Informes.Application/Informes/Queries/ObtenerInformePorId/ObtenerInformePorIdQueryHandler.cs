using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ObtenerInformePorId;

public sealed class ObtenerInformePorIdQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger, IFileStorage fileStorage)
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

        var causa = informe.CausaId is null
            ? null
            : await dbContext.Causas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == informe.CausaId, cancellationToken);

        // URL prefirmada de corta expiración para la vista previa del PDF en
        // la ficha de detalle — nunca se persiste ni se expone la clave cruda
        // del bucket, se regenera en cada consulta (ver IFileStorage).
        var pdfUrl = informe.PdfPath is null
            ? null
            : await fileStorage.ObtenerUrlDescargaAsync(informe.PdfPath, cancellationToken);

        return new InformeDto(
            informe.Id,
            informe.IdRegistro,
            informe.FechaAnalisis,
            informe.Relato,
            informe.CasoAnalisisId,
            informe.CausaId,
            causa?.Caratula,
            causa?.NroPiezaSumarial,
            causa?.CircunscripcionJudicial,
            informe.DependenciaDestinoId,
            informe.PdfPath,
            pdfUrl,
            informe.Estado,
            informe.Origen);
    }
}
