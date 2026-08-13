using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Commands.CambiarPasswordPropia;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

public class CambiarPasswordPropiaCommandHandlerTests
{
    private const string PasswordOriginal = "PasswordOriginal123";

    [Fact]
    public async Task CambiarPasswordPropia_ContraseñaActualCorrecta_DebeActualizarla()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(usuarioId, Roles.Analista);

        var handler = new CambiarPasswordPropiaCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await handler.Handle(new CambiarPasswordPropiaCommand(PasswordOriginal, "ContraseñaNueva123"), CancellationToken.None);

        var llamada = Assert.Single(userManagementService.CambiarPasswordPropiaAsyncLlamadoCon);
        Assert.Equal(usuarioId, llamada.UsuarioId);
        Assert.Equal(PasswordOriginal, llamada.PasswordActual);
        Assert.Equal("ContraseñaNueva123", llamada.PasswordNueva);
    }

    [Fact]
    public async Task CambiarPasswordPropia_ContraseñaActualIncorrecta_DebeRechazarConReglaDeNegocioViolada()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(usuarioId, Roles.Analista);

        var handler = new CambiarPasswordPropiaCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new CambiarPasswordPropiaCommand("ContraseñaEquivocada123", "ContraseñaNueva123"), CancellationToken.None));
    }

    [Fact]
    public async Task CambiarPasswordPropia_ContraseñaNuevaNoCumpleLaPoliticaMinima_DebeRechazarConReglaDeNegocioViolada()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(usuarioId, Roles.Analista);

        var handler = new CambiarPasswordPropiaCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new CambiarPasswordPropiaCommand(PasswordOriginal, "corta"), CancellationToken.None));
    }

    [Fact]
    public async Task CambiarPasswordPropia_FuncionaParaLosTresRoles_AdminIncluido()
    {
        var userManagementService = new FakeUserManagementService();
        var adminId = userManagementService.AgregarUsuarioExistente("Admin Principal", "admin@institucion.gob", Roles.Admin);
        var currentUserService = new FakeCurrentUserService(adminId, Roles.Admin);

        var handler = new CambiarPasswordPropiaCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await handler.Handle(new CambiarPasswordPropiaCommand(PasswordOriginal, "ContraseñaNueva123"), CancellationToken.None);

        var llamada = Assert.Single(userManagementService.CambiarPasswordPropiaAsyncLlamadoCon);
        Assert.Equal(adminId, llamada.UsuarioId);
    }

    [Fact]
    public async Task CambiarPasswordPropia_CambioValido_RegistraElEventoEnAuditLog()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(usuarioId, Roles.Analista);
        var auditLogger = new FakeAuditLogger();

        var handler = new CambiarPasswordPropiaCommandHandler(userManagementService, currentUserService, auditLogger);

        await handler.Handle(new CambiarPasswordPropiaCommand(PasswordOriginal, "ContraseñaNueva123"), CancellationToken.None);

        var registro = Assert.Single(auditLogger.Registros);
        Assert.Equal("CambiarPasswordPropia", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuarioId, registro.EntidadId);
    }
}
