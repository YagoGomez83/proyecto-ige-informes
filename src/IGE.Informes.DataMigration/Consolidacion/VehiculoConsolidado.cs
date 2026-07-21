using IGE.Informes.Domain.Entities;

namespace IGE.Informes.DataMigration.Consolidacion;

/// <summary>
/// Resultado de fusionar todas las filas del Excel que comparten el mismo
/// Dominio (o una fila individual sin Dominio) en un único Vehiculo.
/// </summary>
public sealed record VehiculoConsolidado(
    string? Dominio,
    string Marca,
    string Modelo,
    string Color,
    string? Caracteristicas,
    EstadoVehiculo Estado,
    IReadOnlyCollection<string> CategoriasAlerta,
    IReadOnlyCollection<string> HojasOrigen);
