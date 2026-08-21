using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Dependencias.Commands.CrearDependencia;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CrearDependenciaCommand(string Nombre, TipoDependencia Tipo) : IRequest<Guid>;
