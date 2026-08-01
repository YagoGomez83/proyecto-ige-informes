using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarImagenesVehiculo;

public sealed class ListarImagenesVehiculoQueryHandler(IAppDbContext dbContext, IFileStorage fileStorage, IAuditLogger auditLogger)
    : IRequestHandler<ListarImagenesVehiculoQuery, IReadOnlyCollection<VehiculoImagenDto>>
{
    public async Task<IReadOnlyCollection<VehiculoImagenDto>> Handle(
        ListarImagenesVehiculoQuery request, CancellationToken cancellationToken)
    {
        var imagenes = await dbContext.VehiculoImagenes.AsNoTracking()
            .Where(vi => vi.VehiculoId == request.VehiculoId)
            .OrderByDescending(vi => vi.FechaCarga)
            .ToListAsync(cancellationToken);

        var resultado = new List<VehiculoImagenDto>(imagenes.Count);
        foreach (var imagen in imagenes)
        {
            var url = await fileStorage.ObtenerUrlDescargaAsync(imagen.ImagenPath, cancellationToken);
            resultado.Add(new VehiculoImagenDto(imagen.Id, url, imagen.FechaCarga));
        }

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Vehiculo), request.VehiculoId, cancellationToken);

        return resultado;
    }
}
