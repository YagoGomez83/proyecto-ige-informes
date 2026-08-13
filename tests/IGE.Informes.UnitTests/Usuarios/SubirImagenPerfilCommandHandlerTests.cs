using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Commands.SubirImagenPerfil;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

public class SubirImagenPerfilCommandHandlerTests
{
    [Fact]
    public async Task Sube_la_imagen_y_la_asocia_al_usuario_autenticado()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var fileStorage = new FakeFileStorage();
        var auditLogger = new FakeAuditLogger();

        var handler = new SubirImagenPerfilCommandHandler(
            userManagementService, new FakeCurrentUserService(usuarioId), fileStorage, new FakeAntivirusScanner(), auditLogger);

        await handler.Handle(new SubirImagenPerfilCommand([1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None);

        Assert.Single(fileStorage.ArchivosSubidos);
        var perfil = await userManagementService.ObtenerPerfilPropioAsync(usuarioId, CancellationToken.None);
        Assert.Equal(fileStorage.ArchivosSubidos[0], perfil!.ImagenPerfilPath);
    }

    [Fact]
    public async Task Reemplaza_la_imagen_anterior_y_la_elimina_de_storage()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var fileStorage = new FakeFileStorage();

        var handler = new SubirImagenPerfilCommandHandler(
            userManagementService, new FakeCurrentUserService(usuarioId), fileStorage, new FakeAntivirusScanner(), new FakeAuditLogger());

        await handler.Handle(new SubirImagenPerfilCommand([1, 2, 3], "foto1.jpg", "image/jpeg"), CancellationToken.None);
        var pathAnterior = (await userManagementService.ObtenerPerfilPropioAsync(usuarioId, CancellationToken.None))!.ImagenPerfilPath;

        await handler.Handle(new SubirImagenPerfilCommand([4, 5, 6], "foto2.jpg", "image/jpeg"), CancellationToken.None);

        Assert.Equal(2, fileStorage.ArchivosSubidos.Count);
        Assert.Contains(pathAnterior!, fileStorage.ArchivosEliminados);

        var perfil = await userManagementService.ObtenerPerfilPropioAsync(usuarioId, CancellationToken.None);
        Assert.NotEqual(pathAnterior, perfil!.ImagenPerfilPath);
    }

    [Fact]
    public async Task Rechaza_una_imagen_detectada_como_amenaza_y_no_la_sube()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var fileStorage = new FakeFileStorage();
        var antivirus = new FakeAntivirusScanner { ResultadoLimpio = false };

        var handler = new SubirImagenPerfilCommandHandler(
            userManagementService, new FakeCurrentUserService(usuarioId), fileStorage, antivirus, new FakeAuditLogger());

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new SubirImagenPerfilCommand([1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None));

        Assert.Empty(fileStorage.ArchivosSubidos);
    }

    [Fact]
    public async Task Propaga_la_excepcion_si_el_antivirus_no_esta_disponible()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var fileStorage = new FakeFileStorage();
        var antivirus = new FakeAntivirusScanner { LanzarNoDisponible = true };

        var handler = new SubirImagenPerfilCommandHandler(
            userManagementService, new FakeCurrentUserService(usuarioId), fileStorage, antivirus, new FakeAuditLogger());

        await Assert.ThrowsAsync<AntivirusNoDisponibleException>(() => handler.Handle(
            new SubirImagenPerfilCommand([1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None));

        Assert.Empty(fileStorage.ArchivosSubidos);
    }

    [Fact]
    public async Task Registra_el_evento_en_AuditLog()
    {
        var userManagementService = new FakeUserManagementService();
        var usuarioId = userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        var auditLogger = new FakeAuditLogger();

        var handler = new SubirImagenPerfilCommandHandler(
            userManagementService, new FakeCurrentUserService(usuarioId), new FakeFileStorage(), new FakeAntivirusScanner(), auditLogger);

        await handler.Handle(new SubirImagenPerfilCommand([1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None);

        var registro = Assert.Single(auditLogger.Registros);
        Assert.Equal("SubirImagenPerfil", registro.Accion);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(usuarioId, registro.EntidadId);
    }
}
