using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace IGE.Informes.Infrastructure.Identity;

public sealed class UserManagementService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) : IUserManagementService
{
    public async Task<IReadOnlyCollection<UsuarioDto>> ListarUsuariosAsync(CancellationToken cancellationToken)
    {
        var usuarios = userManager.Users.ToList();
        var resultado = new List<UsuarioDto>(usuarios.Count);

        foreach (var usuario in usuarios)
        {
            resultado.Add(await MapearAsync(usuario));
        }

        return resultado;
    }

    public async Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken) =>
        await userManager.FindByEmailAsync(email) is not null;

    public async Task<Guid?> CrearUsuarioAsync(
        string nombreCompleto, string email, string password, string rol, CancellationToken cancellationToken)
    {
        var usuario = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = nombreCompleto,
        };

        // Transacción explícita: si AddToRoleAsync falla después de que CreateAsync ya
        // persistió, sin rollback quedaría un usuario creado sin ningún rol asignado,
        // reportado igual como alta exitosa (mismo bug que se corrigió en CambiarRolAsync).
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var resultado = await userManager.CreateAsync(usuario, password);
        if (!resultado.Succeeded)
        {
            return null;
        }

        var resultadoRol = await userManager.AddToRoleAsync(usuario, rol);
        if (!resultadoRol.Succeeded)
        {
            throw new InvalidOperationException($"No se pudo asignar el rol '{rol}' al usuario nuevo.");
        }

        await transaction.CommitAsync(cancellationToken);

        return usuario.Id;
    }

    public async Task<bool> ExisteUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        await userManager.FindByIdAsync(usuarioId.ToString()) is not null;

    public async Task CambiarRolAsync(Guid usuarioId, string nuevoRol, CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException($"Usuario '{usuarioId}' no encontrado.");

        // Transacción explícita: RemoveFromRolesAsync/AddToRoleAsync/UpdateSecurityStampAsync
        // cada uno hace su propio SaveChanges vía UserManager. Sin esto, si algo falla a
        // mitad de camino (ej. AddToRoleAsync), el usuario queda sin ningún rol asignado.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rolesActuales = await userManager.GetRolesAsync(usuario);
        if (rolesActuales.Count > 0)
        {
            var resultadoRemover = await userManager.RemoveFromRolesAsync(usuario, rolesActuales);
            if (!resultadoRemover.Succeeded)
            {
                throw new InvalidOperationException(
                    $"No se pudo remover los roles actuales del usuario '{usuarioId}'.");
            }
        }

        var resultadoAgregar = await userManager.AddToRoleAsync(usuario, nuevoRol);
        if (!resultadoAgregar.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo asignar el rol '{nuevoRol}' al usuario '{usuarioId}'.");
        }

        // Mismo motivo que en BloquearAsync: invalida el SecurityStamp para que
        // PersistingRevalidatingAuthenticationStateProvider fuerce el circuito a
        // revalidar y refleje el nuevo rol, en vez de operar con permisos viejos.
        var resultadoStamp = await userManager.UpdateSecurityStampAsync(usuario);
        if (!resultadoStamp.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo invalidar el SecurityStamp del usuario '{usuarioId}'.");
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task BloquearAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException($"Usuario '{usuarioId}' no encontrado.");

        await userManager.SetLockoutEnabledAsync(usuario, true);
        await userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);

        // Invalida el SecurityStamp para que PersistingRevalidatingAuthenticationStateProvider
        // cierre cualquier circuito de Blazor Server ya abierto en su próximo ciclo de
        // revalidación (máx. 30 min), en vez de dejarlo activo hasta que expire la cookie.
        await userManager.UpdateSecurityStampAsync(usuario);
    }

    public async Task DesbloquearAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException($"Usuario '{usuarioId}' no encontrado.");

        await userManager.SetLockoutEndDateAsync(usuario, null);
    }

    private async Task<UsuarioDto> MapearAsync(ApplicationUser usuario)
    {
        var roles = await userManager.GetRolesAsync(usuario);
        var bloqueado = await userManager.IsLockedOutAsync(usuario);

        return new UsuarioDto(
            usuario.Id,
            usuario.NombreCompleto,
            usuario.Email ?? string.Empty,
            roles.Count > 0 ? roles[0] : string.Empty,
            bloqueado);
    }
}
