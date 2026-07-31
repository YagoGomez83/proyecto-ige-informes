using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Queries.BuscarVehiculos;

/// <summary>
/// Búsqueda de Vehículos por texto libre (Dominio, Marca, Modelo,
/// Características) para la búsqueda combinada de /busqueda — extensión de
/// HU-05 más allá de solo Informe. Contains/ILIKE simple, no full-text
/// search (no hay campo de texto largo tipo Relato en Vehiculo que lo
/// justifique).
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record BuscarVehiculosQuery(string TextoLibre) : IRequest<IReadOnlyCollection<VehiculoResumenDto>>;
