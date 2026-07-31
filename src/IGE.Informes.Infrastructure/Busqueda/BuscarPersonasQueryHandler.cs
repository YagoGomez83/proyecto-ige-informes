using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Personas.Queries.BuscarPersonas;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Infrastructure.Busqueda;

/// <summary>
/// Vive en Infrastructure, no en Application, por EF.Functions.ILike — ver
/// nota en BuscarVehiculosQueryHandler.
/// </summary>
public sealed class BuscarPersonasQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<BuscarPersonasQuery, IReadOnlyCollection<PersonaBusquedaResultDto>>
{
    private const int LimiteResultados = 20;

    public async Task<IReadOnlyCollection<PersonaBusquedaResultDto>> Handle(BuscarPersonasQuery request, CancellationToken cancellationToken)
    {
        // unaccent + ILike: un Analista buscando "perez" (sin acento, minúscula)
        // debe encontrar "Pérez" — ILike solo, aunque case-insensitive, es
        // sensible a acentos. EF.Constant(texto) fuerza a que
        // FuncionesPostgres.Unaccent quede en el árbol de expresión LINQ y se
        // traduzca a unaccent() en SQL. Concatenación con + en vez de
        // interpolación de string: string.Format no traduce con un argumento
        // no constante.
        var texto = request.TextoLibre;

        var resultado = await dbContext.Personas.AsNoTracking()
            .Where(p =>
                (p.Nombre != null && EF.Functions.ILike(FuncionesPostgres.Unaccent(p.Nombre), "%" + FuncionesPostgres.Unaccent(EF.Constant(texto)) + "%")) ||
                (p.Dni != null && EF.Functions.ILike(p.Dni, "%" + texto + "%")) ||
                (p.Caracteristicas != null && EF.Functions.ILike(FuncionesPostgres.Unaccent(p.Caracteristicas), "%" + FuncionesPostgres.Unaccent(EF.Constant(texto)) + "%")))
            .OrderBy(p => p.Nombre)
            .Take(LimiteResultados)
            .Select(p => new PersonaBusquedaResultDto(p.Id, p.Nombre, p.Dni, p.Rol))
            .ToListAsync(cancellationToken);

        // Igual que BuscarInformesQueryHandler: leer Persona por Nombre/Dni es
        // acceso a un dato personal sensible, requiere auditoría explícita
        // (docs/06-seguridad-amenazas.md), no alcanza la genérica de "Busqueda".
        await auditLogger.RegistrarAccesoAsync("Busqueda", nameof(Persona), null, cancellationToken);

        return resultado;
    }
}
