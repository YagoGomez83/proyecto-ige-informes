using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.AgregarImagenVehiculo;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record AgregarImagenVehiculoCommand(
    Guid VehiculoId,
    byte[] Contenido,
    string NombreArchivo,
    string TipoMime) : IRequest<Guid>;
