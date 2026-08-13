using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;

namespace IGE.Informes.Application.Usuarios.Commands.SubirImagenPerfil;

public sealed class SubirImagenPerfilCommandHandler(
    IUserManagementService userManagementService,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage,
    IAntivirusScanner antivirusScanner,
    IAuditLogger auditLogger) : IRequestHandler<SubirImagenPerfilCommand>
{
    public async Task Handle(SubirImagenPerfilCommand request, CancellationToken cancellationToken)
    {
        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        // Mismo criterio fail-closed que AgregarImagenVehiculoCommandHandler:
        // si ClamAV no responde, la excepción se propaga sin capturar.
        var estaLimpio = await antivirusScanner.EstaLimpioAsync(request.Contenido, cancellationToken);
        if (!estaLimpio)
        {
            throw new ReglaDeNegocioVioladaException(
                "La imagen fue rechazada por el escaneo antivirus — no se subió ni se guardó ningún dato.");
        }

        var perfilActual = await userManagementService.ObtenerPerfilPropioAsync(usuarioId, cancellationToken)
            ?? throw new EntidadNoEncontradaException("Usuario", usuarioId);

        using var stream = new MemoryStream(request.Contenido);
        var imagenPath = await fileStorage.SubirAsync(request.NombreArchivo, stream, request.TipoMime, cancellationToken);

        await userManagementService.ActualizarImagenPerfilAsync(usuarioId, imagenPath, cancellationToken);
        await auditLogger.RegistrarAccesoAsync("SubirImagenPerfil", "Usuario", usuarioId, cancellationToken);

        // Recién después de que la nueva quedó persistida se borra la
        // anterior — evita quedar sin ninguna imagen si EliminarAsync
        // fallara antes de guardar la nueva.
        if (perfilActual.ImagenPerfilPath is not null)
        {
            await fileStorage.EliminarAsync(perfilActual.ImagenPerfilPath, cancellationToken);
        }
    }
}
