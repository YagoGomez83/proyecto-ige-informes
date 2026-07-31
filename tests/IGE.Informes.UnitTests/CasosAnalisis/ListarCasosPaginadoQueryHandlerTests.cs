using IGE.Informes.Application.CasosAnalisis.Queries.ListarCasosPaginado;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CasosAnalisis;

public class ListarCasosPaginadoQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_los_casos_ordenados_por_fecha_descendente_y_registra_el_acceso()
    {
        var dbContext = new TestAppDbContext();
        dbContext.CasosAnalisis.Add(new CasoAnalisis(new DateOnly(2026, 7, 20), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        dbContext.CasosAnalisis.Add(new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ListarCasosPaginadoQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ListarCasosPaginadoQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(2, resultado.TotalItems);
        Assert.Equal(new DateOnly(2026, 7, 21), resultado.Items.First().Fecha);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Listado", nameof(CasoAnalisis), (Guid?)null), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task Pagina_los_resultados_segun_tamanio_de_pagina()
    {
        var dbContext = new TestAppDbContext();
        for (var i = 1; i <= 5; i++)
        {
            dbContext.CasosAnalisis.Add(new CasoAnalisis(new DateOnly(2026, 7, i), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        }
        await dbContext.SaveChangesAsync();

        var handler = new ListarCasosPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var primeraPagina = await handler.Handle(new ListarCasosPaginadoQuery(Pagina: 1, TamanioPagina: 2), CancellationToken.None);

        Assert.Equal(2, primeraPagina.Items.Count);
        Assert.Equal(5, primeraPagina.TotalItems);
        Assert.Equal(3, primeraPagina.TotalPaginas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_pagina_menor_a_uno(int pagina)
    {
        var validator = new ListarCasosPaginadoQueryValidator();

        var resultado = validator.Validate(new ListarCasosPaginadoQuery(Pagina: pagina));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Rechaza_tamanio_de_pagina_fuera_de_rango(int tamanioPagina)
    {
        var validator = new ListarCasosPaginadoQueryValidator();

        var resultado = validator.Validate(new ListarCasosPaginadoQuery(TamanioPagina: tamanioPagina));

        Assert.False(resultado.IsValid);
    }
}
