using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CentrosControlCamaras.Queries.ListarCentrosControlCamaras;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarCentrosControlCamarasQuery : IRequest<IReadOnlyCollection<CentroControlCamarasDto>>;
