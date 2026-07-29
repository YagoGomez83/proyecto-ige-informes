using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.TiposIncidente.Commands.CrearTipoIncidente;

public sealed class CrearTipoIncidenteCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<CrearTipoIncidenteCommand, Guid>
{
    public async Task<Guid> Handle(CrearTipoIncidenteCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await dbContext.TiposIncidente.AnyAsync(t => t.Codigo == request.Codigo, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(TipoIncidente), nameof(TipoIncidente.Codigo), request.Codigo);
        }

        var tipoIncidente = new TipoIncidente(request.Codigo, request.Descripcion);

        dbContext.TiposIncidente.Add(tipoIncidente);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EntidadDuplicadaException(nameof(TipoIncidente), nameof(TipoIncidente.Codigo), request.Codigo);
        }

        return tipoIncidente.Id;
    }
}
