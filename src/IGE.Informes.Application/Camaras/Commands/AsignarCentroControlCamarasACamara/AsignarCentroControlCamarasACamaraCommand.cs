using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.AsignarCentroControlCamarasACamara;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record AsignarCentroControlCamarasACamaraCommand(Guid CamaraId, Guid CentroControlCamarasId) : IRequest;
