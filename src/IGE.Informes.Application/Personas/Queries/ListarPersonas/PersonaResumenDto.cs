using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Personas.Queries.ListarPersonas;

public sealed record PersonaResumenDto(Guid Id, string? Nombre, RolPersona Rol, bool Identificada);
