using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Barrios.Commands.CrearBarrio;

[Autorizar(Roles.Admin)]
public sealed record CrearBarrioCommand(string Nombre) : IRequest<Guid>;
