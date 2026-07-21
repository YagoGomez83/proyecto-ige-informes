using IGE.Informes.Application.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IGE.Informes.Infrastructure.Identity;

/// <summary>
/// Crea los roles del sistema y, si no existe ningún usuario todavía, un
/// primer Admin a partir de variables de entorno — necesario porque no hay
/// AD/LDAP (ver 00-vision-alcance.md) y alguien tiene que poder loguearse
/// la primera vez que se levanta el sistema.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles.Todos)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }

        if (userManager.Users.Any())
        {
            return;
        }

        var adminEmail = Environment.GetEnvironmentVariable("IGE_ADMIN_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("IGE_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            NombreCompleto = "Administrador",
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
