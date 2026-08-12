using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.SugerirCausas;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Infrastructure.Busqueda;

/// <summary>
/// Vive en Infrastructure, no en Application, por el mismo motivo que
/// BuscarInformesQueryHandler: depende de similarity() (extensión pg_trgm de
/// Postgres, ver FuncionesPostgres), que Application no puede referenciar
/// (Clean Architecture).
/// </summary>
public sealed class SugerirCausasQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<SugerirCausasQuery, IReadOnlyCollection<CausaSugeridaDto>>
{
    // Umbral de similaridad de pg_trgm (0 a 1) — 0.3 es el default que usa
    // Postgres para el operador "%" de pg_trgm, suficientemente laxo para
    // tolerar variaciones de transcripción entre PDFs del mismo expediente
    // sin devolver resultados irrelevantes.
    private const double UmbralSimilaridad = 0.3;

    public async Task<IReadOnlyCollection<CausaSugeridaDto>> Handle(SugerirCausasQuery request, CancellationToken cancellationToken)
    {
        var resultado = await dbContext.Causas.AsNoTracking()
            .Where(c => FuncionesPostgres.Similarity(c.Caratula, request.CaratulaAproximada) >= UmbralSimilaridad)
            .OrderByDescending(c => FuncionesPostgres.Similarity(c.Caratula, request.CaratulaAproximada))
            .Select(c => new CausaSugeridaDto(c.Id, c.Caratula, c.NroPiezaSumarial, c.CircunscripcionJudicial))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Busqueda", nameof(Causa), null, cancellationToken);

        return resultado;
    }
}
