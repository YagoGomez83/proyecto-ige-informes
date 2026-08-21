using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.RegistrarCamara;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record RegistrarCamaraCommand(string Codigo, TipoCamara Tipo, string? Ubicacion, Guid? DependenciaId = null) : IRequest<Guid>;
