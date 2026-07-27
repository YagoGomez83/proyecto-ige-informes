using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.CrearUsuario;

public sealed class CrearUsuarioCommandHandler(IUserManagementService userManagementService, IAuditLogger auditLogger)
    : IRequestHandler<CrearUsuarioCommand, Guid>
{
    public async Task<Guid> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await userManagementService.ExisteEmailAsync(request.Email, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException("Usuario", "Email", request.Email);
        }

        var usuarioId = await userManagementService.CrearUsuarioAsync(
            request.NombreCompleto, request.Email, request.Password, request.Rol, cancellationToken);

        if (usuarioId is null)
        {
            throw new ReglaDeNegocioVioladaException(
                "No se pudo crear el usuario: la contraseña no cumple la política mínima de seguridad.");
        }

        await auditLogger.RegistrarAccesoAsync("CrearUsuario", "Usuario", usuarioId.Value, cancellationToken);

        return usuarioId.Value;
    }
}
