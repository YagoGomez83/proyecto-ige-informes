using IGE.Informes.Application.Common.Security;
using IGE.Informes.Infrastructure.Identity;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Usuarios;

public class CambiarRolAsyncAtomicidadTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task CambiarRol_ConRolNuevoInexistente_HaceRollback_YElUsuarioConservaSuRolOriginal()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid usuarioId;
        await using (var stack = new IdentityStack(options))
        {
            await stack.RoleManager.CreateAsync(new ApplicationRole(Roles.Analista));
            // Roles.Supervisor deliberadamente NO se crea, para que
            // AddToRoleAsync falle dentro de CambiarRolAsync.

            var usuario = new ApplicationUser
            {
                UserName = "ana.gomez@institucion.gob",
                Email = "ana.gomez@institucion.gob",
                EmailConfirmed = true,
                NombreCompleto = "Ana Gómez",
            };
            await stack.UserManager.CreateAsync(usuario, "ContraseñaSegura123!");
            await stack.UserManager.AddToRoleAsync(usuario, Roles.Analista);
            usuarioId = usuario.Id;

            var service = new UserManagementService(stack.UserManager, stack.DbContext);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CambiarRolAsync(usuarioId, Roles.Supervisor, CancellationToken.None));
        }

        await using var assertStack = new IdentityStack(options);
        var usuarioTrasFallo = await assertStack.UserManager.FindByIdAsync(usuarioId.ToString());
        var rolesTrasFallo = await assertStack.UserManager.GetRolesAsync(usuarioTrasFallo!);

        Assert.Single(rolesTrasFallo);
        Assert.Equal(Roles.Analista, rolesTrasFallo[0]);
    }

    [Fact]
    public async Task CambiarRol_CaminoFeliz_CambiaElRolYElSecurityStamp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid usuarioId;
        string? stampOriginal;
        await using (var stack = new IdentityStack(options))
        {
            await stack.RoleManager.CreateAsync(new ApplicationRole(Roles.Analista));
            await stack.RoleManager.CreateAsync(new ApplicationRole(Roles.Supervisor));

            var usuario = new ApplicationUser
            {
                UserName = "ana.gomez@institucion.gob",
                Email = "ana.gomez@institucion.gob",
                EmailConfirmed = true,
                NombreCompleto = "Ana Gómez",
            };
            await stack.UserManager.CreateAsync(usuario, "ContraseñaSegura123!");
            await stack.UserManager.AddToRoleAsync(usuario, Roles.Analista);
            usuarioId = usuario.Id;
            stampOriginal = await stack.UserManager.GetSecurityStampAsync(usuario);

            var service = new UserManagementService(stack.UserManager, stack.DbContext);
            await service.CambiarRolAsync(usuarioId, Roles.Supervisor, CancellationToken.None);
        }

        await using var assertStack2 = new IdentityStack(options);
        var usuarioActualizado = await assertStack2.UserManager.FindByIdAsync(usuarioId.ToString());
        var rolesActualizados = await assertStack2.UserManager.GetRolesAsync(usuarioActualizado!);
        var stampActualizado = await assertStack2.UserManager.GetSecurityStampAsync(usuarioActualizado!);

        Assert.Single(rolesActualizados);
        Assert.Equal(Roles.Supervisor, rolesActualizados[0]);
        Assert.NotEqual(stampOriginal, stampActualizado);
    }
}
