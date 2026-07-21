using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ObtenerCasoPorId;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerCasoPorIdQuery(Guid CasoId) : IRequest<CasoAnalisisDto?>;
