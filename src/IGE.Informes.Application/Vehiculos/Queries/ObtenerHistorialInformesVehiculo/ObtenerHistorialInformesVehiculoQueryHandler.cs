using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Queries.ObtenerHistorialInformesVehiculo;

public sealed class ObtenerHistorialInformesVehiculoQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ObtenerHistorialInformesVehiculoQuery, IReadOnlyCollection<InformeHistorialDto>>
{
    public async Task<IReadOnlyCollection<InformeHistorialDto>> Handle(
        ObtenerHistorialInformesVehiculoQuery request, CancellationToken cancellationToken)
    {
        var evidencias = await dbContext.Evidencias.AsNoTracking()
            .Where(e => e.VehiculoIds.Contains(request.VehiculoId))
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

        // CLAUDE.md, sección Seguridad: toda query que lea Vehiculo debe
        // registrar el evento en AuditLog — sin excepción por sensibilidad
        // del dato, mismo criterio que ObtenerVehiculoPorIdQueryHandler.
        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Vehiculo), request.VehiculoId, cancellationToken);

        return resultado;
    }
}
