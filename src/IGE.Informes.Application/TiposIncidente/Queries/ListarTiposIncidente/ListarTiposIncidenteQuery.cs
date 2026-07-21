using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.TiposIncidente.Queries.ListarTiposIncidente;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarTiposIncidenteQuery : IRequest<IReadOnlyCollection<TipoIncidenteDto>>;
