using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Vehiculos.Queries.ObtenerVehiculoPorId;

public sealed record VehiculoDto(
    Guid Id,
    string Marca,
    string Modelo,
    string Color,
    string? Dominio,
    CertezaDominio DominioCerteza,
    EstadoVehiculo Estado,
    AccionARealizar AccionARealizar,
    string AvisarA,
    DateOnly? FechaBaja,
    string? Caracteristicas,
    TipoVehiculo TipoVehiculo,
    string? Cilindrada,
    IReadOnlyCollection<Guid> CategoriasAlertaIds);
