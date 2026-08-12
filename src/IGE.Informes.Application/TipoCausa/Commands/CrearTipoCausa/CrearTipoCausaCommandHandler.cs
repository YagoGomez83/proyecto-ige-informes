using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.TipoCausa.Commands.CrearTipoCausa;

public sealed class CrearTipoCausaCommandHandler(IAppDbContext dbContext) : IRequestHandler<CrearTipoCausaCommand, Guid>
{
    public async Task<Guid> Handle(CrearTipoCausaCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await dbContext.TiposCausa.AnyAsync(t => t.Nombre == request.Nombre, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Domain.Entities.TipoCausa), nameof(Domain.Entities.TipoCausa.Nombre), request.Nombre);
        }

        var tipoCausa = new Domain.Entities.TipoCausa(request.Nombre);

        dbContext.TiposCausa.Add(tipoCausa);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EntidadDuplicadaException(nameof(Domain.Entities.TipoCausa), nameof(Domain.Entities.TipoCausa.Nombre), request.Nombre);
        }

        return tipoCausa.Id;
    }
}
