using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.QuitarImagenPersona;

[Autorizar(Roles.Supervisor, Roles.Admin)]
public sealed record QuitarImagenPersonaCommand(Guid PersonaImagenId) : IRequest;
