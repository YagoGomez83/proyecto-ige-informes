using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.CasosAnalisis.Commands.CerrarCaso;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CasosAnalisis;

public class CerrarCasoCommandHandlerTests
{
    private static async Task<(TestAppDbContext DbContext, CasoAnalisis Caso)> PrepararCasoAsync()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        dbContext.CasosAnalisis.Add(caso);
        await dbContext.SaveChangesAsync();

        return (dbContext, caso);
    }

    [Fact]
    public async Task Cierra_el_caso_con_resultado_Positivo()
    {
        var (dbContext, caso) = await PrepararCasoAsync();
        var handler = new CerrarCasoCommandHandler(dbContext);

        await handler.Handle(new CerrarCasoCommand(caso.Id, ResultadoCaso.Positivo), CancellationToken.None);

        var actualizado = await dbContext.CasosAnalisis.FindAsync(caso.Id);
        Assert.Equal(EstadoCaso.Cerrado, actualizado!.Estado);
        Assert.Equal(ResultadoCaso.Positivo, actualizado.Resultado);
    }

    [Fact]
    public async Task Resultado_Revision_marca_el_caso_En_Revision_en_vez_de_Cerrado()
    {
        var (dbContext, caso) = await PrepararCasoAsync();
        var handler = new CerrarCasoCommandHandler(dbContext);

        await handler.Handle(new CerrarCasoCommand(caso.Id, ResultadoCaso.Revision), CancellationToken.None);

        var actualizado = await dbContext.CasosAnalisis.FindAsync(caso.Id);
        Assert.Equal(EstadoCaso.EnRevision, actualizado!.Estado);
    }

    [Fact]
    public async Task Rechaza_un_caso_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CerrarCasoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() =>
            handler.Handle(new CerrarCasoCommand(Guid.NewGuid(), ResultadoCaso.Positivo), CancellationToken.None));
    }
}
