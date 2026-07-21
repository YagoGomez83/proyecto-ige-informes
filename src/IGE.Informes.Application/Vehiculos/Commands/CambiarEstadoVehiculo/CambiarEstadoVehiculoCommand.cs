using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.CambiarEstadoVehiculo;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CambiarEstadoVehiculoCommand(Guid VehiculoId, EstadoVehiculo NuevoEstado) : IRequest;
