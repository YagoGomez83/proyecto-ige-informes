using Microsoft.AspNetCore.Identity;

namespace IGE.Informes.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Path en MinIO de la foto de perfil (mismo IFileStorage que
    /// VehiculoImagen/PersonaImagen). Null si el usuario no subió ninguna
    /// — la UI muestra las iniciales como fallback, igual que hoy en
    /// NavMenu.razor.
    /// </summary>
    public string? ImagenPerfilPath { get; set; }
}
