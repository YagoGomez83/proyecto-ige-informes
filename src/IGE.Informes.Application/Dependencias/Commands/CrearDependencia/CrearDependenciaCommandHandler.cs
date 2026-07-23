using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Dependencias.Commands.CrearDependencia;

public sealed class CrearDependenciaCommandHandler(IAppDbContext dbContext) : IRequestHandler<CrearDependenciaCommand, Guid>
{
    public async Task<Guid> Handle(CrearDependenciaCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await dbContext.Dependencias.AnyAsync(d => d.Nombre == request.Nombre, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Dependencia), nameof(Dependencia.Nombre), request.Nombre);
        }

        var dependencia = new Dependencia(request.Nombre, request.Tipo);

        dbContext.Dependencias.Add(dependencia);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EntidadDuplicadaException(nameof(Dependencia), nameof(Dependencia.Nombre), request.Nombre);
        }

        return dependencia.Id;
    }
}
