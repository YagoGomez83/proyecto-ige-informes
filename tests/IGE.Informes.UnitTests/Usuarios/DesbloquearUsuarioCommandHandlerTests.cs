using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Commands.DesbloquearUsuario;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

public class DesbloquearUsuarioCommandHandlerTests
{
    [Fact]
    public async Task DesbloquearUsuario_UsuarioExistenteBloqueado_DebeDesbloquearloCorrectamente()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente(
            "Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista, bloqueado: true);

        var handler = new DesbloquearUsuarioCommandHandler(userManagementService, new FakeAuditLogger());

        await handler.Handle(new DesbloquearUsuarioCommand(usuarioId), CancellationToken.None);

        var usuarios = await userManagementService.ListarUsuariosAsync(CancellationToken.None);
        var usuarioActualizado = Assert.Single(usuarios);
        Assert.False(usuarioActualizado.Bloqueado);
    }

    [Fact]
    public async Task DesbloquearUsuario_UsuarioInexistente_DebeRechazarConEntidadNoEncontrada()
    {
        var userManagementService = new FakeUserManagementService();

        var handler = new DesbloquearUsuarioCommandHandler(userManagementService, new FakeAuditLogger());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new DesbloquearUsuarioCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task DesbloquearUsuario_ElPropioUsuarioLogueado_DebePermitirloSinRestriccion()
    {
        // La HU-17 solo restringe la auto-edición para cambio de rol y
        // bloqueo, no para el desbloqueo — no hay ICurrentUserService
        // involucrado en este Handler.
        var userManagementService = new FakeUserManagementService();
        var adminId = userManagementService.AgregarUsuarioExistente(
            "Admin Principal", "admin@institucion.gob", Roles.Admin, bloqueado: true);

        var handler = new DesbloquearUsuarioCommandHandler(userManagementService, new FakeAuditLogger());

        await handler.Handle(new DesbloquearUsuarioCommand(adminId), CancellationToken.None);

        var usuarios = await userManagementService.ListarUsuariosAsync(CancellationToken.None);
        var usuarioActualizado = Assert.Single(usuarios);
        Assert.False(usuarioActualizado.Bloqueado);
    }
}
