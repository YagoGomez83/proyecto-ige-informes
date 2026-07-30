using IGE.Informes.Application.Barrios.Queries.ListarBarrios;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Barrios;

public class ListarBarriosQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_los_barrios_ordenados_por_nombre()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Barrios.Add(new Barrio("Barrio Sur"));
        dbContext.Barrios.Add(new Barrio("Barrio Norte"));
        await dbContext.SaveChangesAsync();

        var handler = new ListarBarriosQueryHandler(dbContext);

        var resultado = (await handler.Handle(new ListarBarriosQuery(), CancellationToken.None)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Barrio Norte", resultado[0].Nombre);
        Assert.Equal("Barrio Sur", resultado[1].Nombre);
    }

    [Fact]
    public async Task Devuelve_el_nombre_de_la_localidad_para_un_barrio_que_la_tiene_asociada()
    {
        var dbContext = new TestAppDbContext();
        var localidad = new Localidad("San Luis");
        dbContext.Localidades.Add(localidad);
        dbContext.Barrios.Add(new Barrio("Barrio Norte", localidad.Id));
        await dbContext.SaveChangesAsync();

        var handler = new ListarBarriosQueryHandler(dbContext);

        var resultado = (await handler.Handle(new ListarBarriosQuery(), CancellationToken.None)).ToList();

        var barrioDto = Assert.Single(resultado);
        Assert.Equal(localidad.Id, barrioDto.LocalidadId);
        Assert.Equal("San Luis", barrioDto.LocalidadNombre);
    }

    [Fact]
    public async Task Devuelve_localidad_nula_para_un_barrio_sin_localidad_asociada()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Barrios.Add(new Barrio("Barrio Norte", null));
        await dbContext.SaveChangesAsync();

        var handler = new ListarBarriosQueryHandler(dbContext);

        var resultado = (await handler.Handle(new ListarBarriosQuery(), CancellationToken.None)).ToList();

        var barrioDto = Assert.Single(resultado);
        Assert.Null(barrioDto.LocalidadId);
        Assert.Null(barrioDto.LocalidadNombre);
    }
}
