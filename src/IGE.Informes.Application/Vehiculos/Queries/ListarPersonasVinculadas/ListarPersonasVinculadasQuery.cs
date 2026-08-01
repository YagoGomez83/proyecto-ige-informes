using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarPersonasVinculadas;

/// <summary>
/// Personas vinculadas directamente a un Vehículo (HU-09, "vincular Persona
/// a un Vehículo") — vínculo independiente de los Informes/Casos en los
/// que ambos aparezcan (ver <see cref="PersonaVehiculo"/>).
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarPersonasVinculadasQuery(Guid VehiculoId)
    : IRequest<IReadOnlyCollection<PersonaVinculadaResumenDto>>;

public sealed record PersonaVinculadaResumenDto(Guid Id, string? Nombre, string? Dni, RolPersona Rol);
