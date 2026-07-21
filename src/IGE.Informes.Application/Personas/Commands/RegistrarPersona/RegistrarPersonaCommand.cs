using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.RegistrarPersona;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record RegistrarPersonaCommand(
    RolPersona Rol,
    string? Nombre,
    string? Dni,
    string? Caracteristicas) : IRequest<Guid>;
