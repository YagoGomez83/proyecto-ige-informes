using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Queries.ObtenerPerfilPropio;

public sealed class ObtenerPerfilPropioQueryHandler(
    IUserManagementService userManagementService,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage,
    IAuditLogger auditLogger) : IRequestHandler<ObtenerPerfilPropioQuery, PerfilPropioDto>
{
    public async Task<PerfilPropioDto> Handle(ObtenerPerfilPropioQuery request, CancellationToken cancellationToken)
    {
        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        var perfil = await userManagementService.ObtenerPerfilPropioAsync(usuarioId, cancellationToken)
            ?? throw new EntidadNoEncontradaException("Usuario", usuarioId);

        var imagenUrl = perfil.ImagenPerfilPath is null
            ? null
            : await fileStorage.ObtenerUrlDescargaAsync(perfil.ImagenPerfilPath, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", "Usuario", usuarioId, cancellationToken);

        return new PerfilPropioDto(perfil.NombreCompleto, perfil.Email, perfil.Rol, imagenUrl);
    }
}
