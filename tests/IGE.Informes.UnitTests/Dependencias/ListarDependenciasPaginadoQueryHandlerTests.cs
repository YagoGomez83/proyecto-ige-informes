using IGE.Informes.Application.Dependencias.Queries.ListarDependenciasPaginado;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Dependencias;

public class ListarDependenciasPaginadoQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_las_dependencias_ordenadas_por_nombre()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Dependencias.Add(new Dependencia("Fiscalía N°3", TipoDependencia.Fiscalia));
        dbContext.Dependencias.Add(new Dependencia("Comisaría 2°", TipoDependencia.Comisaria));
        await dbContext.SaveChangesAsync();

        var handler = new ListarDependenciasPaginadoQueryHandler(dbContext);
        var resultado = await handler.Handle(new ListarDependenciasPaginadoQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(2, resultado.TotalItems);
        Assert.Equal("Comisaría 2°", resultado.Items.First().Nombre);
    }

    [Fact]
    public async Task Pagina_los_resultados_segun_tamanio_de_pagina()
    {
        var dbContext = new TestAppDbContext();
        for (var i = 0; i < 5; i++)
        {
            dbContext.Dependencias.Add(new Dependencia($"Comisaría {i}", TipoDependencia.Comisaria));
        }
        await dbContext.SaveChangesAsync();

        var handler = new ListarDependenciasPaginadoQueryHandler(dbContext);

        var primeraPagina = await handler.Handle(new ListarDependenciasPaginadoQuery(Pagina: 1, TamanioPagina: 2), CancellationToken.None);

        Assert.Equal(2, primeraPagina.Items.Count);
        Assert.Equal(5, primeraPagina.TotalItems);
        Assert.Equal(3, primeraPagina.TotalPaginas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_pagina_menor_a_uno(int pagina)
    {
        var validator = new ListarDependenciasPaginadoQueryValidator();

        var resultado = validator.Validate(new ListarDependenciasPaginadoQuery(Pagina: pagina));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Rechaza_tamanio_de_pagina_fuera_de_rango(int tamanioPagina)
    {
        var validator = new ListarDependenciasPaginadoQueryValidator();

        var resultado = validator.Validate(new ListarDependenciasPaginadoQuery(TamanioPagina: tamanioPagina));

        Assert.False(resultado.IsValid);
    }
}
