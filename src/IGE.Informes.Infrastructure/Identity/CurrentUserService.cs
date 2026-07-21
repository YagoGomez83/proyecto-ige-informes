using System.Security.Claims;
using IGE.Informes.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace IGE.Informes.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UsuarioId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray()
        ?? [];
}
