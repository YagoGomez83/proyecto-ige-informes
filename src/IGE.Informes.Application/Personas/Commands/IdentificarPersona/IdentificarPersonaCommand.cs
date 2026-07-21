using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.IdentificarPersona;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record IdentificarPersonaCommand(Guid PersonaId, string Nombre, string? Dni) : IRequest;
