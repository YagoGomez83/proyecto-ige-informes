using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Commands.BloquearUsuario;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

public class BloquearUsuarioCommandHandlerTests
{
    [Fact]
    public async Task BloquearUsuario_UsuarioDistintoAlActual_DebeBloquearloCorrectamente()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);

        var handler = new BloquearUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await handler.Handle(new BloquearUsuarioCommand(usuarioId), CancellationToken.None);

        var usuarios = await userManagementService.ListarUsuariosAsync(CancellationToken.None);
        var usuarioActualizado = Assert.Single(usuarios);
        Assert.True(usuarioActualizado.Bloqueado);
    }

    [Fact]
    public async Task BloquearUsuario_UsuarioInexistente_DebeRechazarConEntidadNoEncontrada()
    {
        var userManagementService = new FakeUserManagementService();
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);

        var handler = new BloquearUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new BloquearUsuarioCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task BloquearUsuario_AdministradorIntentaBloquearseASiMismo_DebeRechazarLaOperacion()
    {
        var userManagementService = new FakeUserManagementService();
        var adminId = userManagementService.AgregarUsuarioExistente("Admin Principal", "admin@institucion.gob", Roles.Admin);
        var currentUserService = new FakeCurrentUserService(adminId, Roles.Admin);

        var handler = new BloquearUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new BloquearUsuarioCommand(adminId), CancellationToken.None));
    }

    [Fact]
    public async Task BloquearUsuario_BloqueoValido_RegistraElEventoEnAuditLog()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);
        var auditLogger = new FakeAuditLogger();

        var handler = new BloquearUsuarioCommandHandler(userManagementService, currentUserService, auditLogger);

        await handler.Handle(new BloquearUsuarioCommand(usuarioId), CancellationToken.None);

        var registro = Assert.Single(auditLogger.Registros);
        Assert.Equal("BloquearUsuario", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuarioId, registro.EntidadId);
    }
}
