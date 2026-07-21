using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ListarInformesPorCaso;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarInformesPorCasoQuery(Guid CasoAnalisisId) : IRequest<IReadOnlyCollection<InformeResumenDto>>;
