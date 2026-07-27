using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Commands.CambiarRolUsuario;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

public class CambiarRolUsuarioCommandHandlerTests
{
    [Fact]
    public async Task CambiarRol_UsuarioDistintoAlActual_DebeCambiarElRolCorrectamente()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);

        var handler = new CambiarRolUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await handler.Handle(new CambiarRolUsuarioCommand(usuarioId, Roles.Supervisor), CancellationToken.None);

        var usuarios = await userManagementService.ListarUsuariosAsync(CancellationToken.None);
        var usuarioActualizado = Assert.Single(usuarios);
        Assert.Equal(Roles.Supervisor, usuarioActualizado.Rol);
    }

    [Fact]
    public async Task CambiarRol_UsuarioInexistente_DebeRechazarConEntidadNoEncontrada()
    {
        var userManagementService = new FakeUserManagementService();
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);

        var handler = new CambiarRolUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new CambiarRolUsuarioCommand(Guid.NewGuid(), Roles.Supervisor), CancellationToken.None));
    }

    [Fact]
    public async Task CambiarRol_AdministradorIntentaCambiarseSuPropioRol_DebeRechazarLaOperacion()
    {
        var userManagementService = new FakeUserManagementService();
        var adminId = userManagementService.AgregarUsuarioExistente("Admin Principal", "admin@institucion.gob", Roles.Admin);
        var currentUserService = new FakeCurrentUserService(adminId, Roles.Admin);

        var handler = new CambiarRolUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new CambiarRolUsuarioCommand(adminId, Roles.Supervisor), CancellationToken.None));
    }
}
