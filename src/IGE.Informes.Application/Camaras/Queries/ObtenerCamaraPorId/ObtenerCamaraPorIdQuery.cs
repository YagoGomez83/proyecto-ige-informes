using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Camaras.Queries.ObtenerCamaraPorId;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerCamaraPorIdQuery(Guid CamaraId) : IRequest<CamaraDto?>;
