using IGE.Informes.Infrastructure.Identity;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IGE.Informes.IntegrationTests.Usuarios;

/// <summary>
/// Stack mínimo de Identity (UserManager/RoleManager) contra un AppDbContext
/// real, para tests de integración de UserManagementService sin levantar
/// toda la aplicación Web.
/// </summary>
internal sealed class IdentityStack : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public IdentityStack(DbContextOptions<AppDbContext> options)
    {
        DbContext = new AppDbContext(options);

        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(DbContext)
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .Services
            .BuildServiceProvider();

        UserManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager = _serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    }

    public AppDbContext DbContext { get; }

    public UserManager<ApplicationUser> UserManager { get; }

    public RoleManager<ApplicationRole> RoleManager { get; }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }
}
