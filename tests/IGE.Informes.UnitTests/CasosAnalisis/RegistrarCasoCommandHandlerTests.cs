using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.CasosAnalisis.Commands.RegistrarCaso;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CasosAnalisis;

public class RegistrarCasoCommandHandlerTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static async Task<(TestAppDbContext DbContext, Dependencia Dependencia, TipoIncidente TipoIncidente)> PrepararCatalogosAsync()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var tipoIncidente = new TipoIncidente("164", "ROBO");

        dbContext.Dependencias.Add(dependencia);
        dbContext.TiposIncidente.Add(tipoIncidente);
        await dbContext.SaveChangesAsync();

        return (dbContext, dependencia, tipoIncidente);
    }

    [Fact]
    public async Task Crea_el_caso_en_Pendiente_y_asigna_al_usuario_actual_como_Creador()
    {
        var (dbContext, dependencia, tipoIncidente) = await PrepararCatalogosAsync();
        var handler = new RegistrarCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var casoId = await handler.Handle(
            new RegistrarCasoCommand(new DateOnly(2026, 7, 21), dependencia.Id, tipoIncidente.Id, "911-1234", "Observación breve"),
            CancellationToken.None);

        var caso = await dbContext.CasosAnalisis.FindAsync(casoId);
        Assert.NotNull(caso);
        Assert.Equal(EstadoCaso.Pendiente, caso.Estado);
        Assert.Equal(UsuarioId, caso.Analistas.Single().UsuarioId);
        Assert.Equal(RolCasoAnalista.Creador, caso.Analistas.Single().Rol);
    }

    [Fact]
    public async Task Acepta_el_alta_sin_numero_de_llamado_911()
    {
        var (dbContext, dependencia, tipoIncidente) = await PrepararCatalogosAsync();
        var handler = new RegistrarCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var casoId = await handler.Handle(
            new RegistrarCasoCommand(new DateOnly(2026, 7, 21), dependencia.Id, tipoIncidente.Id, null, null),
            CancellationToken.None);

        var caso = await dbContext.CasosAnalisis.FindAsync(casoId);
        Assert.NotNull(caso);
        Assert.Null(caso.NroLlamado911);
    }

    [Fact]
    public async Task Rechaza_una_Dependencia_inexistente()
    {
        var (dbContext, _, tipoIncidente) = await PrepararCatalogosAsync();
        var handler = new RegistrarCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new RegistrarCasoCommand(new DateOnly(2026, 7, 21), Guid.NewGuid(), tipoIncidente.Id, null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_un_TipoIncidente_inexistente()
    {
        var (dbContext, dependencia, _) = await PrepararCatalogosAsync();
        var handler = new RegistrarCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new RegistrarCasoCommand(new DateOnly(2026, 7, 21), dependencia.Id, Guid.NewGuid(), null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_ejecutar_sin_usuario_autenticado()
    {
        var (dbContext, dependencia, tipoIncidente) = await PrepararCatalogosAsync();
        var handler = new RegistrarCasoCommandHandler(dbContext, new FakeCurrentUserService(usuarioId: null));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new RegistrarCasoCommand(new DateOnly(2026, 7, 21), dependencia.Id, tipoIncidente.Id, null, null),
            CancellationToken.None));
    }
}
