using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Usuarios.Queries.ListarUsuarios;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Usuarios;

public class ListarUsuariosQueryHandlerTests
{
    [Fact]
    public async Task ListarUsuarios_VariosUsuariosConDistintosRolesYEstados_DevuelveNombreEmailRolYBloqueadoDeCadaUno()
    {
        var userManagementService = new FakeUserManagementService();
        userManagementService.AgregarUsuarioExistente("Ana Gómez", "ana.gomez@institucion.gob", Roles.Analista);
        userManagementService.AgregarUsuarioExistente("Beto Ruiz", "beto.ruiz@institucion.gob", Roles.Supervisor, bloqueado: true);
        userManagementService.AgregarUsuarioExistente("Carla Díaz", "carla.diaz@institucion.gob", Roles.Admin);

        var handler = new ListarUsuariosQueryHandler(userManagementService);

        var usuarios = await handler.Handle(new ListarUsuariosQuery(), CancellationToken.None);

        Assert.Equal(3, usuarios.Count);

        var ana = Assert.Single(usuarios, u => u.Email == "ana.gomez@institucion.gob");
        Assert.Equal("Ana Gómez", ana.NombreCompleto);
        Assert.Equal(Roles.Analista, ana.Rol);
        Assert.False(ana.Bloqueado);

        var beto = Assert.Single(usuarios, u => u.Email == "beto.ruiz@institucion.gob");
        Assert.Equal("Beto Ruiz", beto.NombreCompleto);
        Assert.Equal(Roles.Supervisor, beto.Rol);
        Assert.True(beto.Bloqueado);

        var carla = Assert.Single(usuarios, u => u.Email == "carla.diaz@institucion.gob");
        Assert.Equal("Carla Díaz", carla.NombreCompleto);
        Assert.Equal(Roles.Admin, carla.Rol);
        Assert.False(carla.Bloqueado);
    }
}
