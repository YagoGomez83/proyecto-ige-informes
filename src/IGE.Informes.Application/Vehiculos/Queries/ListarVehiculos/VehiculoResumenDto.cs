using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;

public sealed record VehiculoResumenDto(
    Guid Id,
    string Marca,
    string Modelo,
    string? Dominio,
    EstadoVehiculo Estado,
    AccionARealizar AccionARealizar,
    TipoVehiculo TipoVehiculo);
