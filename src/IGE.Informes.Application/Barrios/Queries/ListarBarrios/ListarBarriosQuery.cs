using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Barrios.Queries.ListarBarrios;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarBarriosQuery : IRequest<IReadOnlyCollection<BarrioDto>>;
