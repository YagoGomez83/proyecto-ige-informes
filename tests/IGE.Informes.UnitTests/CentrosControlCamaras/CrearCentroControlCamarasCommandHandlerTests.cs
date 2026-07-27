using IGE.Informes.Application.CentrosControlCamaras.Commands.CrearCentroControlCamaras;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CentrosControlCamaras;

public class CrearCentroControlCamarasCommandHandlerTests
{
    [Fact]
    public async Task Registra_un_centro_de_control_nuevo()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearCentroControlCamarasCommandHandler(dbContext);

        var centroId = await handler.Handle(
            new CrearCentroControlCamarasCommand("CCCSL", "Centro de Control de Cámaras San Luis"), CancellationToken.None);

        var centro = await dbContext.CentrosControlCamaras.FindAsync(centroId);
        Assert.Equal("CCCSL", centro!.Sigla);
        Assert.Equal("Centro de Control de Cámaras San Luis", centro.Nombre);
    }

    [Fact]
    public async Task Rechaza_una_sigla_duplicada()
    {
        var dbContext = new TestAppDbContext();
        dbContext.CentrosControlCamaras.Add(new CentroControlCamaras("CCCSL", "Centro de Control de Cámaras San Luis"));
        await dbContext.SaveChangesAsync();

        var handler = new CrearCentroControlCamarasCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearCentroControlCamarasCommand("CCCSL", "Otro nombre"), CancellationToken.None));
    }
}
