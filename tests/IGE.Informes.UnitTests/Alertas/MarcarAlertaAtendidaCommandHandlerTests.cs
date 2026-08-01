using IGE.Informes.Application.Alertas.Commands.MarcarAlertaAtendida;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Alertas;

public class MarcarAlertaAtendidaCommandHandlerTests
{
    [Fact]
    public async Task Marca_la_alerta_como_atendida_por_el_usuario_actual()
    {
        var dbContext = new TestAppDbContext();
        var alerta = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, Guid.NewGuid());
        dbContext.Alertas.Add(alerta);
        await dbContext.SaveChangesAsync();

        var usuarioId = Guid.NewGuid();
        var handler = new MarcarAlertaAtendidaCommandHandler(dbContext, new FakeCurrentUserService(usuarioId));

        await handler.Handle(new MarcarAlertaAtendidaCommand(alerta.Id), CancellationToken.None);

        var actualizada = await dbContext.Alertas.FindAsync(alerta.Id);
        Assert.True(actualizada!.Atendida);
        Assert.Equal(usuarioId, actualizada.AtendidaPorUsuarioId);
    }

    [Fact]
    public async Task Rechaza_una_alerta_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new MarcarAlertaAtendidaCommandHandler(dbContext, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new MarcarAlertaAtendidaCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
