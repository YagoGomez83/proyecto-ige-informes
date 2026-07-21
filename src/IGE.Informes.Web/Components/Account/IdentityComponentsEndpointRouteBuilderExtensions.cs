using System.Security.Claims;
using IGE.Informes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IGE.Informes.Web.Components.Account;

/// <summary>
/// Endpoints no interactivos para login/logout. Necesarios porque Blazor
/// Server no puede escribir la cookie de autenticación desde un componente
/// renderizado sobre SignalR (la respuesta HTTP ya se envió) — este es el
/// patrón estándar de ASP.NET Core Identity + Blazor Server.
/// </summary>
public static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        return endpoints;
    }
}
