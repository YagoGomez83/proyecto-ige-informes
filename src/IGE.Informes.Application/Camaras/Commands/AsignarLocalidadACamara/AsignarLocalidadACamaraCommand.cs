using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.AsignarLocalidadACamara;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record AsignarLocalidadACamaraCommand(Guid CamaraId, Guid LocalidadId) : IRequest;
