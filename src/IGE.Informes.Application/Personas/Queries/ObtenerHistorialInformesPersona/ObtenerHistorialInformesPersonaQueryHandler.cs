using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Queries.ObtenerHistorialInformesPersona;

public sealed class ObtenerHistorialInformesPersonaQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ObtenerHistorialInformesPersonaQuery, IReadOnlyCollection<InformeHistorialDto>>
{
    public async Task<IReadOnlyCollection<InformeHistorialDto>> Handle(
        ObtenerHistorialInformesPersonaQuery request, CancellationToken cancellationToken)
    {
        var evidencias = await dbContext.Evidencias.AsNoTracking()
            .Where(e => e.PersonaIds.Contains(request.PersonaId))
            .ToListAsync(cancellationToken);

        var informeIds = evidencias.Select(e => e.InformeId).Distinct().ToList();

        var informes = await dbContext.Informes.AsNoTracking()
            .Where(i => informeIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var resultado = informes
            .OrderByDescending(i => i.FechaAnalisis)
            .Select(i => new InformeHistorialDto(
                i.Id,
                i.IdRegistro,
                i.FechaAnalisis,
                i.Estado,
                i.DependenciaDestinoId,
                evidencias.Count(e => e.InformeId == i.Id),
                evidencias.Where(e => e.InformeId == i.Id).Select(e => e.NumeroImagen).ToList()))
            .ToList();

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Informe), null, cancellationToken);

        // Consultar el historial de una Persona identificada (con DNI) es
        // acceso a un dato personal sensible — auditoría explícita adicional
        // sobre Persona, mismo criterio que BuscarInformesQueryHandler (HU-05).
        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Persona), request.PersonaId, cancellationToken);

        return resultado;
    }
}
