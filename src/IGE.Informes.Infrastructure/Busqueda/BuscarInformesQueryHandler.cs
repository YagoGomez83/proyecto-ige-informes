using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.BuscarInformes;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Infrastructure.Busqueda;

/// <summary>
/// Vive en Infrastructure, no en Application, porque el filtro TextoLibre
/// depende de EF.Functions.ToTsVector/PlainToTsQuery — extensiones del
/// paquete Npgsql.EntityFrameworkCore.PostgreSQL, que Application no puede
/// referenciar (Clean Architecture: Application depende solo de Domain).
/// MediatR no exige que Handler y Request vivan en el mismo proyecto — el
/// Query y el DTO de resultado siguen en Application
/// (Informes/Queries/BuscarInformes/), solo el Handler está acá.
/// </summary>
public sealed class BuscarInformesQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<BuscarInformesQuery, IReadOnlyCollection<InformeBusquedaResultDto>>
{
    public async Task<IReadOnlyCollection<InformeBusquedaResultDto>> Handle(
        BuscarInformesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Informes.AsNoTracking().AsQueryable();

        if (request.DependenciaId is { } dependenciaId)
        {
            query = query.Where(i => i.DependenciaDestinoId == dependenciaId);
        }

        if (request.TipoIncidenteId is { } tipoIncidenteId)
        {
            // Un Informe migrado (CasoAnalisisId=null) nunca tiene un
            // TipoIncidente asociado — queda excluido cuando se usa este
            // filtro, es coherente con el modelo (no tienen Caso de origen).
            query = query.Where(i => dbContext.CasosAnalisis
                .Any(c => c.Id == i.CasoAnalisisId && c.TipoIncidenteId == tipoIncidenteId));
        }

        if (request.FechaDesde is { } fechaDesde)
        {
            query = query.Where(i => i.FechaAnalisis >= fechaDesde);
        }

        if (request.FechaHasta is { } fechaHasta)
        {
            query = query.Where(i => i.FechaAnalisis <= fechaHasta);
        }

        var necesitaEvidencias = !string.IsNullOrWhiteSpace(request.DominioVehiculo) || !string.IsNullOrWhiteSpace(request.DniOPersona);

        // Contains sobre una primitive collection (VehiculoIds/PersonaIds)
        // combinado con una lista externa no traduce parejo entre EF Core
        // InMemory y Postgres real — se resuelve el match de Evidencia en
        // memoria y se filtra Informes por Guid simple, que sí traduce igual
        // en ambos providers. Se carga Evidencias una sola vez y se reutiliza
        // para ambos filtros (antes se duplicaba el ToListAsync completo si
        // se combinaban DominioVehiculo + DniOPersona en la misma búsqueda —
        // hallazgo del security-reviewer).
        List<Evidencia>? evidencias = necesitaEvidencias
            ? await dbContext.Evidencias.AsNoTracking().ToListAsync(cancellationToken)
            : null;

        if (!string.IsNullOrWhiteSpace(request.DominioVehiculo))
        {
            // Mismo criterio de normalización que InformePdfParser.ExtraerVehiculos
            // (Infrastructure/PdfParsing/InformePdfParser.cs) — sin espacios ni
            // guiones, comparación case-insensitive. ToUpper() (no
            // ToUpperInvariant()) a propósito: Npgsql traduce ToUpper() a
            // upper() en SQL, pero no tiene traducción para ToUpperInvariant()
            // y tira InvalidOperationException en tiempo de ejecución contra
            // Postgres real (no se detecta con EF Core InMemory, que sí lo
            // soporta client-side — bug real encontrado en verificación manual).
            var dominioNormalizado = request.DominioVehiculo.Replace(" ", "").Replace("-", "").ToUpper();

            var vehiculoIds = await dbContext.Vehiculos
                .Where(v => v.Dominio != null
                    && v.Dominio.Replace(" ", "").Replace("-", "").ToUpper() == dominioNormalizado)
                .Select(v => v.Id)
                .ToListAsync(cancellationToken);

            var informeIdsConVehiculo = evidencias!
                .Where(e => e.VehiculoIds.Any(vehiculoIds.Contains))
                .Select(e => e.InformeId)
                .Distinct()
                .ToList();

            query = query.Where(i => informeIdsConVehiculo.Contains(i.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.DniOPersona))
        {
            var texto = request.DniOPersona;

            var personaIds = await dbContext.Personas
                .Where(p => (p.Dni != null && p.Dni == texto)
                    || (p.Nombre != null && p.Nombre.Contains(texto)))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var informeIdsConPersona = evidencias!
                .Where(e => e.PersonaIds.Any(personaIds.Contains))
                .Select(e => e.InformeId)
                .Distinct()
                .ToList();

            query = query.Where(i => informeIdsConPersona.Contains(i.Id));

            // Buscar Informes dirigidos a una Persona identificada (por DNI o
            // nombre) es una consulta activa sobre un dato personal sensible
            // (docs/06-seguridad-amenazas.md exige auditoría de lectura para
            // esto) — no alcanza con la auditoría genérica de "Busqueda"
            // sobre Informe, mismo criterio que ParsearPdfInformeQueryHandler
            // (HU-01) audita explícitamente el acceso a Persona cuando el
            // resultado incluye DNIs.
            await auditLogger.RegistrarAccesoAsync("Busqueda", nameof(Persona), null, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.TextoLibre))
        {
            query = query.Where(i => i.Relato != null
                && EF.Functions.ToTsVector("spanish", i.Relato).Matches(EF.Functions.PlainToTsQuery("spanish", request.TextoLibre)));
        }

        const int LongitudMaximaRelatoEnListado = 200;

        var resultado = await query
            .OrderByDescending(i => i.FechaAnalisis)
            .Select(i => new InformeBusquedaResultDto(
                i.Id,
                i.IdRegistro,
                i.FechaAnalisis,
                i.DependenciaDestinoId,
                // El listado de resultados no necesita el Relato completo de
                // cada Informe que matchea (solo se muestra truncado en la
                // UI) — traer todo el texto de investigaciones en curso en
                // una sola respuesta es una sobre-exposición innecesaria,
                // más marcada al no haber paginación (hallazgo del
                // security-reviewer). El detalle completo se obtiene recién
                // al abrir el Informe individual.
                i.Relato != null && i.Relato.Length > LongitudMaximaRelatoEnListado
                    ? i.Relato.Substring(0, LongitudMaximaRelatoEnListado) + "…"
                    : i.Relato,
                i.Estado))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Busqueda", nameof(Informe), null, cancellationToken);

        return resultado;
    }
}
