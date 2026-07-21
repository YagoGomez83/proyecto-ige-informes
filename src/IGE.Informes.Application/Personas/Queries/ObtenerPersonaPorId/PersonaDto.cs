using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Personas.Queries.ObtenerPersonaPorId;

public sealed record PersonaDto(
    Guid Id,
    string? Nombre,
    string? Dni,
    RolPersona Rol,
    string? Caracteristicas);
