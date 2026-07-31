using IGE.Informes.Application.CasosAnalisis.Queries.BuscarCasos;
using IGE.Informes.Application.CasosAnalisis.Queries.ListarCasos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Infrastructure.Busqueda;

/// <summary>
/// Vive en Infrastructure, no en Application, por EF.Functions.ILike — ver
/// nota en BuscarVehiculosQueryHandler.
/// </summary>
public sealed class BuscarCasosQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<BuscarCasosQuery, IReadOnlyCollection<CasoAnalisisResumenDto>>
{
    private const int LimiteResultados = 20;

    public async Task<IReadOnlyCollection<CasoAnalisisResumenDto>> Handle(BuscarCasosQuery request, CancellationToken cancellationToken)
    {
        // unaccent + ILike: mismo criterio que BuscarPersonasQueryHandler/
        // BuscarVehiculosQueryHandler. EF.Constant(texto) fuerza a que
        // FuncionesPostgres.Unaccent quede en el árbol de expresión LINQ y se
        // traduzca a unaccent() en SQL. Concatenación con + en vez de
        // interpolación de string ($"..."): string.Format (lo que genera la
        // interpolación) no traduce cuando el argumento no es una constante
        // literal, "%" + x + "%" sí.
        var texto = request.TextoLibre;

        var resultado = await dbContext.CasosAnalisis.AsNoTracking()
            .Where(c =>
                (c.Observaciones != null && EF.Functions.ILike(FuncionesPostgres.Unaccent(c.Observaciones), "%" + FuncionesPostgres.Unaccent(EF.Constant(texto)) + "%")) ||
                (c.ElementoSustraido != null && EF.Functions.ILike(FuncionesPostgres.Unaccent(c.ElementoSustraido), "%" + FuncionesPostgres.Unaccent(EF.Constant(texto)) + "%")) ||
                (c.VehiculoInvolucradoTexto != null && EF.Functions.ILike(FuncionesPostgres.Unaccent(c.VehiculoInvolucradoTexto), "%" + FuncionesPostgres.Unaccent(EF.Constant(texto)) + "%")) ||
                (c.NroLlamado911 != null && EF.Functions.ILike(c.NroLlamado911, "%" + texto + "%")))
            .OrderByDescending(c => c.Fecha)
            .Take(LimiteResultados)
            .Select(c => new CasoAnalisisResumenDto(c.Id, c.Fecha, c.Estado, c.Resultado, c.DependenciaId, c.TipoIncidenteId))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Busqueda", nameof(CasoAnalisis), null, cancellationToken);

        return resultado;
    }
}
