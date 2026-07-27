namespace IGE.Informes.Application.Common.Interfaces;

public sealed record UsuarioDto(Guid Id, string NombreCompleto, string Email, string Rol, bool Bloqueado);

/// <summary>
/// Puerto hacia ASP.NET Core Identity: Application depende solo de esta
/// interfaz, nunca de UserManager/RoleManager directamente (Clean
/// Architecture) — ver HU-17 en docs/epic-04-gestion-catalogos.md. No
/// expone IdentityResult ni ningún tipo de Microsoft.AspNetCore.Identity;
/// los errores se comunican con bool/excepciones propias de
/// Application.Common.Exceptions, igual que el resto del proyecto.
/// </summary>
public interface IUserManagementService
{
    Task<IReadOnlyCollection<UsuarioDto>> ListarUsuariosAsync(CancellationToken cancellationToken);

    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Crea el usuario con el rol indicado. Devuelve el Id del usuario
    /// creado, o null si la contraseña no cumple la política mínima de
    /// Identity (el Handler traduce ese caso a una excepción de negocio
    /// con el detalle, ver CrearUsuarioCommandHandler).
    /// </summary>
    Task<Guid?> CrearUsuarioAsync(string nombreCompleto, string email, string password, string rol, CancellationToken cancellationToken);

    Task<bool> ExisteUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task CambiarRolAsync(Guid usuarioId, string nuevoRol, CancellationToken cancellationToken);

    Task BloquearAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task DesbloquearAsync(Guid usuarioId, CancellationToken cancellationToken);
}
