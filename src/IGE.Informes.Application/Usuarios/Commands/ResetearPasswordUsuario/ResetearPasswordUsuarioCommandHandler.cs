using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.ResetearPasswordUsuario;

public sealed class ResetearPasswordUsuarioCommandHandler(
    IUserManagementService userManagementService,
    ICurrentUserService currentUserService,
    IAuditLogger auditLogger) : IRequestHandler<ResetearPasswordUsuarioCommand>
{
    public async Task Handle(ResetearPasswordUsuarioCommand request, CancellationToken cancellationToken)
    {
        var existe = await userManagementService.ExisteUsuarioAsync(request.UsuarioId, cancellationToken);
        if (!existe)
        {
            throw new EntidadNoEncontradaException("Usuario", request.UsuarioId);
        }

        if (request.UsuarioId == currentUserService.UsuarioId)
        {
            throw new ReglaDeNegocioVioladaException(
                "No podés resetear tu propia contraseña desde acá — usá Cuenta > Administrar > Contraseña.");
        }

        var exito = await userManagementService.ResetearPasswordAsync(
            request.UsuarioId, request.NuevaPassword, cancellationToken);

        if (!exito)
        {
            throw new ReglaDeNegocioVioladaException(
                "No se pudo resetear la contraseña: no cumple la política mínima de seguridad.");
        }

        await auditLogger.RegistrarAccesoAsync("ResetearPasswordUsuario", "Usuario", request.UsuarioId, cancellationToken);
    }
}
