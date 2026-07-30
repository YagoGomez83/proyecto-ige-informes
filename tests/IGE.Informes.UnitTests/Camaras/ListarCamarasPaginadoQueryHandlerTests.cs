using IGE.Informes.Application.Camaras.Queries.ListarCamarasPaginado;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class ListarCamarasPaginadoQueryHandlerTests
{
    [Fact]
    public async Task Indica_pendiente_de_ubicacion_cuando_no_tiene_ubicacion()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Camaras.Add(new Camara("SL 18", TipoCamara.Domo, "Av. Illia"));
        dbContext.Camaras.Add(new Camara("JK 51", TipoCamara.Lpr));
        await dbContext.SaveChangesAsync();

        var handler = new ListarCamarasPaginadoQueryHandler(dbContext);
        var resultado = await handler.Handle(new ListarCamarasPaginadoQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(2, resultado.TotalItems);
        Assert.Contains(resultado.Items, c => c.Codigo == "SL 18" && !c.PendienteDeUbicacion);
        Assert.Contains(resultado.Items, c => c.Codigo == "JK 51" && c.PendienteDeUbicacion);
    }

    [Fact]
    public async Task Pagina_los_resultados_segun_tamanio_de_pagina()
    {
        var dbContext = new TestAppDbContext();
        for (var i = 0; i < 5; i++)
        {
            dbContext.Camaras.Add(new Camara($"SL {i}", TipoCamara.Domo, "Av. Illia"));
        }
        await dbContext.SaveChangesAsync();

        var handler = new ListarCamarasPaginadoQueryHandler(dbContext);

        var primeraPagina = await handler.Handle(new ListarCamarasPaginadoQuery(Pagina: 1, TamanioPagina: 2), CancellationToken.None);

        Assert.Equal(2, primeraPagina.Items.Count);
        Assert.Equal(5, primeraPagina.TotalItems);
        Assert.Equal(3, primeraPagina.TotalPaginas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_pagina_menor_a_uno(int pagina)
    {
        var validator = new ListarCamarasPaginadoQueryValidator();

        var resultado = validator.Validate(new ListarCamarasPaginadoQuery(Pagina: pagina));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Rechaza_tamanio_de_pagina_fuera_de_rango(int tamanioPagina)
    {
        var validator = new ListarCamarasPaginadoQueryValidator();

        var resultado = validator.Validate(new ListarCamarasPaginadoQuery(TamanioPagina: tamanioPagina));

        Assert.False(resultado.IsValid);
    }
}
