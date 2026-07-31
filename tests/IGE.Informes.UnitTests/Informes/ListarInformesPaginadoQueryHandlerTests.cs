using IGE.Informes.Application.Informes.Queries.ListarInformesPaginado;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class ListarInformesPaginadoQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_los_informes_ordenados_por_fecha_descendente_y_registra_el_acceso()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Informes.Add(new Informe("1/2026", new DateOnly(2026, 7, 20), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        dbContext.Informes.Add(new Informe("2/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ListarInformesPaginadoQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ListarInformesPaginadoQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(2, resultado.TotalItems);
        Assert.Equal("2/2026", resultado.Items.First().IdRegistro);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Listado", nameof(Informe), (Guid?)null), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task Pagina_los_resultados_segun_tamanio_de_pagina()
    {
        var dbContext = new TestAppDbContext();
        for (var i = 1; i <= 5; i++)
        {
            dbContext.Informes.Add(new Informe($"{i}/2026", new DateOnly(2026, 7, i), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        }
        await dbContext.SaveChangesAsync();

        var handler = new ListarInformesPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var primeraPagina = await handler.Handle(new ListarInformesPaginadoQuery(Pagina: 1, TamanioPagina: 2), CancellationToken.None);

        Assert.Equal(2, primeraPagina.Items.Count);
        Assert.Equal(5, primeraPagina.TotalItems);
        Assert.Equal(3, primeraPagina.TotalPaginas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_pagina_menor_a_uno(int pagina)
    {
        var validator = new ListarInformesPaginadoQueryValidator();

        var resultado = validator.Validate(new ListarInformesPaginadoQuery(Pagina: pagina));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Rechaza_tamanio_de_pagina_fuera_de_rango(int tamanioPagina)
    {
        var validator = new ListarInformesPaginadoQueryValidator();

        var resultado = validator.Validate(new ListarInformesPaginadoQuery(TamanioPagina: tamanioPagina));

        Assert.False(resultado.IsValid);
    }
}
