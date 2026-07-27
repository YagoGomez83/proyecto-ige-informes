using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.DesbloquearUsuario;

public sealed class DesbloquearUsuarioCommandHandler(IUserManagementService userManagementService, IAuditLogger auditLogger)
    : IRequestHandler<DesbloquearUsuarioCommand>
{
    public async Task Handle(DesbloquearUsuarioCommand request, CancellationToken cancellationToken)
    {
        var existe = await userManagementService.ExisteUsuarioAsync(request.UsuarioId, cancellationToken);
        if (!existe)
        {
            throw new EntidadNoEncontradaException("Usuario", request.UsuarioId);
        }

        await userManagementService.DesbloquearAsync(request.UsuarioId, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("DesbloquearUsuario", "Usuario", request.UsuarioId, cancellationToken);
    }
}
