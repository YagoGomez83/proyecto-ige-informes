using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.EliminarPersona;

[Autorizar(Roles.Supervisor, Roles.Admin)]
public sealed record EliminarPersonaCommand(Guid PersonaId) : IRequest;
