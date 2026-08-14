using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Vehiculos.Queries.BuscarVehiculos;
using IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Infrastructure.Busqueda;

/// <summary>
/// Vive en Infrastructure, no en Application, por EF.Functions.ILike —
/// extensión de Npgsql.EntityFrameworkCore.PostgreSQL, que Application no
/// puede referenciar (Clean Architecture). Mismo criterio que
/// BuscarInformesQueryHandler.
/// </summary>
public sealed class BuscarVehiculosQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<BuscarVehiculosQuery, IReadOnlyCollection<VehiculoResumenDto>>
{
    private const int LimiteResultados = 20;

    public async Task<IReadOnlyCollection<VehiculoResumenDto>> Handle(BuscarVehiculosQuery request, CancellationToken cancellationToken)
    {
        var texto = request.TextoLibre;

        // Dominio normalizado (sin espacios/guiones, mayúsculas) porque el
        // catálogo real tiene formatos inconsistentes ("IAK 796" vs
        // "AA959QL") — mismo criterio de normalización que
        // BuscarInformesQueryHandler.DominioVehiculo. ToUpper(), no
        // ToUpperInvariant(): Npgsql traduce ToUpper() a upper() en SQL, pero
        // no tiene traducción para ToUpperInvariant() (ver
        // feedback_efcore_toupperinvariant_no_traduce_npgsql).
        var textoNormalizado = texto.Replace(" ", "").Replace("-", "").ToUpper();

        // FuncionesPostgres.Unaccent(texto) no puede llamarse fuera del
        // Where ni sobre una variable ya materializada: EF Core la evaluaría
        // como constante client-side antes de traducir la query (dispara el
        // throw deliberado del método). EF.Constant(texto) dentro del lambda
        // fuerza a que la llamada quede en el árbol de expresión y se
        // traduzca a unaccent() en SQL. Concatenación con + en vez de
        // interpolación de string: string.Format no traduce con un argumento
        // no constante.
        var resultado = await dbContext.Vehiculos.AsNoTracking()
            .Where(v =>
                (v.Dominio != null && EF.Functions.ILike(v.Dominio, "%" + texto + "%")) ||
                (v.Dominio != null && v.Dominio.Replace(" ", "").Replace("-", "").ToUpper().Contains(textoNormalizado)) ||
                EF.Functions.ILike(FuncionesPostgres.Unaccent(v.Marca), "%" + FuncionesPostgres.Unaccent(EF.Constant(texto)) + "%") ||
                EF.Functions.ILike(FuncionesPostgres.Unaccent(v.Modelo), "%" + FuncionesPostgres.Unaccent(EF.Constant(texto)) + "%") ||
                (v.Caracteristicas != null && EF.Functions.ILike(FuncionesPostgres.Unaccent(v.Caracteristicas), "%" + FuncionesPostgres.Unaccent(EF.Constant(texto)) + "%")))
            .OrderBy(v => v.Marca).ThenBy(v => v.Modelo)
            .Take(LimiteResultados)
            .Select(v => new VehiculoResumenDto(v.Id, v.Marca, v.Modelo, v.Dominio, v.Estado, v.AccionARealizar, v.TipoVehiculo))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Busqueda", nameof(Vehiculo), null, cancellationToken);

        return resultado;
    }
}
