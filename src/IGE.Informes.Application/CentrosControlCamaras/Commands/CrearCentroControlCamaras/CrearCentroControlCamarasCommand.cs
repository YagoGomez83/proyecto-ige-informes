using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CentrosControlCamaras.Commands.CrearCentroControlCamaras;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CrearCentroControlCamarasCommand(string Sigla, string Nombre) : IRequest<Guid>;
