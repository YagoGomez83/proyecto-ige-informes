using IGE.Informes.Application.Common.Security;
using IGE.Informes.Infrastructure.Identity;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Usuarios;

public class ResetearPasswordAsyncTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task ResetearPassword_CaminoFeliz_PermiteLoginConLaNuevaYNoConLaVieja()
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

            var usuario = new ApplicationUser
            {
                UserName = "ana.gomez@institucion.gob",
                Email = "ana.gomez@institucion.gob",
                EmailConfirmed = true,
                NombreCompleto = "Ana Gómez",
            };
            await stack.UserManager.CreateAsync(usuario, "ContraseñaVieja123");
            await stack.UserManager.AddToRoleAsync(usuario, Roles.Analista);
            usuarioId = usuario.Id;
            stampOriginal = await stack.UserManager.GetSecurityStampAsync(usuario);

            var service = new UserManagementService(stack.UserManager, stack.DbContext);
            var exito = await service.ResetearPasswordAsync(usuarioId, "ContraseñaNueva456", CancellationToken.None);

            Assert.True(exito);
        }

        await using var assertStack = new IdentityStack(options);
        var usuario2 = await assertStack.UserManager.FindByIdAsync(usuarioId.ToString());

        Assert.False(await assertStack.UserManager.CheckPasswordAsync(usuario2!, "ContraseñaVieja123"));
        Assert.True(await assertStack.UserManager.CheckPasswordAsync(usuario2!, "ContraseñaNueva456"));

        var stampActualizado = await assertStack.UserManager.GetSecurityStampAsync(usuario2!);
        Assert.NotEqual(stampOriginal, stampActualizado);
    }

    [Fact]
    public async Task ResetearPassword_ConPoliticaMinimaIncumplida_DevuelveFalseYNoInvalidaElStamp()
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
        bool exito;
        await using (var stack = new IdentityStack(options))
        {
            await stack.RoleManager.CreateAsync(new ApplicationRole(Roles.Analista));

            var usuario = new ApplicationUser
            {
                UserName = "ana.gomez@institucion.gob",
                Email = "ana.gomez@institucion.gob",
                EmailConfirmed = true,
                NombreCompleto = "Ana Gómez",
            };
            await stack.UserManager.CreateAsync(usuario, "ContraseñaVieja123");
            await stack.UserManager.AddToRoleAsync(usuario, Roles.Analista);
            usuarioId = usuario.Id;
            stampOriginal = await stack.UserManager.GetSecurityStampAsync(usuario);

            var service = new UserManagementService(stack.UserManager, stack.DbContext);
            exito = await service.ResetearPasswordAsync(usuarioId, "corta", CancellationToken.None);
        }

        Assert.False(exito);

        await using var assertStack = new IdentityStack(options);
        var usuario2 = await assertStack.UserManager.FindByIdAsync(usuarioId.ToString());

        Assert.True(await assertStack.UserManager.CheckPasswordAsync(usuario2!, "ContraseñaVieja123"));

        var stampSinCambios = await assertStack.UserManager.GetSecurityStampAsync(usuario2!);
        Assert.Equal(stampOriginal, stampSinCambios);
    }
}
