using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Commands.PublicarInforme;

public sealed class PublicarInformeCommandHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<PublicarInformeCommand>
{
    public async Task Handle(PublicarInformeCommand request, CancellationToken cancellationToken)
    {
        var informe = await dbContext.Informes
            .Include(i => i.Analistas)
            .FirstOrDefaultAsync(i => i.Id == request.InformeId, cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Informe), request.InformeId);

        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        // Un Informe Publicado es inmutable: no se agregan firmantes nuevos
        // ni se vuelve a firmar. Publicar() ya es un no-op silencioso en ese
        // caso, así que alcanza con no tocar los Analistas.
        if (informe.Estado != EstadoInforme.Publicado)
        {
            // AgregarFirmante hace Remove+Add cuando el usuario ya figuraba
            // con otro rol (ej. Interviniente) — en ese caso el tracker ya
            // conoce la clave (InformeId, UsuarioId) y forzar Added rompe
            // por conflicto de identidad. Solo hace falta el workaround del
            // bug de EF Core cuando es un alta real (el usuario no tenía
            // ningún InformeAnalista previo).
            var esAltaReal = !informe.Analistas.Any(a => a.UsuarioId == usuarioId);

            informe.AgregarFirmante(usuarioId);

            if (esAltaReal)
            {
                var nuevoFirmante = informe.Analistas.Single(a => a.UsuarioId == usuarioId);
                dbContext.MarcarInformeAnalistaComoAgregado(nuevoFirmante);
            }
        }

        informe.Publicar();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
