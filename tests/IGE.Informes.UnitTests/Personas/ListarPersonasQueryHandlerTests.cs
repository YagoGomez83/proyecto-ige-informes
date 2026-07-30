using IGE.Informes.Application.Personas.Queries.ListarPersonas;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class ListarPersonasQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_el_resumen_e_indica_si_esta_identificada_sin_exponer_el_DNI()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Personas.Add(new Persona(RolPersona.Sospechoso, "Juan Pérez", "30123456"));
        dbContext.Personas.Add(new Persona(RolPersona.Testigo, caracteristicas: "Mujer, 1.60m"));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ListarPersonasQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ListarPersonasQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(2, resultado.TotalItems);
        Assert.Contains(resultado.Items, p => p.Nombre == "Juan Pérez" && p.Identificada);
        Assert.Contains(resultado.Items, p => p.Nombre == null && !p.Identificada);
        Assert.Single(auditLogger.Registros);
    }

    [Fact]
    public async Task Pagina_los_resultados_segun_tamanio_de_pagina()
    {
        var dbContext = new TestAppDbContext();
        for (var i = 0; i < 5; i++)
        {
            dbContext.Personas.Add(new Persona(RolPersona.Testigo, caracteristicas: $"Persona {i}"));
        }
        await dbContext.SaveChangesAsync();

        var handler = new ListarPersonasQueryHandler(dbContext, new FakeAuditLogger());

        var primeraPagina = await handler.Handle(new ListarPersonasQuery(Pagina: 1, TamanioPagina: 2), CancellationToken.None);

        Assert.Equal(2, primeraPagina.Items.Count);
        Assert.Equal(5, primeraPagina.TotalItems);
        Assert.Equal(3, primeraPagina.TotalPaginas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_pagina_menor_a_uno(int pagina)
    {
        var validator = new ListarPersonasQueryValidator();

        var resultado = validator.Validate(new ListarPersonasQuery(Pagina: pagina));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(999999999)]
    public void Rechaza_tamanio_de_pagina_fuera_de_rango(int tamanioPagina)
    {
        var validator = new ListarPersonasQueryValidator();

        var resultado = validator.Validate(new ListarPersonasQuery(TamanioPagina: tamanioPagina));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Acepta_los_valores_por_defecto()
    {
        var validator = new ListarPersonasQueryValidator();

        var resultado = validator.Validate(new ListarPersonasQuery());

        Assert.True(resultado.IsValid);
    }
}
