using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.RegistrarCamara;

[Autorizar(Roles.Admin)]
public sealed record RegistrarCamaraCommand(string Codigo, TipoCamara Tipo, string? Ubicacion) : IRequest<Guid>;
