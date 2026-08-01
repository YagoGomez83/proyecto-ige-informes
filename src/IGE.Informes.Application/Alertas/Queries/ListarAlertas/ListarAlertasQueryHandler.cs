using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Alertas.Queries.ListarAlertas;

public sealed class ListarAlertasQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarAlertasQuery, PagedResult<AlertaDto>>
{
    public async Task<PagedResult<AlertaDto>> Handle(ListarAlertasQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Alertas.AsNoTracking().AsQueryable();

        if (request.SoloNoAtendidas is true)
        {
            query = query.Where(a => !a.Atendida);
        }

        query = query.OrderByDescending(a => a.FechaGeneracion);

        var totalItems = await query.CountAsync(cancellationToken);

        var alertas = await query
            .Skip((request.Pagina - 1) * request.TamanioPagina)
            .Take(request.TamanioPagina)
            .ToListAsync(cancellationToken);

        var vehiculoIds = alertas.Where(a => a.VehiculoId is not null).Select(a => a.VehiculoId!.Value).Distinct().ToList();
        var personaIds = alertas.Where(a => a.PersonaId is not null).Select(a => a.PersonaId!.Value).Distinct().ToList();
        var informeIds = alertas.Select(a => a.InformeId)
            .Concat(alertas.Where(a => a.InformePrevioId is not null).Select(a => a.InformePrevioId!.Value))
            .Distinct()
            .ToList();

        var vehiculos = vehiculoIds.Count == 0
            ? []
            : await dbContext.Vehiculos.AsNoTracking()
                .Where(v => vehiculoIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => $"{v.Marca} {v.Modelo}" + (v.Dominio is not null ? $" ({v.Dominio})" : ""), cancellationToken);

        var personas = personaIds.Count == 0
            ? []
            : await dbContext.Personas.AsNoTracking()
                .Where(p => personaIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => (p.Nombre ?? "Sin identificar") + (p.Dni is not null ? $" ({p.Dni})" : ""), cancellationToken);

        var informes = informeIds.Count == 0
            ? []
            : await dbContext.Informes.AsNoTracking()
                .Where(i => informeIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.IdRegistro, cancellationToken);

        var resultado = alertas.Select(a => new AlertaDto(
            a.Id,
            a.Tipo,
            a.VehiculoId,
            a.VehiculoId is not null ? vehiculos.GetValueOrDefault(a.VehiculoId.Value) : null,
            a.PersonaId,
            a.PersonaId is not null ? personas.GetValueOrDefault(a.PersonaId.Value) : null,
            a.InformeId,
            informes.GetValueOrDefault(a.InformeId) ?? "—",
            a.InformePrevioId,
            a.InformePrevioId is not null ? informes.GetValueOrDefault(a.InformePrevioId.Value) : null,
            a.FechaGeneracion,
            a.Atendida))
            .ToList();

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(Alerta), null, cancellationToken);

        // Un registro por Id, no agregado — mismo criterio ya aplicado en
        // ObtenerInformePorIdQueryHandler (Fase C): este listado expone
        // DNI/Dominio de Vehiculo/Persona, y debe poder reconstruirse qué
        // entidades específicas se leyeron en cada acceso.
        foreach (var vehiculoId in vehiculoIds)
        {
            await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Vehiculo), vehiculoId, cancellationToken);
        }

        foreach (var personaId in personaIds)
        {
            await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Persona), personaId, cancellationToken);
        }

        foreach (var informeId in informeIds)
        {
            await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Informe), informeId, cancellationToken);
        }

        return new PagedResult<AlertaDto>(resultado, request.Pagina, request.TamanioPagina, totalItems);
    }
}
