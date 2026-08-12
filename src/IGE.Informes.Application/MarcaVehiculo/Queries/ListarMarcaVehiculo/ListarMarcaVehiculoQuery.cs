using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.MarcaVehiculo.Queries.ListarMarcaVehiculo;

public sealed record MarcaVehiculoDto(Guid Id, string Nombre);

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarMarcaVehiculoQuery : IRequest<IReadOnlyCollection<MarcaVehiculoDto>>;
