using IGE.Informes.Application.TipoCausa.Queries.ListarTipoCausa;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.TiposCausa;

/// <summary>
/// HU-19 (Catálogo de Tipos de Causa) — cubre el Handler de
/// ListarTipoCausaQuery, mismo patrón que ListarDependenciasQueryHandler.
/// Alimenta el escenario Gherkin "Alta de un Tipo de Causa": "Entonces queda
/// disponible en el selector de Carátula al editar cualquier Informe" — el
/// selector de la UI se llena con este listado.
/// </summary>
public class ListarTipoCausaQueryHandlerTests
{
    [Fact]
    public async Task ListarTipoCausa_ConTiposDeCausaCargados_DevuelveTodosOrdenadosAlfabeticamentePorNombre()
    {
        var dbContext = new TestAppDbContext();
        dbContext.TiposCausa.Add(new TipoCausa("AV. ROBO"));
        dbContext.TiposCausa.Add(new TipoCausa("AV. HURTO CALIFICADO"));
        dbContext.TiposCausa.Add(new TipoCausa("AV. ROBO CALIFICADO"));
        await dbContext.SaveChangesAsync();

        var handler = new ListarTipoCausaQueryHandler(dbContext);

        var resultado = await handler.Handle(new ListarTipoCausaQuery(), CancellationToken.None);

        Assert.Equal(3, resultado.Count);
        Assert.Equal(
            new[] { "AV. HURTO CALIFICADO", "AV. ROBO", "AV. ROBO CALIFICADO" },
            resultado.Select(t => t.Nombre));
    }

    [Fact]
    public async Task ListarTipoCausa_SinTiposDeCausaCargados_DevuelveUnaColeccionVacia()
    {
        var dbContext = new TestAppDbContext();
        var handler = new ListarTipoCausaQueryHandler(dbContext);

        var resultado = await handler.Handle(new ListarTipoCausaQuery(), CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ListarTipoCausa_TipoDeCausaRecienCreado_QuedaIncluidoEnElListado()
    {
        var dbContext = new TestAppDbContext();
        var tipoCausa = new TipoCausa("AV. ROBO CALIFICADO");
        dbContext.TiposCausa.Add(tipoCausa);
        await dbContext.SaveChangesAsync();

        var handler = new ListarTipoCausaQueryHandler(dbContext);

        var resultado = await handler.Handle(new ListarTipoCausaQuery(), CancellationToken.None);

        Assert.Contains(resultado, t => t.Id == tipoCausa.Id && t.Nombre == "AV. ROBO CALIFICADO");
    }
}
