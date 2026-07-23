using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Dependencias.Commands.CrearDependencia;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Dependencias;

public class CrearDependenciaCommandHandlerTests
{
    [Fact]
    public async Task Registra_una_dependencia_nueva()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearDependenciaCommandHandler(dbContext);

        var dependenciaId = await handler.Handle(
            new CrearDependenciaCommand("Comisaría Seccional Primera", TipoDependencia.Comisaria),
            CancellationToken.None);

        var dependencia = await dbContext.Dependencias.FindAsync(dependenciaId);
        Assert.Equal("Comisaría Seccional Primera", dependencia!.Nombre);
        Assert.Equal(TipoDependencia.Comisaria, dependencia.Tipo);
    }

    [Fact]
    public async Task Rechaza_un_nombre_duplicado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Dependencias.Add(new Dependencia("Comisaría Seccional Primera", TipoDependencia.Comisaria));
        await dbContext.SaveChangesAsync();

        var handler = new CrearDependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearDependenciaCommand("Comisaría Seccional Primera", TipoDependencia.Fiscalia),
            CancellationToken.None));
    }
}
