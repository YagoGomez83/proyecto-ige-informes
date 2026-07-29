using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.TiposIncidente.Commands.CrearTipoIncidente;

[Autorizar(Roles.Admin)]
public sealed record CrearTipoIncidenteCommand(string Codigo, string Descripcion) : IRequest<Guid>;
