using Microsoft.AspNetCore.Identity;

namespace IGE.Informes.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string NombreCompleto { get; set; } = string.Empty;
}
