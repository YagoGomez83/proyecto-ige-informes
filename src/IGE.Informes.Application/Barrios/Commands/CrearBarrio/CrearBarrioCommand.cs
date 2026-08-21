using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Barrios.Commands.CrearBarrio;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CrearBarrioCommand(string Nombre, Guid? LocalidadId) : IRequest<Guid>;
