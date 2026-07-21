using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Queries.ListarPersonas;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarPersonasQuery : IRequest<IReadOnlyCollection<PersonaResumenDto>>;
