using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Queries.ListarVehiculosVinculados;

/// <summary>
/// Vehículos vinculados directamente a una Persona (HU-09, "vincular
/// Persona a un Vehículo") — mismo vínculo que
/// ListarPersonasVinculadasQuery, visto desde el lado de la Persona.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarVehiculosVinculadosQuery(Guid PersonaId)
    : IRequest<IReadOnlyCollection<VehiculoVinculadoResumenDto>>;

public sealed record VehiculoVinculadoResumenDto(Guid Id, string Marca, string Modelo, string? Dominio);
