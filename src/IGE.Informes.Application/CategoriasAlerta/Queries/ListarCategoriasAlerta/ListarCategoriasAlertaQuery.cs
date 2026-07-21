using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CategoriasAlerta.Queries.ListarCategoriasAlerta;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarCategoriasAlertaQuery : IRequest<IReadOnlyCollection<CategoriaAlertaDto>>;
