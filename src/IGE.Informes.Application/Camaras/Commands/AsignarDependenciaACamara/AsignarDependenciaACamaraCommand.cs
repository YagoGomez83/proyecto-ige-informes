using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.AsignarDependenciaACamara;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record AsignarDependenciaACamaraCommand(Guid CamaraId, Guid DependenciaId) : IRequest;
