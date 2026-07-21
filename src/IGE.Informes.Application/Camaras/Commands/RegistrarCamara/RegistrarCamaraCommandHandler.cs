using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Camaras.Commands.RegistrarCamara;

public sealed class RegistrarCamaraCommandHandler(IAppDbContext dbContext) : IRequestHandler<RegistrarCamaraCommand, Guid>
{
    public async Task<Guid> Handle(RegistrarCamaraCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await dbContext.Camaras.AnyAsync(c => c.Codigo == request.Codigo, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Camara), nameof(Camara.Codigo), request.Codigo);
        }

        var camara = new Camara(request.Codigo, request.Tipo, request.Ubicacion);

        dbContext.Camaras.Add(camara);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Condición de carrera: otra alta con el mismo Codigo terminó
            // primero entre el chequeo AnyAsync y este SaveChanges — el
            // índice único de la base rechazó el insert. Se traduce al
            // mismo error de negocio que el chequeo previo, en vez de
            // dejar propagar la excepción de infraestructura.
            throw new EntidadDuplicadaException(nameof(Camara), nameof(Camara.Codigo), request.Codigo);
        }

        return camara.Id;
    }
}
