using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ObtenerInformePorId;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerInformePorIdQuery(Guid InformeId) : IRequest<InformeDto?>;
