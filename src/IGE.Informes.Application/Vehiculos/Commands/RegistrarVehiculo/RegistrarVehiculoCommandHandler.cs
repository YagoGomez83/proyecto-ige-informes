using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Commands.RegistrarVehiculo;

public sealed class RegistrarVehiculoCommandHandler(IAppDbContext dbContext) : IRequestHandler<RegistrarVehiculoCommand, Guid>
{
    public async Task<Guid> Handle(RegistrarVehiculoCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Dominio))
        {
            var yaExiste = await dbContext.Vehiculos.AnyAsync(v => v.Dominio == request.Dominio, cancellationToken);
            if (yaExiste)
            {
                throw new EntidadDuplicadaException(nameof(Vehiculo), nameof(Vehiculo.Dominio), request.Dominio);
            }
        }

        foreach (var categoriaAlertaId in request.CategoriasAlertaIds)
        {
            var existe = await dbContext.CategoriasAlerta.AnyAsync(c => c.Id == categoriaAlertaId, cancellationToken);
            if (!existe)
            {
                throw new EntidadNoEncontradaException(nameof(CategoriaAlerta), categoriaAlertaId);
            }
        }

        var vehiculo = new Vehiculo(
            request.Marca,
            request.Modelo,
            request.Color,
            request.DominioCerteza,
            request.AccionARealizar,
            request.AvisarA,
            request.Dominio,
            request.Caracteristicas);

        foreach (var categoriaAlertaId in request.CategoriasAlertaIds)
        {
            vehiculo.AsignarCategoriaAlerta(categoriaAlertaId);
        }

        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return vehiculo.Id;
    }
}
