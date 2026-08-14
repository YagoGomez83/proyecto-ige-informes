using IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class ListarVehiculosQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_todos_los_vehiculos_y_registra_el_acceso_de_listado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Vehiculos.Add(new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°", TipoVehiculo.Auto));
        dbContext.Vehiculos.Add(new Vehiculo("Ford", "Fiesta", "Rojo", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Fiscalía N°3", TipoVehiculo.Auto));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ListarVehiculosQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ListarVehiculosQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(2, resultado.TotalItems);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Listado", nameof(Vehiculo), null), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task Pagina_los_resultados_segun_tamanio_de_pagina()
    {
        var dbContext = new TestAppDbContext();
        for (var i = 0; i < 5; i++)
        {
            dbContext.Vehiculos.Add(new Vehiculo($"Marca{i}", "Modelo", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°", TipoVehiculo.Auto));
        }
        await dbContext.SaveChangesAsync();

        var handler = new ListarVehiculosQueryHandler(dbContext, new FakeAuditLogger());

        var primeraPagina = await handler.Handle(new ListarVehiculosQuery(Pagina: 1, TamanioPagina: 2), CancellationToken.None);
        var segundaPagina = await handler.Handle(new ListarVehiculosQuery(Pagina: 2, TamanioPagina: 2), CancellationToken.None);

        Assert.Equal(2, primeraPagina.Items.Count);
        Assert.Equal(2, segundaPagina.Items.Count);
        Assert.Equal(5, primeraPagina.TotalItems);
        Assert.Equal(3, primeraPagina.TotalPaginas);
        Assert.NotEqual(primeraPagina.Items.First().Id, segundaPagina.Items.First().Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_pagina_menor_a_uno(int pagina)
    {
        var validator = new ListarVehiculosQueryValidator();

        var resultado = validator.Validate(new ListarVehiculosQuery(Pagina: pagina));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(999999999)]
    public void Rechaza_tamanio_de_pagina_fuera_de_rango(int tamanioPagina)
    {
        var validator = new ListarVehiculosQueryValidator();

        var resultado = validator.Validate(new ListarVehiculosQuery(TamanioPagina: tamanioPagina));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Acepta_los_valores_por_defecto()
    {
        var validator = new ListarVehiculosQueryValidator();

        var resultado = validator.Validate(new ListarVehiculosQuery());

        Assert.True(resultado.IsValid);
    }
}
