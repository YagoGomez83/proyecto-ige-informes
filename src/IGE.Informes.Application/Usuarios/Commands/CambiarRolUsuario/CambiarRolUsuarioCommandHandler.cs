using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.CambiarRolUsuario;

public sealed class CambiarRolUsuarioCommandHandler(
    IUserManagementService userManagementService,
    ICurrentUserService currentUserService,
    IAuditLogger auditLogger) : IRequestHandler<CambiarRolUsuarioCommand>
{
    public async Task Handle(CambiarRolUsuarioCommand request, CancellationToken cancellationToken)
    {
        var existe = await userManagementService.ExisteUsuarioAsync(request.UsuarioId, cancellationToken);
        if (!existe)
        {
            throw new EntidadNoEncontradaException("Usuario", request.UsuarioId);
        }

        if (request.UsuarioId == currentUserService.UsuarioId)
        {
            throw new ReglaDeNegocioVioladaException("No podés cambiar tu propio rol.");
        }

        await userManagementService.CambiarRolAsync(request.UsuarioId, request.NuevoRol, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("CambiarRolUsuario", "Usuario", request.UsuarioId, cancellationToken);
    }
}
