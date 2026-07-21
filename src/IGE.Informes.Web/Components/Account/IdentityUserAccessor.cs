using IGE.Informes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace IGE.Informes.Web.Components.Account;

public sealed class IdentityUserAccessor(UserManager<ApplicationUser> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: no se pudo cargar el usuario con ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}
