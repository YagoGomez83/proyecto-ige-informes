using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Dependencias.Commands.AsignarUnidadRegionalADependencia;

[Autorizar(Roles.Admin)]
public sealed record AsignarUnidadRegionalADependenciaCommand(Guid DependenciaId, Guid UnidadRegionalId) : IRequest;
