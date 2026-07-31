using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Queries.BuscarPersonas;

public sealed record PersonaBusquedaResultDto(Guid Id, string? Nombre, string? Dni, RolPersona Rol);

/// <summary>
/// Búsqueda de Personas por texto libre (Nombre, Dni, Características) para
/// la búsqueda combinada de /busqueda — extensión de HU-05. A diferencia de
/// ListarPersonasQuery, expone el Dni en el resultado (dato sensible, pero
/// necesario para que el Analista confirme que encontró a la persona
/// correcta) — mismo criterio de auditoría explícita ya usado en
/// BuscarInformesQueryHandler cuando el resultado incluye DNIs.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record BuscarPersonasQuery(string TextoLibre) : IRequest<IReadOnlyCollection<PersonaBusquedaResultDto>>;
