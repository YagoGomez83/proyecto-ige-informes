using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Commands.ResetearPasswordUsuario;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

/// <summary>
/// Tests de la extensión de HU-17 "Reseteo de contraseña por un Admin"
/// (ver docs/epic-04-gestion-catalogos.md). El Handler todavía no existe:
/// estos tests se escriben antes de la implementación (TDD), siguiendo la
/// misma regla que ya aplica el proyecto para el resto de HU-17
/// (CambiarRolUsuarioCommandHandlerTests, BloquearUsuarioCommandHandlerTests).
/// </summary>
public class ResetearPasswordUsuarioCommandHandlerTests
{
    [Fact]
    public async Task ResetearPassword_UsuarioExistenteDistintoAlActual_DebePermitirIniciarSesionConLaContraseñaNueva()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);

        var handler = new ResetearPasswordUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await handler.Handle(new ResetearPasswordUsuarioCommand(usuarioId, "ContraseñaNueva123"), CancellationToken.None);

        var llamada = Assert.Single(userManagementService.ResetearPasswordAsyncLlamadoCon);
        Assert.Equal(usuarioId, llamada.UsuarioId);
        Assert.Equal("ContraseñaNueva123", llamada.NuevaPassword);
    }

    [Fact]
    public async Task ResetearPassword_ContraseñaNoCumpleLaPoliticaMinima_DebeRechazarConReglaDeNegocioViolada()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);

        var handler = new ResetearPasswordUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new ResetearPasswordUsuarioCommand(usuarioId, "corta"), CancellationToken.None));
    }

    [Fact]
    public async Task ResetearPassword_UsuarioInexistente_DebeRechazarConEntidadNoEncontrada()
    {
        var userManagementService = new FakeUserManagementService();
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);

        var handler = new ResetearPasswordUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new ResetearPasswordUsuarioCommand(Guid.NewGuid(), "ContraseñaNueva123"), CancellationToken.None));
    }

    [Fact]
    public async Task ResetearPassword_AdministradorIntentaResetearSuPropiaContraseña_DebeRechazarLaOperacion()
    {
        var userManagementService = new FakeUserManagementService();
        var adminId = userManagementService.AgregarUsuarioExistente("Admin Principal", "admin@institucion.gob", Roles.Admin);
        var currentUserService = new FakeCurrentUserService(adminId, Roles.Admin);

        var handler = new ResetearPasswordUsuarioCommandHandler(userManagementService, currentUserService, new FakeAuditLogger());

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new ResetearPasswordUsuarioCommand(adminId, "ContraseñaNueva123"), CancellationToken.None));
    }

    [Fact]
    public async Task ResetearPassword_ReseteoValido_RegistraElEventoEnAuditLog()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid(), Roles.Admin);
        var auditLogger = new FakeAuditLogger();

        var handler = new ResetearPasswordUsuarioCommandHandler(userManagementService, currentUserService, auditLogger);

        await handler.Handle(new ResetearPasswordUsuarioCommand(usuarioId, "ContraseñaNueva123"), CancellationToken.None);

        var registro = Assert.Single(auditLogger.Registros);
        Assert.Equal("ResetearPasswordUsuario", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuarioId, registro.EntidadId);
    }
}
