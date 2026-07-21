using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.CasosAnalisis.Commands.VincularVehiculoTexto;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CasosAnalisis;

public class VincularVehiculoTextoCommandHandlerTests
{
    [Fact]
    public async Task Guarda_la_descripcion_libre_del_vehiculo_sin_ficha_de_catalogo()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        dbContext.CasosAnalisis.Add(caso);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoTextoCommandHandler(dbContext);

        await handler.Handle(
            new VincularVehiculoTextoCommand(caso.Id, "Sedán oscuro, dominio incierto"),
            CancellationToken.None);

        var actualizado = await dbContext.CasosAnalisis.FindAsync(caso.Id);
        Assert.Equal("Sedán oscuro, dominio incierto", actualizado!.VehiculoInvolucradoTexto);
    }

    [Fact]
    public async Task Rechaza_un_caso_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new VincularVehiculoTextoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new VincularVehiculoTextoCommand(Guid.NewGuid(), "algo"),
            CancellationToken.None));
    }
}
