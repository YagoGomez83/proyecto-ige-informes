using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.ColorVehiculo.Queries.ListarColorVehiculo;

public sealed record ColorVehiculoDto(Guid Id, string Nombre);

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarColorVehiculoQuery : IRequest<IReadOnlyCollection<ColorVehiculoDto>>;
