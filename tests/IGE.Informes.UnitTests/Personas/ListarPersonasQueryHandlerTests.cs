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

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, p => p.Nombre == "Juan Pérez" && p.Identificada);
        Assert.Contains(resultado, p => p.Nombre == null && !p.Identificada);
        Assert.Single(auditLogger.Registros);
    }
}
