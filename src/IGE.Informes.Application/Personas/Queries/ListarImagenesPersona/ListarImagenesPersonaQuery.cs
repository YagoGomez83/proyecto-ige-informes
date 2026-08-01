using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Queries.ListarImagenesPersona;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarImagenesPersonaQuery(Guid PersonaId)
    : IRequest<IReadOnlyCollection<PersonaImagenDto>>;

public sealed record PersonaImagenDto(Guid Id, string UrlDescarga, DateTime FechaCarga);
