using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Commands.CrearUsuario;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

public class CrearUsuarioCommandHandlerTests
{
    [Fact]
    public async Task CrearUsuario_AltaValida_DevuelveElIdDelUsuarioCreado()
    {
        var userManagementService = new FakeUserManagementService();
        var handler = new CrearUsuarioCommandHandler(userManagementService, new FakeAuditLogger());

        var usuarioId = await handler.Handle(
            new CrearUsuarioCommand("Ana Gómez", "ana.gomez@institucion.gob", "unaContraseñaBienLarga123", Roles.Analista),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, usuarioId);

        var usuarios = await userManagementService.ListarUsuariosAsync(CancellationToken.None);
        var usuarioCreado = Assert.Single(usuarios);
        Assert.Equal(usuarioId, usuarioCreado.Id);
        Assert.Equal("Ana Gómez", usuarioCreado.NombreCompleto);
        Assert.Equal("ana.gomez@institucion.gob", usuarioCreado.Email);
        Assert.Equal(Roles.Analista, usuarioCreado.Rol);
        Assert.False(usuarioCreado.Bloqueado);
    }

    [Fact]
    public async Task CrearUsuario_AltaValida_RegistraElEventoEnAuditLog()
    {
        var userManagementService = new FakeUserManagementService();
        var auditLogger = new FakeAuditLogger();
        var handler = new CrearUsuarioCommandHandler(userManagementService, auditLogger);

        var usuarioId = await handler.Handle(
            new CrearUsuarioCommand("Ana Gómez", "ana.gomez@institucion.gob", "unaContraseñaBienLarga123", Roles.Analista),
            CancellationToken.None);

        var registro = Assert.Single(auditLogger.Registros);
        Assert.Equal("CrearUsuario", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuarioId, registro.EntidadId);
    }

    [Fact]
    public async Task CrearUsuario_EmailDuplicado_DebeRechazarElAlta()
    {
        var userManagementService = new FakeUserManagementService();
        userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);

        var handler = new CrearUsuarioCommandHandler(userManagementService, new FakeAuditLogger());

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearUsuarioCommand("Otra Persona", "ana.gomez@institucion.gob", "unaContraseñaBienLarga123", Roles.Analista),
            CancellationToken.None));
    }

    [Fact]
    public async Task CrearUsuario_ContraseñaQueNoCumpleLaPoliticaMinima_DebeRechazarElAlta()
    {
        var userManagementService = new FakeUserManagementService();
        var handler = new CrearUsuarioCommandHandler(userManagementService, new FakeAuditLogger());

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new CrearUsuarioCommand("Ana Gómez", "ana.gomez@institucion.gob", "corta123", Roles.Analista),
            CancellationToken.None));
    }
}
