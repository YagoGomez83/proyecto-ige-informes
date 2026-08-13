using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.CambiarPasswordPropia;

public sealed class CambiarPasswordPropiaCommandHandler(
    IUserManagementService userManagementService,
    ICurrentUserService currentUserService,
    IAuditLogger auditLogger) : IRequestHandler<CambiarPasswordPropiaCommand>
{
    public async Task Handle(CambiarPasswordPropiaCommand request, CancellationToken cancellationToken)
    {
        var usuarioId = currentUserService.UsuarioId
            ?? throw new InvalidOperationException("No hay un usuario autenticado.");

        var exito = await userManagementService.CambiarPasswordPropiaAsync(
            usuarioId, request.PasswordActual, request.PasswordNueva, cancellationToken);

        if (!exito)
        {
            throw new ReglaDeNegocioVioladaException(
                "No se pudo cambiar la contraseña: la contraseña actual es incorrecta o la nueva no cumple la política mínima de seguridad.");
        }

        await auditLogger.RegistrarAccesoAsync("CambiarPasswordPropia", "Usuario", usuarioId, cancellationToken);
    }
}
