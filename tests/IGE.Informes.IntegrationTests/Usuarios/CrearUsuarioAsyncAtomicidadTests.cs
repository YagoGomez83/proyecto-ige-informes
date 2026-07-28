using IGE.Informes.Application.Common.Security;
using IGE.Informes.Infrastructure.Identity;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Usuarios;

public class CrearUsuarioAsyncAtomicidadTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task CrearUsuario_ConRolInexistente_HaceRollback_YNoQuedaUsuarioHuerfanoSinRol()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        // Deliberadamente no se crea ningún ApplicationRole, para que
        // AddToRoleAsync falle dentro de CrearUsuarioAsync.
        await using (var stack = new IdentityStack(options))
        {
            var service = new UserManagementService(stack.UserManager, stack.DbContext);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CrearUsuarioAsync(
                    "Ana Gómez", "ana.gomez@institucion.gob", "ContraseñaSegura123!", Roles.Analista, CancellationToken.None));
        }

        await using var assertStack = new IdentityStack(options);
        var usuarioTrasFallo = await assertStack.UserManager.FindByEmailAsync("ana.gomez@institucion.gob");

        Assert.Null(usuarioTrasFallo);
    }

    [Fact]
    public async Task CrearUsuario_CaminoFeliz_CreaElUsuarioConSuRol()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid? usuarioId;
        await using (var stack = new IdentityStack(options))
        {
            await stack.RoleManager.CreateAsync(new ApplicationRole(Roles.Analista));

            var service = new UserManagementService(stack.UserManager, stack.DbContext);
            usuarioId = await service.CrearUsuarioAsync(
                "Ana Gómez", "ana.gomez@institucion.gob", "ContraseñaSegura123!", Roles.Analista, CancellationToken.None);
        }

        Assert.NotNull(usuarioId);

        await using var assertStack = new IdentityStack(options);
        var usuarioCreado = await assertStack.UserManager.FindByIdAsync(usuarioId!.Value.ToString());
        var roles = await assertStack.UserManager.GetRolesAsync(usuarioCreado!);

        Assert.NotNull(usuarioCreado);
        Assert.Single(roles);
        Assert.Equal(Roles.Analista, roles[0]);
    }
}
