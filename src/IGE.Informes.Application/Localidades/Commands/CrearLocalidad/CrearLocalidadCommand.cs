using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Localidades.Commands.CrearLocalidad;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CrearLocalidadCommand(string Nombre) : IRequest<Guid>;
