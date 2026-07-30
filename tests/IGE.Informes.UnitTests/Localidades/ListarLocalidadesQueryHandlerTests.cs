using IGE.Informes.Application.Localidades.Queries.ListarLocalidades;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Localidades;

public class ListarLocalidadesQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_las_localidades_ordenadas_por_nombre()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Localidades.Add(new Localidad("Villa Mercedes"));
        dbContext.Localidades.Add(new Localidad("Arizona"));
        await dbContext.SaveChangesAsync();

        var handler = new ListarLocalidadesQueryHandler(dbContext);

        var resultado = (await handler.Handle(new ListarLocalidadesQuery(), CancellationToken.None)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Arizona", resultado[0].Nombre);
        Assert.Equal("Villa Mercedes", resultado[1].Nombre);
    }

    [Fact]
    public async Task Cuenta_los_barrios_que_referencian_a_la_localidad()
    {
        var dbContext = new TestAppDbContext();
        var localidad = new Localidad("San Luis");
        dbContext.Localidades.Add(localidad);
        dbContext.Barrios.Add(new Barrio("Barrio Norte", localidad.Id));
        dbContext.Barrios.Add(new Barrio("Barrio Sur", localidad.Id));
        await dbContext.SaveChangesAsync();

        var handler = new ListarLocalidadesQueryHandler(dbContext);

        var resultado = (await handler.Handle(new ListarLocalidadesQuery(), CancellationToken.None)).ToList();

        var localidadDto = Assert.Single(resultado);
        Assert.Equal(2, localidadDto.CantidadBarrios);
    }

    [Fact]
    public async Task Devuelve_cero_barrios_para_una_localidad_sin_barrios_asociados()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Localidades.Add(new Localidad("Potrero de Los Funes"));
        await dbContext.SaveChangesAsync();

        var handler = new ListarLocalidadesQueryHandler(dbContext);

        var resultado = (await handler.Handle(new ListarLocalidadesQuery(), CancellationToken.None)).ToList();

        var localidadDto = Assert.Single(resultado);
        Assert.Equal(0, localidadDto.CantidadBarrios);
    }

    [Fact]
    public async Task No_cuenta_barrios_de_otra_localidad()
    {
        var dbContext = new TestAppDbContext();
        var sanLuis = new Localidad("San Luis");
        var villaMercedes = new Localidad("Villa Mercedes");
        dbContext.Localidades.Add(sanLuis);
        dbContext.Localidades.Add(villaMercedes);
        dbContext.Barrios.Add(new Barrio("Barrio Norte", villaMercedes.Id));
        await dbContext.SaveChangesAsync();

        var handler = new ListarLocalidadesQueryHandler(dbContext);

        var resultado = (await handler.Handle(new ListarLocalidadesQuery(), CancellationToken.None)).ToList();

        var sanLuisDto = resultado.Single(l => l.Nombre == "San Luis");
        Assert.Equal(0, sanLuisDto.CantidadBarrios);
    }
}
