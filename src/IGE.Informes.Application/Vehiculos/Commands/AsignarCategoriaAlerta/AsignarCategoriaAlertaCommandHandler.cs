using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Commands.AsignarCategoriaAlerta;

public sealed class AsignarCategoriaAlertaCommandHandler(IAppDbContext dbContext) : IRequestHandler<AsignarCategoriaAlertaCommand>
{
    public async Task Handle(AsignarCategoriaAlertaCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = await dbContext.Vehiculos.FindAsync([request.VehiculoId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Vehiculo), request.VehiculoId);

        var categoriaExiste = await dbContext.CategoriasAlerta
            .AnyAsync(c => c.Id == request.CategoriaAlertaId, cancellationToken);
        if (!categoriaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(CategoriaAlerta), request.CategoriaAlertaId);
        }

        vehiculo.AsignarCategoriaAlerta(request.CategoriaAlertaId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
