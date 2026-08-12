using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.TipoCausa.Queries.ListarTipoCausa;

public sealed record TipoCausaDto(Guid Id, string Nombre);

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarTipoCausaQuery : IRequest<IReadOnlyCollection<TipoCausaDto>>;
