using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.CentrosControlCamaras.Commands.CrearCentroControlCamaras;

public sealed class CrearCentroControlCamarasCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<CrearCentroControlCamarasCommand, Guid>
{
    public async Task<Guid> Handle(CrearCentroControlCamarasCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await dbContext.CentrosControlCamaras.AnyAsync(c => c.Sigla == request.Sigla, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(CentroControlCamaras), nameof(CentroControlCamaras.Sigla), request.Sigla);
        }

        var centro = new CentroControlCamaras(request.Sigla, request.Nombre);

        dbContext.CentrosControlCamaras.Add(centro);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EntidadDuplicadaException(nameof(CentroControlCamaras), nameof(CentroControlCamaras.Sigla), request.Sigla);
        }

        return centro.Id;
    }
}
