using IGE.Informes.Application.Camaras.Commands.RegistrarCamara;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class RegistrarCamaraCommandHandlerTests
{
    [Fact]
    public async Task Registra_una_camara_nueva()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarCamaraCommandHandler(dbContext);

        var camaraId = await handler.Handle(
            new RegistrarCamaraCommand("SL 18", TipoCamara.Domo, "Av. Illia y San Martín"),
            CancellationToken.None);

        var camara = await dbContext.Camaras.FindAsync(camaraId);
        Assert.Equal("SL 18", camara!.Codigo);
    }

    [Fact]
    public async Task Rechaza_un_codigo_duplicado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Camaras.Add(new Camara("SL 18", TipoCamara.Domo));
        await dbContext.SaveChangesAsync();

        var handler = new RegistrarCamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new RegistrarCamaraCommand("SL 18", TipoCamara.Lpr, null), CancellationToken.None));
    }

    [Fact]
    public async Task Registra_una_camara_con_dependencia()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var handler = new RegistrarCamaraCommandHandler(dbContext);

        var camaraId = await handler.Handle(
            new RegistrarCamaraCommand("SL 18", TipoCamara.Domo, null, dependencia.Id),
            CancellationToken.None);

        var camara = await dbContext.Camaras.FindAsync(camaraId);
        Assert.Equal(dependencia.Id, camara!.DependenciaId);
    }

    [Fact]
    public async Task Registra_una_camara_sin_dependencia_para_lpr_en_ruta()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarCamaraCommandHandler(dbContext);

        var camaraId = await handler.Handle(
            new RegistrarCamaraCommand("LP 217", TipoCamara.Lpr, "Ruta 20 km 5"),
            CancellationToken.None);

        var camara = await dbContext.Camaras.FindAsync(camaraId);
        Assert.Null(camara!.DependenciaId);
    }

    [Fact]
    public async Task Rechaza_una_dependencia_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarCamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new RegistrarCamaraCommand("SL 18", TipoCamara.Domo, null, Guid.NewGuid()), CancellationToken.None));
    }
}
