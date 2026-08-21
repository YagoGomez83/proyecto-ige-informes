using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.CompletarUbicacionCamara;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CompletarUbicacionCamaraCommand(Guid CamaraId, string Ubicacion) : IRequest;
