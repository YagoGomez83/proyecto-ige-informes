using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ListarCasos;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarCasosQuery : IRequest<IReadOnlyCollection<CasoAnalisisResumenDto>>;
