using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.TipoCausa.Commands.CrearTipoCausa;

[Autorizar(Roles.Admin)]
public sealed record CrearTipoCausaCommand(string Nombre) : IRequest<Guid>;
