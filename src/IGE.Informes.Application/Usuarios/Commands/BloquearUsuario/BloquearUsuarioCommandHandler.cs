using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.BloquearUsuario;

public sealed class BloquearUsuarioCommandHandler(
    IUserManagementService userManagementService,
    ICurrentUserService currentUserService,
    IAuditLogger auditLogger) : IRequestHandler<BloquearUsuarioCommand>
{
    public async Task Handle(BloquearUsuarioCommand request, CancellationToken cancellationToken)
    {
        var existe = await userManagementService.ExisteUsuarioAsync(request.UsuarioId, cancellationToken);
        if (!existe)
        {
            throw new EntidadNoEncontradaException("Usuario", request.UsuarioId);
        }

        if (request.UsuarioId == currentUserService.UsuarioId)
        {
            throw new ReglaDeNegocioVioladaException("No podés bloquear tu propio usuario.");
        }

        await userManagementService.BloquearAsync(request.UsuarioId, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("BloquearUsuario", "Usuario", request.UsuarioId, cancellationToken);
    }
}
