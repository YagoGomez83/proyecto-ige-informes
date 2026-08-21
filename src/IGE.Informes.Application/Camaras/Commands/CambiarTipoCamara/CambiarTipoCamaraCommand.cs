using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.CambiarTipoCamara;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CambiarTipoCamaraCommand(Guid CamaraId, TipoCamara NuevoTipo) : IRequest;
