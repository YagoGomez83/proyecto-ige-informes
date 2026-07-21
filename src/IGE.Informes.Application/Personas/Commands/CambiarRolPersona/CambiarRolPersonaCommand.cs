using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.CambiarRolPersona;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CambiarRolPersonaCommand(Guid PersonaId, RolPersona NuevoRol) : IRequest;
