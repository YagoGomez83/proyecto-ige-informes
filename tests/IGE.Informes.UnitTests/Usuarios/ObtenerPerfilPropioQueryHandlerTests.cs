using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Queries.ObtenerPerfilPropio;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

public class ObtenerPerfilPropioQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_nombre_email_y_rol_del_usuario_autenticado()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Supervisor);

        var handler = new ObtenerPerfilPropioQueryHandler(
            userManagementService, new FakeCurrentUserService(usuarioId), new FakeFileStorage(), new FakeAuditLogger());

        var perfil = await handler.Handle(new ObtenerPerfilPropioQuery(), CancellationToken.None);

        Assert.Equal("Ana Gómez", perfil.NombreCompleto);
        Assert.Equal("ana.gomez@institucion.gob", perfil.Email);
        Assert.Equal(Roles.Supervisor, perfil.Rol);
        Assert.Null(perfil.ImagenPerfilUrl);
    }

    [Fact]
    public async Task Resuelve_la_URL_de_descarga_cuando_hay_imagen_de_perfil()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        await userManagementService.ActualizarImagenPerfilAsync(usuarioId, "fake/foto.jpg", CancellationToken.None);

        var handler = new ObtenerPerfilPropioQueryHandler(
            userManagementService, new FakeCurrentUserService(usuarioId), new FakeFileStorage(), new FakeAuditLogger());

        var perfil = await handler.Handle(new ObtenerPerfilPropioQuery(), CancellationToken.None);

        Assert.Equal("https://fake-storage.local/fake/foto.jpg", perfil.ImagenPerfilUrl);
    }

    [Fact]
    public async Task Rechaza_si_no_hay_usuario_autenticado()
    {
        var userManagementService = new FakeUserManagementService();
        var handler = new ObtenerPerfilPropioQueryHandler(
            userManagementService, new FakeCurrentUserService(null), new FakeFileStorage(), new FakeAuditLogger());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(new ObtenerPerfilPropioQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Registra_el_evento_en_AuditLog()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var auditLogger = new FakeAuditLogger();

        var handler = new ObtenerPerfilPropioQueryHandler(
            userManagementService, new FakeCurrentUserService(usuarioId), new FakeFileStorage(), auditLogger);

        await handler.Handle(new ObtenerPerfilPropioQuery(), CancellationToken.None);

        var registro = Assert.Single(auditLogger.Registros);
        Assert.Equal("Lectura", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuarioId, registro.EntidadId);
    }
}
