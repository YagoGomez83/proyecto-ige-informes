namespace IGE.Informes.Application.Common.Interfaces;

public sealed record UsuarioDto(Guid Id, string NombreCompleto, string Email, string Rol, bool Bloqueado);

public sealed record PerfilUsuarioDto(Guid Id, string NombreCompleto, string Email, string Rol, string? ImagenPerfilPath);

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

    /// <summary>
    /// Resetea la contraseña del usuario indicado a <paramref name="nuevaPassword"/> e
    /// invalida su SecurityStamp (mismo mecanismo que <see cref="CambiarRolAsync"/> y
    /// <see cref="BloquearAsync"/>, para cortar cualquier sesión Blazor ya abierta con la
    /// contraseña vieja). Devuelve false si la contraseña no cumple la política mínima de
    /// Identity (el Handler traduce ese caso a una excepción de negocio con el detalle,
    /// mismo patrón que <see cref="CrearUsuarioAsync"/>), true si el reseteo tuvo éxito.
    /// </summary>
    Task<bool> ResetearPasswordAsync(Guid usuarioId, string nuevaPassword, CancellationToken cancellationToken);

    /// <summary>
    /// Cambia la contraseña del propio usuario autenticado, requiriendo la
    /// actual (a diferencia de <see cref="ResetearPasswordAsync"/>, que es
    /// un reseteo administrativo sin conocerla). Devuelve false si la
    /// actual no coincide o la nueva no cumple la política mínima de
    /// Identity — el Handler traduce ambos casos a una excepción de
    /// negocio con el detalle, mismo patrón que el resto del servicio.
    /// </summary>
    Task<bool> CambiarPasswordPropiaAsync(Guid usuarioId, string passwordActual, string passwordNueva, CancellationToken cancellationToken);

    /// <summary>
    /// Datos del propio usuario autenticado para la pantalla de Perfil —
    /// nombre, email, rol e ImagenPerfilPath (path crudo en MinIO, no URL;
    /// el Handler de la Query resuelve la URL prefirmada vía IFileStorage,
    /// mismo criterio que ListarImagenesVehiculoQueryHandler). Null si el
    /// usuario no existe (no debería pasar para un UsuarioId autenticado
    /// real, pero el Handler lo traduce a excepción igual que el resto).
    /// </summary>
    Task<PerfilUsuarioDto?> ObtenerPerfilPropioAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Reemplaza la foto de perfil del propio usuario autenticado. A
    /// diferencia de VehiculoImagen/PersonaImagen (colección con
    /// histórico), acá es un campo único — el Handler es responsable de
    /// eliminar el archivo anterior de MinIO antes de llamar a esto (mismo
    /// motivo que QuitarImagenVehiculoCommandHandler, evitar huérfanos).
    /// </summary>
    Task ActualizarImagenPerfilAsync(Guid usuarioId, string? imagenPerfilPath, CancellationToken cancellationToken);
}
