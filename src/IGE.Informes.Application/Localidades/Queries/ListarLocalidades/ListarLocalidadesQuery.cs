using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Localidades.Queries.ListarLocalidades;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarLocalidadesQuery : IRequest<IReadOnlyCollection<LocalidadDto>>;
