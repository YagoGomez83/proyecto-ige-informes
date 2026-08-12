using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Dashboard.Queries.ObtenerResumenDashboard;

public sealed class ObtenerResumenDashboardQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ObtenerResumenDashboardQuery, ResumenDashboardDto>
{
    public async Task<ResumenDashboardDto> Handle(ObtenerResumenDashboardQuery request, CancellationToken cancellationToken)
    {
        var casosAbiertos = await dbContext.CasosAnalisis.AsNoTracking()
            .CountAsync(c => c.Estado == EstadoCaso.Pendiente || c.Estado == EstadoCaso.EnRevision, cancellationToken);

        // FechaBaja no nula significa "fin de vigilancia activa" (ver
        // Vehiculo.DarDeBaja) — excluirlo acá evita contar como alerta un
        // Vehículo que el usuario ya marcó explícitamente como fuera de
        // seguimiento, aunque su Estado siga en Vigente.
        var vehiculosConAlertaVigente = await dbContext.Vehiculos.AsNoTracking()
            .CountAsync(v => v.Estado == EstadoVehiculo.Vigente && v.FechaBaja == null, cancellationToken);

        var informesEnBorrador = await dbContext.Informes.AsNoTracking()
            .CountAsync(i => i.Estado == EstadoInforme.Borrador, cancellationToken);

        var informesPublicados = await dbContext.Informes.AsNoTracking()
            .CountAsync(i => i.Estado == EstadoInforme.Publicado, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Dashboard", nameof(CasoAnalisis), null, cancellationToken);

        return new ResumenDashboardDto(
            casosAbiertos,
            vehiculosConAlertaVigente,
            informesEnBorrador,
            informesPublicados);
    }
}
